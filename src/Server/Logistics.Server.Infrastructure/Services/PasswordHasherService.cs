using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Password hashing service utilizing ASP.NET Core Identity's PBKDF2 with HMAC-SHA256,
/// salt generation, constant-time verification, and automatic rehash detection.
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<UserAccount> _hasher;
    private static readonly UserAccount DummyUser = new();

    public PasswordHasherService()
    {
        _hasher = new PasswordHasher<UserAccount>();
    }

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(DummyUser, password);
    }

    public (bool IsValid, bool NeedsRehash) VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
        return result switch
        {
            PasswordVerificationResult.Success => (true, false),
            PasswordVerificationResult.SuccessRehashNeeded => (true, true),
            _ => (false, false)
        };
    }
}
