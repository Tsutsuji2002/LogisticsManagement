using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Logistics.Server.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Cryptographic token generator and validator for JWT access tokens and secure random refresh tokens.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret configuration is missing.");
        _signingKey = Encoding.UTF8.GetBytes(secret);
        _issuer = jwtSection["Issuer"] ?? "LogisticsManagement";
        _audience = jwtSection["Audience"] ?? "LogisticsManagement.Client";
        _accessTokenExpirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var mins) ? mins : 15;
    }

    public string GenerateAccessToken(
        long userId,
        Guid publicId,
        string username,
        string email,
        string securityStamp,
        long? employeeId,
        long? defaultBranchId,
        IReadOnlyList<long> accessibleBranchIds,
        IReadOnlyList<string> roles,
        out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("public_id", publicId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new("security_stamp", securityStamp)
        };

        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        }

        if (defaultBranchId.HasValue)
        {
            claims.Add(new Claim("branch_id", defaultBranchId.Value.ToString()));
        }

        foreach (var branchId in accessibleBranchIds)
        {
            claims.Add(new Claim("branch_access", branchId.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_signingKey),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_signingKey),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
