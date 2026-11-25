using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Exceptions;
using Logistics.Shared.Contracts;

namespace Logistics.Client.Infrastructure.Http;

/// <summary>
/// Centralized HTTP implementation of <see cref="IApiClient"/> with structured error mapping,
/// timeout/network handling, and safe binary downloads.
/// </summary>
public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ApiClientOptions? _options;

    public ApiClient(HttpClient httpClient, ApiClientOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        => await SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Get, BuildUri(endpoint)), cancellationToken);

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse>(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, BuildUri(endpoint));
            if (request is not null)
            {
                req.Content = JsonContent.Create(request, options: JsonOptions);
            }
            return req;
        }, cancellationToken);
    }

    public async Task<TResponse?> PostEmptyAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        => await SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Post, BuildUri(endpoint)), cancellationToken);

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Put, BuildUri(endpoint))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, cancellationToken);
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse>(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, BuildUri(endpoint));
            if (request is not null)
            {
                req.Content = JsonContent.Create(request, options: JsonOptions);
            }
            return req;
        }, cancellationToken);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await SendAsync<ApiResponse>(() => new HttpRequestMessage(HttpMethod.Delete, BuildUri(endpoint)), cancellationToken);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetFileAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(endpoint));
        using var response = await ExecuteHttpAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ParseApiExceptionAsync(response, cancellationToken);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                    ?? "export.csv";

        return (bytes, contentType, fileName);
    }

    private Uri BuildUri(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absolute))
            return absolute;

        var configuredBase = _options?.BaseUrl;
        if (!string.IsNullOrWhiteSpace(configuredBase) && Uri.TryCreate(configuredBase.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            return new Uri(baseUri, endpoint.TrimStart('/'));
        }

        if (_httpClient.BaseAddress != null)
            return new Uri(_httpClient.BaseAddress, endpoint.TrimStart('/'));

        throw new ApiException(
            "API Base URL chưa được cấu hình.",
            HttpStatusCode.ServiceUnavailable,
            isNetworkError: true);
    }

    private async Task<TResponse?> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        using var request = requestFactory();
        using var response = await ExecuteHttpAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ParseApiExceptionAsync(response, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return default;

        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    private async Task<HttpResponseMessage> ExecuteHttpAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(
                "Yêu cầu tới API quá thời gian phản hồi (timeout).",
                HttpStatusCode.RequestTimeout,
                isTimeout: true,
                innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                "Không thể kết nối tới máy chủ API. Kiểm tra URL hoặc dịch vụ backend.",
                HttpStatusCode.ServiceUnavailable,
                isNetworkError: true,
                innerException: ex);
        }
    }

    private static async Task<ApiException> ParseApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string raw = string.Empty;
        try
        {
            raw = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            // Ignore body read error on failed response
        }

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var apiErr = JsonSerializer.Deserialize<ApiResponse>(raw, JsonOptions);
                if (apiErr != null && (!string.IsNullOrWhiteSpace(apiErr.Message) || apiErr.ValidationErrors != null))
                {
                    var msg = !string.IsNullOrWhiteSpace(apiErr.Message)
                        ? apiErr.Message
                        : $"HTTP {(int)response.StatusCode} ({response.StatusCode})";

                    return new ApiException(
                        msg,
                        response.StatusCode,
                        apiErr.ErrorCode,
                        apiErr.TraceId,
                        apiErr.Errors,
                        apiErr.ValidationErrors);
                }
            }
            catch
            {
                // Fall back to status-based message
            }
        }

        var fallbackMessage = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.",
            HttpStatusCode.Forbidden => "Bạn không có quyền hoặc không thuộc phạm vi chi nhánh cho thao tác này.",
            HttpStatusCode.NotFound => "Không tìm thấy tài nguyên yêu cầu.",
            _ => $"Máy chủ trả về lỗi HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
        };

        return new ApiException(fallbackMessage, response.StatusCode);
    }
}
