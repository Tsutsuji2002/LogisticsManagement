using System.Collections.Concurrent;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

public class NumberSequenceService : INumberSequenceService
{
    private readonly ApplicationDbContext _context;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    public NumberSequenceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextNumberAsync(string scope, CancellationToken cancellationToken = default)
    {
        var normalizedScope = scope.Trim().ToUpperInvariant();
        var semaphore = Locks.GetOrAdd(normalizedScope, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var sequence = await _context.NumberSequences
                .FirstOrDefaultAsync(s => s.Scope.ToUpper() == normalizedScope, cancellationToken);

            if (sequence == null)
            {
                var defaultPrefix = normalizedScope.Length >= 3 ? normalizedScope[..3] : normalizedScope;
                sequence = new NumberSequence
                {
                    Scope = normalizedScope,
                    Prefix = defaultPrefix,
                    NextNumber = 2,
                    Increment = 1,
                    NumberLength = 6,
                    CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}",
                    ResetFrequency = "Monthly",
                    LastResetDate = today
                };

                await _context.NumberSequences.AddAsync(sequence, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return FormatNumber(sequence, 1, now);
            }

            // Check reset frequency
            var shouldReset = false;
            if (string.Equals(sequence.ResetFrequency, "Monthly", StringComparison.OrdinalIgnoreCase))
            {
                shouldReset = !sequence.LastResetDate.HasValue ||
                              sequence.LastResetDate.Value.Year != today.Year ||
                              sequence.LastResetDate.Value.Month != today.Month;
            }
            else if (string.Equals(sequence.ResetFrequency, "Daily", StringComparison.OrdinalIgnoreCase))
            {
                shouldReset = !sequence.LastResetDate.HasValue ||
                              sequence.LastResetDate.Value != today;
            }
            else if (string.Equals(sequence.ResetFrequency, "Yearly", StringComparison.OrdinalIgnoreCase))
            {
                shouldReset = !sequence.LastResetDate.HasValue ||
                              sequence.LastResetDate.Value.Year != today.Year;
            }

            if (shouldReset)
            {
                sequence.NextNumber = 1;
                sequence.LastResetDate = today;
            }

            var currentNumber = sequence.NextNumber;
            sequence.NextNumber += sequence.Increment > 0 ? sequence.Increment : 1;
            sequence.LastResetDate ??= today;

            await _context.SaveChangesAsync(cancellationToken);

            return FormatNumber(sequence, currentNumber, now);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string FormatNumber(NumberSequence sequence, long number, DateTime timestamp)
    {
        var format = string.IsNullOrWhiteSpace(sequence.CurrentFormat)
            ? "{PREFIX}-{YYYYMM}-{NUMBER}"
            : sequence.CurrentFormat;

        var paddedNumber = number.ToString().PadLeft(sequence.NumberLength > 0 ? sequence.NumberLength : 6, '0');

        return format
            .Replace("{PREFIX}", sequence.Prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{YYYYMM}", timestamp.ToString("yyyyMM"), StringComparison.OrdinalIgnoreCase)
            .Replace("{YYYY}", timestamp.ToString("yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{MM}", timestamp.ToString("MM"), StringComparison.OrdinalIgnoreCase)
            .Replace("{DD}", timestamp.ToString("dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{NUMBER}", paddedNumber, StringComparison.OrdinalIgnoreCase)
            .Replace("{SUFFIX}", sequence.Suffix ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
