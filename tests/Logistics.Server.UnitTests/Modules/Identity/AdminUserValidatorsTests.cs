using FluentAssertions;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Identity;

public class AdminUserValidatorsTests
{
    private readonly CreateUserRequestValidator _createValidator = new();
    private readonly UpdateUserRequestValidator _updateValidator = new();

    [Fact]
    public void CreateUserRequestValidator_WithValidPayload_ShouldPass()
    {
        var request = new CreateUserRequest(
            Username: "john_doe",
            Email: "john.doe@logistics.vn",
            Password: "SecurePassword2026!"
        );

        var result = _createValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("a")] // Too short
    [InlineData("invalid user with spaces")] // Invalid chars
    public void CreateUserRequestValidator_WithInvalidUsername_ShouldFail(string username)
    {
        var request = new CreateUserRequest(
            Username: username,
            Email: "valid@logistics.vn",
            Password: "SecurePassword2026!"
        );

        var result = _createValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    public void UpdateUserRequestValidator_WithInvalidEmail_ShouldFail(string email)
    {
        var request = new UpdateUserRequest(Email: email);
        var result = _updateValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
