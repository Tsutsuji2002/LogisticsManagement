using System.Net;

namespace Logistics.Client.Infrastructure.Exceptions;

/// <summary>
/// Represents an HTTP, network, timeout, or API-level validation/business error returned to the client.
/// Never contains raw authentication tokens or credentials.
/// </summary>
public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }
    public string? TraceId { get; }
    public IReadOnlyList<string> Errors { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }
    public bool IsNetworkError { get; }
    public bool IsTimeout { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;

    public ApiException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        string? errorCode = null,
        string? traceId = null,
        IReadOnlyList<string>? errors = null,
        IDictionary<string, string[]>? validationErrors = null,
        bool isNetworkError = false,
        bool isTimeout = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        TraceId = traceId;
        Errors = errors ?? Array.Empty<string>();
        ValidationErrors = validationErrors;
        IsNetworkError = isNetworkError;
        IsTimeout = isTimeout;
    }
}
