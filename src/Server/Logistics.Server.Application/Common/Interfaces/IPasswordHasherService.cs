namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Abstraction for secure, adaptive password hashing and verification.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Creates a secure cryptographically salted hash of the plaintext password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that the provided plaintext password matches the stored hash using constant-time comparison.
    /// </summary>
    (bool IsValid, bool NeedsRehash) VerifyHashedPassword(string hashedPassword, string providedPassword);
}
