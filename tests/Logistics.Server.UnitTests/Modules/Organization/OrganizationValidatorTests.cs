using FluentAssertions;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Organization;

public class OrganizationValidatorTests
{
    private readonly CreateCompanyRequestValidator _companyValidator = new();
    private readonly CreateBranchRequestValidator _branchValidator = new();

    [Fact]
    public void CreateCompanyRequestValidator_WithValidData_ShouldPass()
    {
        var validRequest = new CreateCompanyRequest(
            Code: "COMP-001",
            LegalName: "Test Logistics Joint Stock Co.",
            TradingName: "Test Express",
            TaxNumber: "0108889999",
            Email: "contact@testexpress.vn",
            Phone: "+84-24-11223344"
        );

        var result = _companyValidator.Validate(validRequest);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Company code is required.")]
    [InlineData(null, "Company code is required.")]
    public void CreateCompanyRequestValidator_WithEmptyCode_ShouldFail(string? code, string expectedMessage)
    {
        var request = new CreateCompanyRequest(
            Code: code!,
            LegalName: "Test Logistics Co.",
            TradingName: "Test Express",
            TaxNumber: "0108889999",
            Email: "contact@testexpress.vn",
            Phone: "+84-24-11223344"
        );

        var result = _companyValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorMessage == expectedMessage);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("notanemail.com")]
    public void CreateCompanyRequestValidator_WithInvalidEmail_ShouldFail(string email)
    {
        var request = new CreateCompanyRequest(
            Code: "COMP-001",
            LegalName: "Test Logistics Co.",
            TradingName: "Test Express",
            TaxNumber: "0108889999",
            Email: email,
            Phone: "+84-24-11223344"
        );

        var result = _companyValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void CreateBranchRequestValidator_WithValidData_ShouldPass()
    {
        var validRequest = new CreateBranchRequest(
            CompanyId: 1,
            Code: "BR-HANOI-01",
            Name: "Hanoi Central Hub",
            BranchType: "SortingHub",
            Phone: "+84-24-99887766"
        );

        var result = _branchValidator.Validate(validRequest);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateBranchRequestValidator_WithInvalidCompanyId_ShouldFail(long companyId)
    {
        var request = new CreateBranchRequest(
            CompanyId: companyId,
            Code: "BR-01",
            Name: "Branch",
            BranchType: "Office",
            Phone: "+84-123456"
        );

        var result = _branchValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
    }

    [Fact]
    public void UpdateCompanyRequestValidator_WithValidData_ShouldPass()
    {
        var validator = new UpdateCompanyRequestValidator();
        var request = new UpdateCompanyRequest("Legal Co", "Trade Co", "1234567890", "info@company.com", "0123456789");
        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateDepartmentRequestValidator_WithInvalidBranchId_ShouldFail()
    {
        var validator = new CreateDepartmentRequestValidator();
        var request = new CreateDepartmentRequest(0, "DEP-01", "Dispatch");
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BranchId");
    }

    [Fact]
    public void CreateEmployeeRequestValidator_WithValidData_ShouldPass()
    {
        var validator = new CreateEmployeeRequestValidator();
        var request = new CreateEmployeeRequest(
            BranchId: 1,
            DepartmentId: 2,
            Code: "EMP-001",
            FirstName: "Nguyen",
            LastName: "Van A",
            Email: "a.nguyen@logistics.vn",
            PhoneNumber: "+84988776655",
            DateOfBirth: new DateOnly(1990, 5, 12),
            Gender: "Male",
            JobTitle: "Courier Driver",
            HireDate: new DateOnly(2023, 1, 15)
        );

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEmployeeRequestValidator_WithInvalidGender_ShouldFail()
    {
        var validator = new CreateEmployeeRequestValidator();
        var request = new CreateEmployeeRequest(
            BranchId: 1,
            DepartmentId: null,
            Code: "EMP-001",
            FirstName: "Nguyen",
            LastName: "Van A",
            Email: "a.nguyen@logistics.vn",
            PhoneNumber: "+84988776655",
            DateOfBirth: null,
            Gender: "InvalidGender",
            JobTitle: "Courier",
            HireDate: new DateOnly(2023, 1, 15)
        );

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Gender");
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("Inactive", true)]
    [InlineData("OnLeave", true)]
    [InlineData("Resigned", true)]
    [InlineData("Terminated", true)]
    [InlineData("UnknownStatus", false)]
    public void UpdateEmployeeStatusRequestValidator_ShouldEnforceAllowedStatuses(string status, bool expectedValid)
    {
        var validator = new UpdateEmployeeStatusRequestValidator();
        var request = new UpdateEmployeeStatusRequest(status, "Status updated by HR");
        var result = validator.Validate(request);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void CreateEmployeeAccountRequestValidator_WithWeakPassword_ShouldFail()
    {
        var validator = new CreateEmployeeAccountRequestValidator();
        var request = new CreateEmployeeAccountRequest("testuser", "Password123456", ["BRANCH_MANAGER"]);
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void CreateEmployeeAccountRequestValidator_WithNoRoles_ShouldFail()
    {
        var validator = new CreateEmployeeAccountRequestValidator();
        var request = new CreateEmployeeAccountRequest("testuser", "StrongSecureP@ssword2026!", []);
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleCodes");
    }
}
