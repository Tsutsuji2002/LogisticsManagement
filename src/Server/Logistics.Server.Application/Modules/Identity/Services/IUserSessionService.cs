using Logistics.Server.Application.Modules.Identity.DTOs;

namespace Logistics.Server.Application.Modules.Identity.Services;

/// <summary>
/// Service contract for managing user sessions and terminations.
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Gets all active login sessions for the specified user.
    /// </summary>
    Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session belonging to the user.
    /// </summary>
    Task TerminateSessionAsync(
        long userId,
        long sessionId,
        CancellationToken cancellationToken = default);
}
