namespace Logistics.Shared.Contracts.Health;

/// <summary>
/// Response model for the health check endpoint.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; init; } = "Healthy";
    public string Environment { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Version { get; init; } = string.Empty;
}
