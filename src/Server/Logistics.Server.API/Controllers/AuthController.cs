using FluentValidation;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Authentication and session lifecycle management endpoints.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authService;
    private readonly IUserSessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthController(
        IAuthenticationService authService,
        IUserSessionService sessionService,
        ICurrentUserService currentUserService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _authService = authService;
        _sessionService = sessionService;
        _currentUserService = currentUserService;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// Authenticates a user and issues JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-rate-limit")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Authentication successful."));
    }

    /// <summary>
    /// Rotates a refresh token and issues a new access token and rotated refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth-rate-limit")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var validation = await _refreshValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed successfully."));
    }

    /// <summary>
    /// Logs out the user by revoking the specified refresh token and active session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        await _authService.LogoutAsync(userId, request?.RefreshToken, cancellationToken);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    /// <summary>
    /// Terminates all active sessions and revokes all refresh tokens for the current user.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        await _authService.LogoutAllAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok("All sessions have been revoked successfully."));
    }

    /// <summary>
    /// Retrieves the profile, effective roles, permissions, and authorized branches of the current user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        var profile = await _authService.GetCurrentUserProfileAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Changes the password for the currently authenticated user and terminates all active sessions.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _authService.ChangePasswordAsync(userId, request, ipAddress, cancellationToken);
        return Ok(ApiResponse.Ok("Password changed successfully. Existing sessions have been terminated."));
    }

    /// <summary>
    /// Returns the active login sessions for the currently authenticated user.
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserSessionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        var sessions = await _sessionService.GetUserSessionsAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserSessionDto>>.Ok(sessions));
    }

    /// <summary>
    /// Terminates an active login session belonging to the currently authenticated user.
    /// </summary>
    [HttpDelete("sessions/{sessionId:long}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateSession(long sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Unauthenticated request.");

        await _sessionService.TerminateSessionAsync(userId, sessionId, cancellationToken);
        return Ok(ApiResponse.Ok("Session terminated successfully."));
    }
}
