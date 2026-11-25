using System.Text;
using FluentAssertions;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Validators;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Reporting;

public class ReportingValidatorAndExportTests
{
    [Fact]
    public void ReportFilterRequestValidator_ValidRequest_Passes()
    {
        var validator = new ReportFilterRequestValidator();
        var req = new ReportFilterRequest
        {
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 30),
            TimeZoneId = "SE Asia Standard Time",
            Search = "SGN-001"
        };

        validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReportFilterRequestValidator_EndDateBeforeStartDate_Fails()
    {
        var validator = new ReportFilterRequestValidator();
        var req = new ReportFilterRequest
        {
            StartDate = new DateOnly(2026, 5, 10),
            EndDate = new DateOnly(2026, 5, 1)
        };

        var result = validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public void ReportFilterRequestValidator_Exceeding366Days_Fails()
    {
        var validator = new ReportFilterRequestValidator();
        var req = new ReportFilterRequest
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2026, 3, 1)
        };

        var result = validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("366"));
    }

    [Fact]
    public void ReportFilterRequestValidator_InvalidTimeZone_Fails()
    {
        var validator = new ReportFilterRequestValidator();
        var req = new ReportFilterRequest
        {
            TimeZoneId = "Not/ARealTimezone"
        };

        var result = validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TimeZoneId");
    }

    [Fact]
    public void ReportingQueryHelpers_ResolveTimeWindow_ComputesHalfOpenUtcBounds()
    {
        var filter = new ReportFilterRequest
        {
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 1),
            TimeZoneId = "SE Asia Standard Time"
        };

        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);
        window.UtcFromInclusive.Should().Be(new DateTime(2026, 3, 31, 17, 0, 0, DateTimeKind.Utc));
        window.UtcToExclusive.Should().Be(new DateTime(2026, 4, 1, 17, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ReportingQueryHelpers_ResolveTimeWindow_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var filter = new ReportFilterRequest
        {
            StartDate = new DateOnly(2026, 4, 5),
            EndDate = new DateOnly(2026, 4, 1)
        };

        Action act = () => ReportingQueryHelpers.ResolveTimeWindow(filter);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ReportingQueryHelpers_BuildCsv_IncludesUtf8Bom_AndNeutralizesFormulaInjection()
    {
        var rows = new[]
        {
            new { Name = "=CMD|'/C calc'!A0", Note = "+123, \"quoted\"", Code = "@SUM(A1)" },
            new { Name = "-456", Note = "Normal text", Code = "\tTAB" }
        };

        var bytes = ReportingQueryHelpers.BuildCsv(rows, [
            ("Name", r => r.Name),
            ("Note", r => r.Note),
            ("Code", r => r.Code)
        ]);

        // Verify UTF-8 BOM
        bytes.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());

        var csv = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        csv.Should().Contain("'=CMD|'/C calc'!A0");
        csv.Should().Contain("\"\'+123, \"\"quoted\"\"\"");
        csv.Should().Contain("'@SUM(A1)");
        csv.Should().Contain("'-456");
        csv.Should().Contain("'\tTAB");
    }

    [Fact]
    public void ReportingQueryHelpers_SanitizeFileName_StripsPathTraversalAndInvalidChars()
    {
        var clean = ReportingQueryHelpers.SanitizeFileName("../../orders:export?.csv");
        clean.Should().NotContain("/");
        clean.Should().NotContain("\\");
        clean.Should().NotContain(":");
        clean.Should().NotContain("?");
        clean.Should().EndWith(".csv");
    }

    [Fact]
    public async Task ReportingExecutionEngine_PdfAndUnknown_ReturnUnsupported_WhileCsvGeneratesRealPayload()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("RptEngineTest_" + Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new ApplicationDbContext(options);
        var company = new Company { Code = "CMP", LegalName = "Co", TradingName = "Co", TaxNumber = "001", IsActive = true };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var b1 = new Branch { CompanyId = company.Id, Code = "SGN", Name = "Saigon", BranchType = "Hub", Phone = "111", IsActive = true };
        var b2 = new Branch { CompanyId = company.Id, Code = "HAN", Name = "Hanoi", BranchType = "Hub", Phone = "222", IsActive = true };
        context.Branches.AddRange(b1, b2);
        await context.SaveChangesAsync();

        var c1 = new Customer { BranchId = b1.Id, CustomerCode = "C1", Name = "Saigon Shop", Email = "s@t.vn", PhoneNumber = "1", IsActive = true };
        var c2 = new Customer { BranchId = b2.Id, CustomerCode = "C2", Name = "Hanoi Shop", Email = "h@t.vn", PhoneNumber = "2", IsActive = true };
        context.Customers.AddRange(c1, c2);
        await context.SaveChangesAsync();

        context.Orders.AddRange(
            new Order { OrderNumber = "ORD-SGN-1", BranchId = b1.Id, CustomerId = c1.Id, OrderDate = DateTime.UtcNow, OrderStatus = "Confirmed", PaymentStatus = "Pending", DeclaredValue = 100m, CodAmount = 50m },
            new Order { OrderNumber = "ORD-HAN-1", BranchId = b2.Id, CustomerId = c2.Id, OrderDate = DateTime.UtcNow, OrderStatus = "Confirmed", PaymentStatus = "Pending", DeclaredValue = 200m, CodAmount = 75m });
        await context.SaveChangesAsync();

        var engine = new ReportingExecutionEngine(context);

        var pdfResult = await engine.GenerateAsync("RPT_ORDERS", "SELECT 1", "Pdf", b1.Id);
        pdfResult.IsSupported.Should().BeFalse();
        pdfResult.Succeeded.Should().BeFalse();

        var unknownResult = await engine.GenerateAsync("RPT_DOES_NOT_EXIST", "SELECT 1", "Csv", b1.Id);
        unknownResult.IsSupported.Should().BeFalse();
        unknownResult.Succeeded.Should().BeFalse();

        var csvResult = await engine.GenerateAsync("RPT_ORDERS", "SELECT 1", "Csv", b1.Id);
        csvResult.IsSupported.Should().BeTrue();
        csvResult.Succeeded.Should().BeTrue();
        csvResult.ContentType.Should().Contain("text/csv");
        csvResult.FileName.Should().StartWith("rpt_orders-").And.EndWith(".csv");

        var csvText = Encoding.UTF8.GetString(csvResult.FileBytes!.Skip(3).ToArray());
        csvText.Should().Contain("ORD-SGN-1");
        csvText.Should().NotContain("ORD-HAN-1");
    }
}
