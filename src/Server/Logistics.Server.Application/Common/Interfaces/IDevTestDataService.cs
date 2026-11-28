using Logistics.Shared.Contracts.DevData;

namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Development-only test data seeding, inspection, and integrity verification service.
/// </summary>
public interface IDevTestDataService
{
    Task<DevTestDataStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<ExecuteDevSeedResultDto> ExecuteAsync(ExecuteDevSeedRequest request, CancellationToken ct = default);
    Task<DevTestDataIntegrityReportDto> VerifyIntegrityAsync(CancellationToken ct = default);
}
