using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Services;
using Logistics.Server.Integrations.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Notification;

public class EmailDeliveryTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("EmailDeliveryTest_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ConfigurableEmailProvider_None_ReturnsConfigurationMissingInsteadOfFakeSuccess()
    {
        var factory = new Mock<IHttpClientFactory>();
        var provider = new ConfigurableEmailProvider(
            Options.Create(new EmailSettings { Provider = "None" }),
            factory.Object,
            NullLogger<ConfigurableEmailProvider>.Instance);

        var result = await provider.SendAsync(new EmailSendRequest
        {
            ToEmail = "user@example.com",
            Subject = "Test",
            Body = "Body"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureClassification.Should().Be(EmailFailureClassification.ConfigurationMissing);
    }

    [Fact]
    public async Task SendGridProvider_202Accepted_ReturnsSucceededWithMessageId()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var msg = new HttpResponseMessage(HttpStatusCode.Accepted);
            msg.Headers.Add("X-Message-Id", "sg-msg-12345");
            return Task.FromResult(msg);
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SendGrid")).Returns(new HttpClient(handler));

        var settings = new EmailSettings
        {
            Provider = "SendGrid",
            FromEmail = "noreply@logistics.test",
            SendGrid = new EmailSettings.SendGridOptions { ApiKey = "SG.secret-key-value", BaseUrl = "https://api.sendgrid.com" }
        };

        var provider = new ConfigurableEmailProvider(
            Options.Create(settings), factory.Object, NullLogger<ConfigurableEmailProvider>.Instance);

        var result = await provider.SendAsync(new EmailSendRequest
        {
            ToEmail = "customer@example.com",
            Subject = "Order Confirmed",
            Body = "<p>Confirmed</p>"
        });

        result.Succeeded.Should().BeTrue();
        result.ProviderMessageId.Should().Be("sg-msg-12345");
        result.FailureClassification.Should().Be(EmailFailureClassification.None);
    }

    [Fact]
    public async Task SendGridProvider_429WithRetryAfter_ClassifiesTransientAndCapturesRetryAfter()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var msg = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded for Bearer SG.secret")
            };
            msg.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(42));
            return Task.FromResult(msg);
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SendGrid")).Returns(new HttpClient(handler));

        var settings = new EmailSettings
        {
            Provider = "SendGrid",
            SendGrid = new EmailSettings.SendGridOptions { ApiKey = "SG.secret" }
        };

        var provider = new ConfigurableEmailProvider(
            Options.Create(settings), factory.Object, NullLogger<ConfigurableEmailProvider>.Instance);

        var result = await provider.SendAsync(new EmailSendRequest
        {
            ToEmail = "customer@example.com",
            Subject = "Test",
            Body = "Body"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureClassification.Should().Be(EmailFailureClassification.Transient);
        result.RetryAfterSeconds.Should().Be(42);
        result.ErrorDetails.Should().NotContain("SG.secret");
    }

    [Fact]
    public async Task SendGridProvider_400BadRequest_ClassifiesPermanent()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid recipient address")
            }));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SendGrid")).Returns(new HttpClient(handler));

        var settings = new EmailSettings
        {
            Provider = "SendGrid",
            SendGrid = new EmailSettings.SendGridOptions { ApiKey = "SG.valid" }
        };

        var provider = new ConfigurableEmailProvider(
            Options.Create(settings), factory.Object, NullLogger<ConfigurableEmailProvider>.Instance);

        var result = await provider.SendAsync(new EmailSendRequest
        {
            ToEmail = "bad@invalid",
            Subject = "Test",
            Body = "Body"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureClassification.Should().Be(EmailFailureClassification.Permanent);
    }

    [Fact]
    public async Task SendGridProvider_Timeout_ClassifiesTransient()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SendGrid")).Returns(new HttpClient(handler));

        var settings = new EmailSettings
        {
            Provider = "SendGrid",
            TimeoutSeconds = 3, // Clamped minimum 3s — test uses linked cancel token so let's keep 1s? Timeout is clamped to 3s min
            SendGrid = new EmailSettings.SendGridOptions { ApiKey = "SG.valid" }
        };

        var provider = new ConfigurableEmailProvider(
            Options.Create(settings), factory.Object, NullLogger<ConfigurableEmailProvider>.Instance);

        var result = await provider.SendAsync(new EmailSendRequest
        {
            ToEmail = "c@example.com",
            Subject = "Timeout test",
            Body = "Body"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureClassification.Should().Be(EmailFailureClassification.Transient);
        result.ErrorDetails.Should().Contain("timed out");
    }

    [Fact]
    public async Task NotificationDeliveryService_TransientRetriesAndExhaustion_MarksFailedAfterMaxAttempts()
    {
        await using var db = CreateInMemoryContext();
        var notif = new Server.Domain.Entities.Notification.Notification
        {
            RecipientEmail = "user@example.com",
            Title = "Alert",
            Content = "Test",
            Channel = "Email",
            Status = "Pending"
        };
        db.Notifications.Add(notif);
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailProvider>();
        emailMock.Setup(p => p.SendAsync(It.IsAny<EmailSendRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult
            {
                Succeeded = false,
                FailureClassification = EmailFailureClassification.Transient,
                ErrorDetails = "Temporary 503"
            });

        var currentUser = new Mock<ICurrentUserService>();
        var audit = new Mock<IAuditService>();
        var notifService = new NotificationService(
            db, new NotificationTemplateRenderer(), currentUser.Object, audit.Object, NullLogger<NotificationService>.Instance);

        var deliveryService = new NotificationDeliveryService(
            db, emailMock.Object, notifService, NullLogger<NotificationDeliveryService>.Instance);

        // Simulate 4 transient failures -> Status remains Queued
        for (int i = 1; i < NotificationDeliveryService.MaxDeliveryAttempts; i++)
        {
            await deliveryService.DeliverNotificationAsync(notif.Id);
            var n = await db.Notifications.AsNoTracking().FirstAsync(x => x.Id == notif.Id);
            n.Status.Should().Be("Queued");
        }

        // 5th attempt -> MaxDeliveryAttempts exhausted -> Status becomes Failed
        await deliveryService.DeliverNotificationAsync(notif.Id);
        var finalNotif = await db.Notifications.Include(n => n.Deliveries).FirstAsync(x => x.Id == notif.Id);
        finalNotif.Status.Should().Be("Failed");
        finalNotif.Deliveries.Should().HaveCount(NotificationDeliveryService.MaxDeliveryAttempts);

        // Calling DeliverNotificationAsync on already Sent notification must not call provider again
        emailMock.Reset();
        finalNotif.Status = "Sent";
        finalNotif.Deliveries.Add(new NotificationDelivery { NotificationId = finalNotif.Id, Channel = "Email", AttemptNumber = 6, Status = "Success" });
        await db.SaveChangesAsync();

        await deliveryService.DeliverNotificationAsync(notif.Id);
        emailMock.Verify(p => p.SendAsync(It.IsAny<EmailSendRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void RedactEmail_MasksLocalPartSafely()
    {
        ConfigurableEmailProvider.RedactEmail("alice.smith@company.com").Should().Be("a***@company.com");
        ConfigurableEmailProvider.SanitizeGatewayError("Error with Bearer my-secret-jwt and SG.abc123")
            .Should().NotContain("SG.abc123")
            .And.Contain("[REDACTED]");
    }
}
