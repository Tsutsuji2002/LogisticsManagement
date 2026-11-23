using System.Net;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Base API controller providing standardized response wrappers and HTTP status helpers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected string TraceId => HttpContext.TraceIdentifier;

    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse<PagedResponse<T>>> PagedOkResponse<T>(PagedResponse<T> data)
    {
        return Ok(ApiResponse<PagedResponse<T>>.Ok(data));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(string actionName, object routeValues, T data, string? message = null)
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse> ErrorResponse(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        string? errorCode = null,
        IReadOnlyList<string>? errors = null,
        IDictionary<string, string[]>? validationErrors = null)
    {
        var response = ApiResponse.Fail(message, errors, errorCode, TraceId, validationErrors);
        return StatusCode((int)statusCode, response);
    }
}
