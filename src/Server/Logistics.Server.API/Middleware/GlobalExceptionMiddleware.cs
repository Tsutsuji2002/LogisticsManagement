using System.Net;
using System.Text.Json;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Domain.Exceptions;
using Logistics.Shared.Contracts;

namespace Logistics.Server.API.Middleware;

/// <summary>
/// Intercepts all unhandled exceptions and formats a consistent, secure API error response.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, errorCode, message, validationErrors) = exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_FAILED",
                valEx.Message,
                valEx.Errors
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                "RESOURCE_NOT_FOUND",
                notFoundEx.Message,
                null
            ),
            EntityNotFoundException entityNotFoundEx => (
                HttpStatusCode.NotFound,
                entityNotFoundEx.Code,
                entityNotFoundEx.Message,
                null
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                "RESOURCE_CONFLICT",
                conflictEx.Message,
                null
            ),
            ConcurrencyConflictException concurrencyEx => (
                HttpStatusCode.Conflict,
                concurrencyEx.Code,
                concurrencyEx.Message,
                null
            ),
            ForbiddenAccessException forbiddenEx => (
                HttpStatusCode.Forbidden,
                "FORBIDDEN",
                forbiddenEx.Message,
                null
            ),
            UnauthorizedAccessException unauthEx => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                string.IsNullOrWhiteSpace(unauthEx.Message)
                    ? "You are not authorized to perform this action."
                    : unauthEx.Message,
                null
            ),
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                domainEx.Code,
                domainEx.Message,
                null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected internal server error occurred. Please contact system support.",
                null
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "[{TraceId}] Unhandled server exception: {Message}", traceId, exception.Message);
        }
        else
        {
            _logger.LogWarning("[{TraceId}] Handled exception ({ErrorCode}): {Message}", traceId, errorCode, message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(
            message: message,
            errors: validationErrors == null ? [message] : null,
            errorCode: errorCode,
            traceId: traceId,
            validationErrors: validationErrors
        );

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
