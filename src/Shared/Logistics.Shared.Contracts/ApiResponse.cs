namespace Logistics.Shared.Contracts;

/// <summary>
/// Standard API response wrapper for endpoints returning data.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(
        string message,
        IReadOnlyList<string>? errors = null,
        string? errorCode = null,
        string? traceId = null,
        IDictionary<string, string[]>? validationErrors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            ErrorCode = errorCode,
            TraceId = traceId,
            ValidationErrors = validationErrors
        };
}

/// <summary>
/// Standard API response wrapper for operations that do not return data.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }

    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(
        string message,
        IReadOnlyList<string>? errors = null,
        string? errorCode = null,
        string? traceId = null,
        IDictionary<string, string[]>? validationErrors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            ErrorCode = errorCode,
            TraceId = traceId,
            ValidationErrors = validationErrors
        };
}
