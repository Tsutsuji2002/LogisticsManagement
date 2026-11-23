using System.Security.Claims;
using FluentAssertions;
using Logistics.Server.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Logistics.Server.UnitTests.Security;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "A_SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_EXCEEDING_32_BYTES!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpirationMinutes"] = "30"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidJwtWithClaims()
    {
        var userId = 1001L;
        var publicId = Guid.NewGuid();
        var username = "logistics_admin";
        var email = "admin@logistics.vn";
        var securityStamp = Guid.NewGuid().ToString("N");
        var employeeId = 55L;
        var defaultBranchId = 10L;
        var branchIds = new List<long> { 10L, 20L };
        var roles = new List<string> { "SUPER_ADMIN", "BRANCH_MANAGER" };

        var token = _tokenService.GenerateAccessToken(
            userId, publicId, username, email, securityStamp,
            employeeId, defaultBranchId, branchIds, roles, out var expiresAt);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        // Validate token extraction
        var principal = _tokenService.GetPrincipalFromToken(token, validateLifetime: true);
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("1001");
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(username);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(email);
        principal.FindFirst("employee_id")?.Value.Should().Be("55");
        principal.FindFirst("branch_id")?.Value.Should().Be("10");
        principal.FindAll("branch_access").Select(c => c.Value).Should().BeEquivalentTo(new[] { "10", "20" });
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceHighEntropyBase64String()
    {
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
        token1.Length.Should().BeGreaterThanOrEqualTo(40);
    }

    [Fact]
    public void HashToken_ShouldBeDeterministicAndHexEncoded()
    {
        var rawToken = "my-secret-refresh-token-123456";
        var hash1 = _tokenService.HashToken(rawToken);
        var hash2 = _tokenService.HashToken(rawToken);

        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64); // SHA-256 in hex
    }
}
