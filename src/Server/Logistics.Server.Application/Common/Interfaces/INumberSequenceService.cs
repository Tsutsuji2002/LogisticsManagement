namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Service contract for generating thread-safe, concurrency-controlled sequential human-readable codes.
/// </summary>
public interface INumberSequenceService
{
    /// <summary>
    /// Generates the next sequential formatted reference code for the specified scope.
    /// </summary>
    /// <param name="scope">Scope identifier (e.g., ORDER, WAYBILL, INVOICE, RECEIPT, DISPATCH).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted unique sequence number.</returns>
    Task<string> GetNextNumberAsync(string scope, CancellationToken cancellationToken = default);
}
