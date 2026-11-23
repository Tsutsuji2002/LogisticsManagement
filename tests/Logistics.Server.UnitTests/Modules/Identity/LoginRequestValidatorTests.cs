using FluentAssertions;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Identity;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void LoginRequestValidator_WithValidCredentials_ShouldPass()
    {
        var request = new LoginRequest("admin_user", "P@ssword123!");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "P@ssword123!")]
    [InlineData("admin_user", "")]
    public void LoginRequestValidator_WithMissingFields_ShouldFail(string username, string password)
    {
        var request = new LoginRequest(username, password);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
