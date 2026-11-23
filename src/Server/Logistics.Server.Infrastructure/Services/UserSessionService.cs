using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Service for querying and revoking user login sessions.
/// </summary>
public class UserSessionService : IUserSessionService
{
    private readonly ApplicationDbContext _context;

    public UserSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsTerminated && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        // Mark the most recent session as current
        var first = true;
        var result = new List<UserSessionDto>(sessions.Count);

        foreach (var s in sessions)
        {
            result.Add(new UserSessionDto(
                Id: s.Id,
                DeviceInfo: s.DeviceInfo,
                IpAddress: s.IpAddress,
                StartedAt: s.StartedAt,
                LastActivityAt: s.LastActivityAt,
                ExpiresAt: s.ExpiresAt,
                IsCurrentSession: first
            ));
            first = false;
        }

        return result;
    }

    public async Task TerminateSessionAsync(long userId, long sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("UserSession", sessionId);
        }

        session.IsTerminated = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
