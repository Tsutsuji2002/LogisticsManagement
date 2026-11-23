using System.Text.Json;
using System.Text.Json.Serialization;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Domain.Entities.Audit;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Persists immutable audit records to the common.AuditLogs table with HTTP context correlation.
/// </summary>
public class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordAsync(
        string action,
        string schemaName,
        string tableName,
        string primaryKey,
        object? oldValues,
        object? newValues,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var username = _currentUserService.Username ?? "System";
        var userRole = _currentUserService.Roles.FirstOrDefault();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var correlationId = httpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Username = username,
            UserRole = userRole,
            Action = action,
            SchemaName = schemaName,
            TableName = tableName,
            PrimaryKey = primaryKey,
            OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues, SerializerOptions) : null,
            NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues, SerializerOptions) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(audit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
