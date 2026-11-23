using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Organization.Services;

public interface ICompanyService
{
    Task<CompanyDto?> GetPrimaryCompanyAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<OrganizationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    // Existing compatibility
    Task<PagedResult<CompanyDto>> GetCompaniesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CompanyDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CompanyDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
}

public interface IBranchService
{
    Task<PagedResult<BranchDto>> GetBranchesAsync(string? search, bool? isActive, string? branchType, PagedRequest request, CancellationToken cancellationToken = default);
    Task<BranchDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<BranchSummaryDto?> GetSummaryByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<BranchDto> UpdateAsync(string identifier, UpdateBranchRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateBranchStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeDto>> GetBranchEmployeesAsync(string identifier, string? search, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentDto>> GetBranchDepartmentsAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseDto>> GetBranchWarehousesAsync(string identifier, CancellationToken cancellationToken = default);

    // Existing compatibility
    Task<PagedResult<BranchDto>> GetBranchesAsync(long? companyId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<BranchDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<BranchDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
}

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(string? search, long? branchId, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateAsync(string identifier, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateDepartmentStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeDto>> GetDepartmentEmployeesAsync(string identifier, PagedRequest request, CancellationToken cancellationToken = default);
}

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(string? search, long? branchId, long? departmentId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<EmployeeProfileDto?> GetProfileByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateAsync(string identifier, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeAccountDto> GetAccountAsync(string identifier, CancellationToken cancellationToken = default);
    Task<EmployeeAccountDto> CreateAccountAsync(string identifier, CreateEmployeeAccountRequest request, CancellationToken cancellationToken = default);
    Task UpdateAccountStatusAsync(string identifier, UpdateEmployeeAccountStatusRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeBranchAccessDto> GetBranchAccessAsync(string identifier, CancellationToken cancellationToken = default);
}

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, long? branchId, string? type, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
}
