using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Contracts.Organization;
using Logistics.Client.Application.Interfaces;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.DevData;
using Logistics.Shared.Contracts.Health;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.Infrastructure.Api;

public sealed class HealthApiClient(IApiClient api) : IHealthApiClient
{
    public async Task<HealthCheckResponse?> GetHealthAsync(CancellationToken ct = default)
        => (await api.GetAsync<ApiResponse<HealthCheckResponse>>("api/health", ct))?.Data;
}

public sealed class DevTestDataApiClient(IApiClient api) : IDevTestDataApiClient
{
    public async Task<DevTestDataStatusDto> GetStatusAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<DevTestDataStatusDto>>("api/v1/dev/test-data/status", ct), "GET dev/test-data/status");

    public async Task<ExecuteDevSeedResultDto> ExecuteAsync(ExecuteDevSeedRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ExecuteDevSeedRequest, ApiResponse<ExecuteDevSeedResultDto>>("api/v1/dev/test-data/execute", request, ct), "POST dev/test-data/execute");

    public async Task<DevTestDataIntegrityReportDto> VerifyIntegrityAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<object, ApiResponse<DevTestDataIntegrityReportDto>>("api/v1/dev/test-data/verify", new { }, ct), "POST dev/test-data/verify");
}

public sealed class AuthSessionApiClient(IApiClient api) : IAuthSessionApiClient
{
    public async Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<UserProfileDto>>("api/v1/auth/me", ct), "GET auth/me");

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        => await api.PostAsync<ChangePasswordRequest, ApiResponse>("api/v1/auth/change-password", request, ct);

    public async Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<UserSessionDto>>>("api/v1/auth/sessions", ct), "GET auth/sessions");

    public Task TerminateSessionAsync(long sessionId, CancellationToken ct = default)
        => api.DeleteAsync($"api/v1/auth/sessions/{sessionId}", ct);
}

public sealed class OrganizationApiClient(IApiClient api) : IOrganizationApiClient
{
    public async Task<CompanyDto?> GetCompanyAsync(CancellationToken ct = default)
        => (await api.GetAsync<ApiResponse<CompanyDto>>("api/v1/organization/company", ct))?.Data;

