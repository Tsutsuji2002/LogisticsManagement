using FluentAssertions;
using Logistics.Server.Infrastructure.Services;
using Xunit;

namespace Logistics.Server.UnitTests.Security;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePasswordDueToSalting()
    {
        var password = "SecurePassword123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyHashedPassword_WithCorrectPassword_ShouldReturnValid()
    {
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.HashPassword(password);

        var (isValid, _) = _hasher.VerifyHashedPassword(hash, password);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHashedPassword_WithIncorrectPassword_ShouldReturnInvalid()
    {
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.HashPassword(password);

        var (isValid, _) = _hasher.VerifyHashedPassword(hash, "WrongPassword456!");

        isValid.Should().BeFalse();
    }
}
