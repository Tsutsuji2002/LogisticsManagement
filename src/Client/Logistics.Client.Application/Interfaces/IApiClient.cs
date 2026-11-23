namespace Logistics.Client.Application.Interfaces;

/// <summary>
/// Abstraction for HTTP communication between the WinForms client and backend API.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Sends an HTTP GET request to the specified endpoint.
    /// </summary>
    Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP POST request with a payload to the specified endpoint.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP PUT request with a payload to the specified endpoint.
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP DELETE request to the specified endpoint.
    /// </summary>
    Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
