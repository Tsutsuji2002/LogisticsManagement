using FluentAssertions;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Identity;

public class AuthValidatorsTests
{
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly RefreshTokenRequestValidator _refreshValidator = new();
    private readonly ChangePasswordRequestValidator _changePasswordValidator = new();

    [Fact]
    public void LoginRequestValidator_ShouldFailWhenEmpty()
    {
        var result = _loginValidator.Validate(new LoginRequest("", ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UsernameOrEmail");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void RefreshTokenRequestValidator_ShouldFailWhenEmpty()
    {
        var result = _refreshValidator.Validate(new RefreshTokenRequest(""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }

    [Theory]
    [InlineData("ValidCurrentPassword123!", "short")] // Too short new password
    [InlineData("ValidCurrentPassword123!", "Password12345!")] // Disallowed common
    [InlineData("OldPassword123!", "OldPassword123!")] // Same as current
    public void ChangePasswordRequestValidator_ShouldEnforcePolicy(string current, string newPass)
    {
        var result = _changePasswordValidator.Validate(new ChangePasswordRequest(current, newPass));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangePasswordRequestValidator_WithValidInput_ShouldPass()
    {
        var result = _changePasswordValidator.Validate(new ChangePasswordRequest("OldSecurePassword123!", "NewUniqueStrongPassword2026!"));
        result.IsValid.Should().BeTrue();
    }
}
