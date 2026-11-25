using FluentAssertions;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Infrastructure.Services;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Notification;

public class NotificationTemplateRendererTests
{
    private readonly NotificationTemplateRenderer _renderer = new();

    [Fact]
    public void Render_ValidApprovedPlaceholders_SubstitutesCorrectly()
    {
        var approved = new[] { "CustomerName", "OrderNumber", "DeliveryStatus" };
        var parameters = new Dictionary<string, string>
        {
            ["CustomerName"] = "Acme Corp",
            ["OrderNumber"] = "ORD-202611-0001",
            ["DeliveryStatus"] = "Delivered"
        };

        var result = _renderer.Render(
            subjectTemplate: "Order {{OrderNumber}} - {{DeliveryStatus}}",
            bodyTemplate: "Dear {{CustomerName}}, your order {{OrderNumber}} is now {{DeliveryStatus}}.",
            approvedPlaceholders: approved,
            parameters: parameters,
            htmlEscapeBody: false);

        result.Subject.Should().Be("Order ORD-202611-0001 - Delivered");
        result.Body.Should().Be("Dear Acme Corp, your order ORD-202611-0001 is now Delivered.");
        result.ResolvedPlaceholders.Should().BeEquivalentTo(["CustomerName", "DeliveryStatus", "OrderNumber"]);
    }

    [Fact]
    public void Render_HtmlEscapeBodyTrue_EscapesUntrustedHtml()
    {
        var parameters = new Dictionary<string, string>
        {
            ["CustomerName"] = "<script>alert('xss')</script> & Co."
        };

        var result = _renderer.Render(
            subjectTemplate: "Welcome {{CustomerName}}",
            bodyTemplate: "<p>Hello {{CustomerName}}</p>",
            approvedPlaceholders: ["CustomerName"],
            parameters: parameters,
            htmlEscapeBody: true);

        result.Body.Should().Be("<p>Hello &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; &amp; Co.</p>");
        result.Body.Should().NotContain("<script>");
    }

    [Fact]
    public void Render_MissingRequiredPlaceholder_ThrowsValidationException()
    {
        var act = () => _renderer.Render(
            subjectTemplate: "Waybill {{TrackingNumber}}",
            bodyTemplate: "Shipment {{TrackingNumber}} for {{CustomerName}}",
            approvedPlaceholders: ["TrackingNumber", "CustomerName"],
            parameters: new Dictionary<string, string> { ["TrackingNumber"] = "WB-001" },
            htmlEscapeBody: false);

        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainKey("TemplateParameters");
    }

    [Fact]
    public void Render_UnapprovedPlaceholder_ThrowsValidationException()
    {
        var act = () => _renderer.Render(
            subjectTemplate: "Alert",
            bodyTemplate: "Secret key: {{InternalSecretKey}}",
            approvedPlaceholders: ["OrderNumber", "CustomerName"],
            parameters: new Dictionary<string, string> { ["InternalSecretKey"] = "XYZ" },
            htmlEscapeBody: false);

        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("Hello <script>bad()</script>")]
    [InlineData("Click javascript:alert(1)")]
    [InlineData("Eval ${1+1}")]
    public void Render_DisallowedExpressionOrScript_ThrowsValidationException(string maliciousBody)
    {
        var act = () => _renderer.Render(
            subjectTemplate: "Alert",
            bodyTemplate: maliciousBody,
            approvedPlaceholders: null,
            parameters: null,
            htmlEscapeBody: true);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CronScheduleHelper_ValidCronWithTimezone_CalculatesNextUtc()
    {
        var fromUtc = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
        // Every day at 08:30 AM Asia/Bangkok (UTC+7 -> 01:30 UTC)
        var nextUtc = CronScheduleHelper.CalculateNextOccurrenceUtc("30 8 * * *", "SE Asia Standard Time", fromUtc);

        nextUtc.Should().Be(new DateTime(2026, 11, 25, 1, 30, 0, DateTimeKind.Utc));
    }
}
