using System.Text;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services.Reporting;

/// <summary>
/// Shared helper for timezone-consistent date windows, branch access scoping, and formula-safe CSV generation.
/// </summary>
public static class ReportingQueryHelpers
{
    public const int MaxExportRows = 10000;

    public static ReportTimeWindow ResolveTimeWindow(ReportFilterRequest filter)
    {
        var tz = CronScheduleHelper.ResolveTimeZone(filter.TimeZoneId);

        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.EndDate < filter.StartDate)
            throw new ValidationException("EndDate", "EndDate must be greater than or equal to StartDate.");

        DateTime? utcFrom = null;
        DateTime? utcToExclusive = null;

        if (filter.StartDate.HasValue)
        {
            var localStart = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue);
            utcFrom = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), tz);
        }

        if (filter.EndDate.HasValue)
        {
            // Half-open upper bound: EndDate + 1 day 00:00:00 local -> UTC
            var localNextDay = filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            utcToExclusive = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localNextDay, DateTimeKind.Unspecified), tz);
        }

        return new ReportTimeWindow(utcFrom, utcToExclusive, filter.StartDate, filter.EndDate, tz.Id);
    }

    public static (bool IsGlobal, IReadOnlyList<long> AllowedBranches) ResolveBranchScope(
        ICurrentUserService currentUser,
        long? requestedBranchId)
    {
        var isGlobal = currentUser.IsInRole(AppRoles.SuperAdmin);

        if (requestedBranchId.HasValue)
        {
            if (!isGlobal && !currentUser.HasBranchAccess(requestedBranchId.Value))
            {
                throw new ForbiddenAccessException($"You do not have access to branch '{requestedBranchId.Value}'.");
            }

            return (false, [requestedBranchId.Value]);
        }

        if (isGlobal)
        {
            return (true, Array.Empty<long>());
        }

        return (false, currentUser.AccessibleBranchIds);
    }

    public static void ConfigureReadOnlyTimeout(ApplicationDbContext context, int seconds = 30)
    {
        if (context.Database.IsRelational())
        {
            context.Database.SetCommandTimeout(seconds);
        }
    }

    public static string BuildBranchCacheKey(string prefix, ICurrentUserService currentUser, ReportFilterRequest filter)
    {
        var isGlobal = currentUser.IsInRole(AppRoles.SuperAdmin);
        var scopePart = filter.BranchId.HasValue
            ? $"b:{filter.BranchId.Value}"
            : isGlobal
                ? "global"
                : $"bs:{string.Join(",", currentUser.AccessibleBranchIds.OrderBy(x => x))}";

        return $"rpt:{prefix}:{scopePart}:{filter.StartDate}:{filter.EndDate}:{filter.TimeZoneId}:{filter.CustomerId}:{filter.CarrierId}:{filter.WarehouseId}";
    }

    public static string SanitizeFileName(string name)
    {
        var fileName = Path.GetFileName(name);
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }
        return sb.Length > 0 ? sb.ToString() : "report.csv";
    }

    public static byte[] BuildCsv<T>(IReadOnlyList<T> rows, (string Header, Func<T, object?> Selector)[] columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsvCell(c.Header))));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsvCell(c.Selector(row)))));
        }

        // UTF-8 BOM + UTF-8 content for seamless Excel opening
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    private static string EscapeCsvCell(object? value)
    {
        if (value == null) return string.Empty;
        var str = value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            decimal dec => dec.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        // Prevent CSV formula injection (=, +, -, @, tab, CR)
        if (str.Length > 0 && (str[0] == '=' || str[0] == '+' || str[0] == '-' || str[0] == '@' || str[0] == '\t' || str[0] == '\r'))
        {
            str = "'" + str;
        }

        if (str.Contains(',') || str.Contains('"') || str.Contains('\n') || str.Contains('\r'))
        {
            str = $"\"{str.Replace("\"", "\"\"")}\"";
        }

        return str;
    }
}
