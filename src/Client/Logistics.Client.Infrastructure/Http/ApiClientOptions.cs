namespace Logistics.Client.Infrastructure.Http;

/// <summary>
/// Configuration options for HTTP communication with the Logistics Server API.
/// </summary>
public class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    /// <summary>
    /// Base address of the Logistics API server.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
