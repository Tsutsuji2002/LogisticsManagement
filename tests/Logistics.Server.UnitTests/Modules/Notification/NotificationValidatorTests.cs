using FluentAssertions;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Validators;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Notification;

public class NotificationValidatorTests
{
    [Fact]
    public void CreateNotificationRequestValidator_ValidUserTarget_Passes()
    {
        var validator = new CreateNotificationRequestValidator();
        var request = new CreateNotificationRequest
        {
            RecipientUserId = 10,
            Title = "Shipment Dispatched",
            Content = "Waybill VN-001 has been dispatched.",
            Channel = "InApp",
            Priority = "Normal"
        };

        validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateNotificationRequestValidator_NoRecipient_Fails()
    {
        var validator = new CreateNotificationRequestValidator();
        var request = new CreateNotificationRequest
        {
            Title = "Test",
            Content = "Body",
            Channel = "InApp"
        };

        validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNotificationRequestValidator_InvalidChannel_Fails()
    {
        var validator = new CreateNotificationRequestValidator();
        var request = new CreateNotificationRequest
        {
            RecipientUserId = 1,
            Title = "Test",
            Content = "Body",
            Channel = "CarrierPigeon"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Theory]
    [InlineData("ORDER_CONFIRMED")]
    [InlineData("SHIPMENT_DELIVERED_V1")]
    public void CreateNotificationTemplateRequestValidator_ValidCode_Passes(string code)
    {
        var validator = new CreateNotificationTemplateRequestValidator();
        var req = new CreateNotificationTemplateRequest
        {
            Code = code,
            Name = "Test Template",
            Channel = "Email",
            SubjectTemplate = "Order {{OrderNumber}}",
            BodyTemplate = "Hello {{CustomerName}}"
        };

        validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("lowercase")]
    [InlineData("HAS-DASH")]
    [InlineData("")]
    public void CreateNotificationTemplateRequestValidator_InvalidCode_Fails(string code)
    {
        var validator = new CreateNotificationTemplateRequestValidator();
        var req = new CreateNotificationTemplateRequest
        {
            Code = code,
            Name = "Test",
            Channel = "InApp",
            BodyTemplate = "Body"
        };

        validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateScheduledReportRequestValidator_ValidCron_Passes()
    {
        var validator = new CreateScheduledReportRequestValidator();
        var req = new CreateScheduledReportRequest
        {
            ReportDefinitionId = 1,
            ScheduleCron = "0 6 * * 1-5",
            TimeZoneId = "UTC",
            OutputFormat = "Pdf",
            RecipientEmails = "ops@logistics.test"
        };

        validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateScheduledReportRequestValidator_InvalidCronFields_Fails()
    {
        var validator = new CreateScheduledReportRequestValidator();
        var req = new CreateScheduledReportRequest
        {
            ReportDefinitionId = 1,
            ScheduleCron = "0 6 *",
            OutputFormat = "Pdf",
            RecipientEmails = "ops@logistics.test"
        };

        validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EnqueueBackgroundJobRequestValidator_ValidRequest_Passes()
    {
        var validator = new EnqueueBackgroundJobRequestValidator();
        var req = new EnqueueBackgroundJobRequest
        {
            JobName = "SampleJob",
            MaxAttempts = 3
        };

        validator.Validate(req).IsValid.Should().BeTrue();
    }
}
