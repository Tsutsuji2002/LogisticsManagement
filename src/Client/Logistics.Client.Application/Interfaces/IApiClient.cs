namespace Logistics.Client.Application.Interfaces;

/// <summary>
/// Abstraction for HTTP communication between the WinForms client and backend API.
/// </summary>
public interface IApiClient
{
    Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    Task<TResponse?> PostEmptyAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        => PostAsync<object?, TResponse>(endpoint, null, cancellationToken);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        => PutAsync<TRequest, TResponse>(endpoint, request, cancellationToken);

    Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);

    Task<(byte[] Bytes, string ContentType, string FileName)> GetFileAsync(string endpoint, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("File download is not implemented by this transport.");
}
