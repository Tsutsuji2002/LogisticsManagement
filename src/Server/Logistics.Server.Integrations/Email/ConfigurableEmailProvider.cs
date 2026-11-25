using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Server.Integrations.Email;

/// <summary>
/// Email provider router selecting configured provider (SMTP or SendGrid) with redaction,
/// timeout enforcement, cancellation support, and transient vs permanent failure classification.
/// </summary>
public sealed class ConfigurableEmailProvider : IEmailProvider
{
    private readonly IOptions<EmailSettings> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurableEmailProvider> _logger;

    public ConfigurableEmailProvider(
        IOptions<EmailSettings> options,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurableEmailProvider> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ProviderName => _options.Value.Provider;

    public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        var provider = settings.Provider?.Trim() ?? "None";

        if (string.Equals(provider, "SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaSendGridAsync(settings, request, cancellationToken);
        }

        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaSmtpAsync(settings, request, cancellationToken);
        }

        _logger.LogWarning(
            "Email delivery skipped for recipient {Recipient}: Email provider is configured as '{Provider}'.",
            RedactEmail(request.ToEmail), provider);

        return new EmailSendResult
        {
            Succeeded = false,
            FailureClassification = EmailFailureClassification.ConfigurationMissing,
            ErrorDetails = $"Email provider is not configured (Email:Provider='{provider}')."
        };
    }

    private async Task<EmailSendResult> SendViaSendGridAsync(
        EmailSettings settings,
        EmailSendRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.SendGrid.ApiKey))
        {
            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.ConfigurationMissing,
                ErrorDetails = "SendGrid API key is not configured."
            };
        }

        var timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 3, 120));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            var client = _httpClientFactory.CreateClient("SendGrid");
            var baseUrl = string.IsNullOrWhiteSpace(settings.SendGrid.BaseUrl)
                ? "https://api.sendgrid.com"
                : settings.SendGrid.BaseUrl.TrimEnd('/');

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[]
                        {
                            new { email = request.ToEmail, name = request.ToName }
                        },
                        custom_args = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                            ? null
                            : new Dictionary<string, string> { ["idempotency_key"] = request.IdempotencyKey }
                    }
                },
                from = new { email = settings.FromEmail, name = settings.FromName },
                subject = request.Subject,
                content = new[]
                {
                    new
                    {
                        type = request.IsHtml ? "text/html" : "text/plain",
                        value = request.Body
                    }
                }
            };

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v3/mail/send")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.SendGrid.ApiKey);

            using var response = await client.SendAsync(httpReq, timeoutCts.Token);
            var statusCode = (int)response.StatusCode;
            var messageId = response.Headers.TryGetValues("X-Message-Id", out var ids) ? ids.FirstOrDefault() : null;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent via SendGrid to {Recipient} (MessageId: {MessageId}).",
                    RedactEmail(request.ToEmail), messageId ?? "N/A");

                return new EmailSendResult
                {
                    Succeeded = true,
                    ProviderMessageId = messageId,
                    GatewayResponse = $"HTTP {statusCode}; MessageId={messageId}"
                };
            }

            int? retryAfter = null;
            if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
            {
                retryAfter = Math.Max(1, (int)Math.Ceiling(delta.TotalSeconds));
            }
            else if (response.Headers.RetryAfter?.Date is DateTimeOffset date)
            {
                retryAfter = Math.Max(1, (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds));
            }

            var isTransient = response.StatusCode == HttpStatusCode.TooManyRequests || statusCode >= 500;
            var errorBody = SanitizeGatewayError(await response.Content.ReadAsStringAsync(timeoutCts.Token));

            _logger.LogWarning("SendGrid returned HTTP {StatusCode} for {Recipient}. Classification: {Classification}",
                statusCode, RedactEmail(request.ToEmail), isTransient ? "Transient" : "Permanent");

            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = isTransient ? EmailFailureClassification.Transient : EmailFailureClassification.Permanent,
                RetryAfterSeconds = retryAfter,
                GatewayResponse = $"HTTP {statusCode}",
                ErrorDetails = errorBody
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("SendGrid request timed out after {Timeout}s for {Recipient}.",
                timeout.TotalSeconds, RedactEmail(request.ToEmail));

            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.Transient,
                ErrorDetails = $"SendGrid request timed out after {timeout.TotalSeconds:0} seconds."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "SendGrid network error for {Recipient}.", RedactEmail(request.ToEmail));
            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.Transient,
                ErrorDetails = SanitizeGatewayError(ex.Message)
            };
        }
    }

    private async Task<EmailSendResult> SendViaSmtpAsync(
        EmailSettings settings,
        EmailSendRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Smtp.Host))
        {
            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.ConfigurationMissing,
                ErrorDetails = "SMTP Host is not configured."
            };
        }

        var timeoutMs = Math.Clamp(settings.TimeoutSeconds, 3, 120) * 1000;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = request.IsHtml
            };
            message.To.Add(new MailAddress(request.ToEmail, request.ToName ?? request.ToEmail));

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                message.Headers.Add("X-Idempotency-Key", request.IdempotencyKey);
            }

            using var smtpClient = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port)
            {
                EnableSsl = settings.Smtp.EnableSsl,
                Timeout = timeoutMs
            };

            if (!string.IsNullOrWhiteSpace(settings.Smtp.Username))
            {
                smtpClient.Credentials = new NetworkCredential(settings.Smtp.Username, settings.Smtp.Password);
            }

            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent via SMTP to {Recipient}.", RedactEmail(request.ToEmail));

            return new EmailSendResult
            {
                Succeeded = true,
                GatewayResponse = $"SMTP {settings.Smtp.Host}:{settings.Smtp.Port} 250 OK"
            };
        }
        catch (FormatException ex)
        {
            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.Permanent,
                ErrorDetails = $"Invalid recipient or sender email format: {ex.Message}"
            };
        }
        catch (SmtpException ex)
        {
            var transient = ex.StatusCode is SmtpStatusCode.MailboxBusy
                or SmtpStatusCode.InsufficientStorage
                or SmtpStatusCode.ServiceNotAvailable
                or SmtpStatusCode.ServiceClosingTransmissionChannel
                or SmtpStatusCode.TransactionFailed
                or SmtpStatusCode.GeneralFailure;

            _logger.LogWarning("SMTP failure ({StatusCode}) for {Recipient}. Classification: {Classification}",
                ex.StatusCode, RedactEmail(request.ToEmail), transient ? "Transient" : "Permanent");

            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = transient ? EmailFailureClassification.Transient : EmailFailureClassification.Permanent,
                GatewayResponse = $"SMTP {ex.StatusCode}",
                ErrorDetails = SanitizeGatewayError(ex.Message)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.Transient,
                ErrorDetails = SanitizeGatewayError(ex.Message)
            };
        }
    }

    public static string RedactEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "<empty>";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***";
        return $"{email[0]}***{email[atIndex..]}";
    }

    public static string SanitizeGatewayError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var sanitized = raw
            .Replace("Bearer ", "Bearer [REDACTED] ", StringComparison.OrdinalIgnoreCase)
            .Replace("SG.", "SG.[REDACTED]", StringComparison.OrdinalIgnoreCase);
        return sanitized.Length > 950 ? sanitized[..950] : sanitized;
    }
}
