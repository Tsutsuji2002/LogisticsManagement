using System.Security.Claims;

namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Abstraction for generating, validating, and hashing authentication tokens (JWT access tokens and refresh tokens).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token containing standard identity and security claims.
    /// </summary>
    string GenerateAccessToken(
        long userId,
        Guid publicId,
        string username,
        string email,
        string securityStamp,
        long? employeeId,
        long? defaultBranchId,
        IReadOnlyList<long> accessibleBranchIds,
        IReadOnlyList<string> roles,
        out DateTime expiresAt);

    /// <summary>
    /// Generates a cryptographically secure random refresh token string.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Generates a SHA-256 hash of a raw token for secure database persistence.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Validates an access token and extracts the claims principal (even if expired, for token refresh/revocation workflows).
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false);
}
