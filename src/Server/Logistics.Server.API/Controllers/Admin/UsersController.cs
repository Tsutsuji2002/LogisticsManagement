using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Admin;

/// <summary>
/// Administrative endpoints for managing user accounts, role memberships, and branch authorizations.
/// </summary>
[Route("api/v1/admin/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UpdateUserRolesRequest> _rolesValidator;
    private readonly IValidator<UpdateUserBranchesRequest> _branchesValidator;
    private readonly IValidator<GrantBranchAccessRequest> _grantBranchValidator;
    private readonly IValidator<AdminResetPasswordRequest> _resetPasswordValidator;

    public UsersController(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UpdateUserRolesRequest> rolesValidator,
        IValidator<UpdateUserBranchesRequest> branchesValidator,
        IValidator<GrantBranchAccessRequest> grantBranchValidator,
        IValidator<AdminResetPasswordRequest> resetPasswordValidator)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _rolesValidator = rolesValidator;
        _branchesValidator = branchesValidator;
        _grantBranchValidator = grantBranchValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    /// <summary>
    /// Gets a paginated list of user accounts with optional filtering.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Identity.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserAccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] long? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetUsersAsync(search, role, branchId, isActive, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<UserAccountDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets user account details by its public identifier.
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [HasPermission(AppPermissions.Identity.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<UserAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid publicId, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetUserByPublicIdAsync(publicId, cancellationToken);
        if (user == null)
        {
            throw new Application.Common.Exceptions.NotFoundException("UserAccount", publicId);
        }
        return Ok(ApiResponse<UserAccountDto>.Ok(user));
    }

    /// <summary>
    /// Creates a new user account with assigned roles and branch accesses.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Identity.UsersCreate)]
    [ProducesResponseType(typeof(ApiResponse<UserAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var creatorUsername = _currentUserService.Username ?? "SystemAdmin";
        var user = await _userManagementService.CreateUserAsync(request, creatorUsername, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { publicId = user.PublicId }, ApiResponse<UserAccountDto>.Ok(user, "User account created successfully."));
    }

    /// <summary>
    /// Updates user account details (email, employee association).
    /// </summary>
    [HttpPut("{publicId:guid}")]
    [HasPermission(AppPermissions.Identity.UsersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<UserAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid publicId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        var user = await _userManagementService.UpdateUserAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse<UserAccountDto>.Ok(user, "User updated successfully."));
    }

    /// <summary>
    /// Activates, deactivates, or unlocks a user account.
    /// </summary>
    [HttpPatch("{publicId:guid}/status")]
    [HasPermission(AppPermissions.Identity.UsersUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(Guid publicId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.UpdateUserStatusAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("User account status updated successfully."));
    }

    /// <summary>
    /// Replaces the assigned roles for a user account.
    /// </summary>
    [HttpPut("{publicId:guid}/roles")]
    [HasPermission(AppPermissions.Identity.UsersRolesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(Guid publicId, [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var validation = await _rolesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.UpdateUserRolesAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("User roles updated successfully."));
    }

    /// <summary>
    /// Gets authorized branch assignments for a user account.
    /// </summary>
    [HttpGet("{publicId:guid}/branches")]
    [HasPermission(AppPermissions.Identity.UsersBranchAccessRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuthorizedBranchDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserBranches(Guid publicId, CancellationToken cancellationToken)
    {
        var branches = await _userManagementService.GetUserBranchesAsync(publicId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AuthorizedBranchDto>>.Ok(branches));
    }

    /// <summary>
    /// Grants access to a branch for a user account.
    /// </summary>
    [HttpPost("{publicId:guid}/branches")]
    [HasPermission(AppPermissions.Identity.UsersBranchAccessManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantUserBranch(Guid publicId, [FromBody] GrantBranchAccessRequest request, CancellationToken cancellationToken)
    {
        var validation = await _grantBranchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.GrantUserBranchAccessAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("Branch access granted successfully."));
    }

    /// <summary>
    /// Revokes access to a branch for a user account.
    /// </summary>
    [HttpDelete("{publicId:guid}/branches/{branchId:long}")]
    [HasPermission(AppPermissions.Identity.UsersBranchAccessManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserBranch(Guid publicId, long branchId, CancellationToken cancellationToken)
    {
        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.RevokeUserBranchAccessAsync(publicId, branchId, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("Branch access revoked successfully."));
    }

    /// <summary>
    /// Updates the authorized branch access list for a user account.
    /// </summary>
    [HttpPut("{publicId:guid}/branches")]
    [HasPermission(AppPermissions.Identity.UsersBranchesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserBranches(Guid publicId, [FromBody] UpdateUserBranchesRequest request, CancellationToken cancellationToken)
    {
        var validation = await _branchesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.UpdateUserBranchesAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("User branch assignments updated successfully."));
    }

    /// <summary>
    /// Administratively resets the password for a user account.
    /// </summary>
    [HttpPost("{publicId:guid}/reset-password")]
    [HasPermission(AppPermissions.Identity.UsersResetPassword)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid publicId, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var updaterUsername = _currentUserService.Username ?? "SystemAdmin";
        await _userManagementService.AdminResetPasswordAsync(publicId, request, updaterUsername, cancellationToken);
        return Ok(ApiResponse.Ok("User password has been reset successfully. Active sessions were terminated."));
    }
}
