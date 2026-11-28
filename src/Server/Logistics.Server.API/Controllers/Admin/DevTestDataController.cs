using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.DevData;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Admin;

/// <summary>
/// Development-only endpoints for inspecting, seeding, resetting, and verifying test data.
/// Blocked in all non-Development environments and whenever DevSeed:Enabled is false.
/// </summary>
[Route("api/v1/dev/test-data")]
public class DevTestDataController : BaseApiController
{
    private readonly IDevTestDataService _devTestDataService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevTestDataController> _logger;

    public DevTestDataController(
        IDevTestDataService devTestDataService,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DevTestDataController> logger)
    {
        _devTestDataService = devTestDataService;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    private IActionResult? EnsureDevelopmentGuard()
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Blocked access to DevTestData endpoint in environment '{Env}'.", _environment.EnvironmentName);
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Fail($"Dev Test Data is strictly disabled in '{_environment.EnvironmentName}' environment.", errorCode: "DEV_ONLY_FEATURE"));
        }

        if (!_configuration.GetValue<bool>("DevSeed:Enabled"))
        {
            _logger.LogWarning("Blocked access to DevTestData endpoint because DevSeed:Enabled is false.");
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Fail("Dev Test Data is disabled by server configuration (DevSeed:Enabled=false).", errorCode: "DEV_SEED_DISABLED"));
        }

        return null;
    }

    [HttpGet("status")]
    [HasPermission(AppPermissions.Configuration.SettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<DevTestDataStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        if (EnsureDevelopmentGuard() is { } blocked) return blocked;
        var status = await _devTestDataService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<DevTestDataStatusDto>.Ok(status));
    }

    [HttpPost("execute")]
    [HasPermission(AppPermissions.Configuration.SettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<ExecuteDevSeedResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Execute([FromBody] ExecuteDevSeedRequest request, CancellationToken cancellationToken)
    {
        if (EnsureDevelopmentGuard() is { } blocked) return blocked;
        var result = await _devTestDataService.ExecuteAsync(request, cancellationToken);
        return Ok(ApiResponse<ExecuteDevSeedResultDto>.Ok(result, result.Summary));
    }

    [HttpPost("verify")]
    [HasPermission(AppPermissions.Configuration.SettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<DevTestDataIntegrityReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> VerifyIntegrity(CancellationToken cancellationToken)
    {
        if (EnsureDevelopmentGuard() is { } blocked) return blocked;
        var report = await _devTestDataService.VerifyIntegrityAsync(cancellationToken);
        return Ok(ApiResponse<DevTestDataIntegrityReportDto>.Ok(report));
    }
}