    public async Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCompanyRequest, ApiResponse<CompanyDto>>("api/v1/organization/company", request, ct), "PUT organization/company");

    public async Task<OrganizationOverviewDto> GetOverviewAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<OrganizationOverviewDto>>("api/v1/organization/overview", ct), "GET organization/overview");

    public async Task<PagedResponse<BranchDto>> GetBranchesAsync(string? search, bool? isActive, string? branchType, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/organization/branches",
            ("search", search), ("isActive", isActive), ("branchType", branchType), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<BranchDto>>>(url, ct), "GET organization/branches");
    }

    public async Task<BranchDetailDto> GetBranchDetailAsync(string idOrCode, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<BranchDetailDto>>($"api/v1/organization/branches/{Uri.EscapeDataString(idOrCode)}", ct), "GET branch detail");

    public async Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateBranchRequest, ApiResponse<BranchDto>>("api/v1/organization/branches", request, ct), "POST branch");

    public async Task<BranchDto> UpdateBranchAsync(string id, UpdateBranchRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateBranchRequest, ApiResponse<BranchDto>>($"api/v1/organization/branches/{Uri.EscapeDataString(id)}", request, ct), "PUT branch");

    public async Task<BranchDto> ToggleBranchStatusAsync(string id, UpdateBranchStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateBranchStatusRequest, ApiResponse<BranchDto>>($"api/v1/organization/branches/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH branch status");

    public async Task<PagedResponse<DepartmentDto>> GetDepartmentsAsync(string? search, long? branchId, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/organization/departments",
            ("search", search), ("branchId", branchId), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<DepartmentDto>>>(url, ct), "GET departments");
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateDepartmentRequest, ApiResponse<DepartmentDto>>("api/v1/organization/departments", request, ct), "POST department");

    public async Task<DepartmentDto> UpdateDepartmentAsync(string id, UpdateDepartmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateDepartmentRequest, ApiResponse<DepartmentDto>>($"api/v1/organization/departments/{Uri.EscapeDataString(id)}", request, ct), "PUT department");

    public async Task<DepartmentDto> ToggleDepartmentStatusAsync(string id, UpdateDepartmentStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateDepartmentStatusRequest, ApiResponse<DepartmentDto>>($"api/v1/organization/departments/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH department status");

    public async Task<PagedResponse<EmployeeDto>> GetDepartmentEmployeesAsync(string id, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl($"api/v1/organization/departments/{Uri.EscapeDataString(id)}/employees",
            ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<EmployeeDto>>>(url, ct), "GET department employees");
    }

    public async Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(string? search, long? branchId, long? departmentId, string? status, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/employees",
            ("search", search), ("branchId", branchId), ("departmentId", departmentId), ("status", status), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<EmployeeDto>>>(url, ct), "GET employees");
    }

    public async Task<EmployeeProfileDto> GetEmployeeProfileAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<EmployeeProfileDto>>($"api/v1/employees/{Uri.EscapeDataString(id)}/profile", ct), "GET employee profile");

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateEmployeeRequest, ApiResponse<EmployeeDto>>("api/v1/employees", request, ct), "POST employee");

    public async Task<EmployeeDto> UpdateEmployeeAsync(string id, UpdateEmployeeRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateEmployeeRequest, ApiResponse<EmployeeDto>>($"api/v1/employees/{Uri.EscapeDataString(id)}", request, ct), "PUT employee");

    public async Task<EmployeeDto> UpdateEmployeeStatusAsync(string id, UpdateEmployeeStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateEmployeeStatusRequest, ApiResponse<EmployeeDto>>($"api/v1/employees/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH employee status");

    public async Task<EmployeeAccountDto> CreateEmployeeAccountAsync(string id, CreateEmployeeAccountRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateEmployeeAccountRequest, ApiResponse<EmployeeAccountDto>>($"api/v1/employees/{Uri.EscapeDataString(id)}/account", request, ct), "POST employee account");

    public async Task<EmployeeAccountDto> ToggleEmployeeAccountStatusAsync(string id, UpdateEmployeeAccountStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateEmployeeAccountStatusRequest, ApiResponse<EmployeeAccountDto>>($"api/v1/employees/{Uri.EscapeDataString(id)}/account/status", request, ct), "PATCH employee account status");
}

public sealed class IdentityAdminApiClient(IApiClient api) : IIdentityAdminApiClient
{
    public async Task<PagedResponse<UserAccountDto>> GetUsersAsync(string? search, string? role, long? branchId, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/admin/users",
            ("search", search), ("role", role), ("branchId", branchId), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<UserAccountDto>>>(url, ct), "GET admin/users");
    }

    public async Task<UserAccountDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateUserRequest, ApiResponse<UserAccountDto>>("api/v1/admin/users", request, ct), "POST admin/users");

    public async Task<UserAccountDto> UpdateUserAsync(Guid publicId, UpdateUserRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateUserRequest, ApiResponse<UserAccountDto>>($"api/v1/admin/users/{publicId}", request, ct), "PUT admin/users");

    public async Task<UserAccountDto> ToggleUserStatusAsync(Guid publicId, UpdateUserStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateUserStatusRequest, ApiResponse<UserAccountDto>>($"api/v1/admin/users/{publicId}/status", request, ct), "PATCH admin/users/status");

    public async Task<UserAccountDto> UpdateUserRolesAsync(Guid publicId, UpdateUserRolesRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateUserRolesRequest, ApiResponse<UserAccountDto>>($"api/v1/admin/users/{publicId}/roles", request, ct), "PUT admin/users/roles");

    public async Task<UserAccountDto> UpdateUserBranchesAsync(Guid publicId, UpdateUserBranchesRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateUserBranchesRequest, ApiResponse<UserAccountDto>>($"api/v1/admin/users/{publicId}/branches", request, ct), "PUT admin/users/branches");

    public async Task ResetUserPasswordAsync(Guid publicId, AdminResetPasswordRequest request, CancellationToken ct = default)
        => await api.PostAsync<AdminResetPasswordRequest, ApiResponse>($"api/v1/admin/users/{publicId}/reset-password", request, ct);

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<RoleDto>>>("api/v1/admin/roles", ct), "GET admin/roles");

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<PermissionDto>>>("api/v1/admin/permissions", ct), "GET admin/permissions");
}
