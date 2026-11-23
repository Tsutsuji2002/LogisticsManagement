using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Health;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Health check endpoint for monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _context;

    public HealthController(IWebHostEnvironment environment, ApplicationDbContext context)
    {
        _environment = environment;
        _context = context;
    }

    /// <summary>
    /// Returns the health status of the API.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        bool canConnect;
        try
        {
            canConnect = await _context.Database.CanConnectAsync();
        }
        catch
        {
            canConnect = false;
        }

        var response = new HealthCheckResponse
        {
            Status = canConnect ? "Healthy" : "Degraded",
            Environment = _environment.EnvironmentName,
            Timestamp = DateTime.UtcNow,
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };

        return Ok(ApiResponse<HealthCheckResponse>.Ok(response));
    }
}
