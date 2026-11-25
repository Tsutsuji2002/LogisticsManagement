namespace Logistics.Client.Application.Contracts.Organization;

public record CompanyDto(
    long Id,
    Guid PublicId,
    string Code,
    string LegalName,
    string TradingName,
    string TaxNumber,
    string Email,
    string Phone,
    string? Website,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAt);

public record UpdateCompanyRequest(
    string LegalName,
    string TradingName,
    string TaxNumber,
    string Email,
    string Phone,
    string? Website = null,
    string? LogoUrl = null);

public record OrganizationOverviewDto(
    int TotalBranches,
    int ActiveBranches,
    int InactiveBranches,
    int TotalDepartments,
    int ActiveDepartments,
    int TotalEmployees,
    int ActiveEmployees,
    int InactiveEmployees,
    int OnLeaveEmployees,
    int ResignedEmployees,
    int TerminatedEmployees,
    int TotalWarehouses,
    int ActiveWarehouses,
    int InactiveWarehouses,
    string ScopeType,
    int AuthorizedBranchCount);

public record DepartmentDto(
    long Id,
    Guid PublicId,
    long CompanyId,
    long BranchId,
    string BranchCode,
    string BranchName,
    string Code,
    string Name,
    string? Description,
    long? ManagerEmployeeId,
    string? ManagerEmployeeName,
    bool IsActive,
    DateTime CreatedAt,
    int EmployeeCount = 0);

public record CreateDepartmentRequest(
    long BranchId,
    string Code,
    string Name,
    string? Description = null,
    long? ManagerEmployeeId = null);

public record UpdateDepartmentRequest(
    string Name,
    string? Description = null,
    long? ManagerEmployeeId = null);

public record UpdateDepartmentStatusRequest(bool IsActive, string? Reason = null);

public record BranchDto(
    long Id,
    Guid PublicId,
    long CompanyId,
    string Code,
    string Name,
    string BranchType,
    string Phone,
    string? Email,
    long? AddressId,
    bool IsActive,
    DateTime CreatedAt,
    string? FormattedAddress = null);

public record OrgWarehouseDto(
    long Id,
    Guid PublicId,
    long BranchId,
    string BranchCode,
    string BranchName,
    string Code,
    string Name,
    string Type,
    decimal TotalAreaSqM,
    long? AddressId,
    long? ManagerEmployeeId,
    string? ManagerEmployeeName,
    bool IsActive,
    DateTime CreatedAt,
    int ZoneCount = 0);

public record BranchDetailDto(
    BranchDto Branch,
    int EmployeeCount,
    int DepartmentCount,
    int WarehouseCount,
    IReadOnlyList<DepartmentDto> Departments,
    IReadOnlyList<OrgWarehouseDto> Warehouses);

public record BranchSummaryDto(
    long Id,
    Guid PublicId,
    string Code,
    string Name,
    string BranchType,
    bool IsActive,
    int EmployeeCount,
    int DepartmentCount,
    int WarehouseCount,
    int ActiveWarehouseCount);

public record CreateBranchRequest(
    long? CompanyId,
    string Code,
    string Name,
    string BranchType,
    string Phone,
    string? Email = null,
    long? AddressId = null);

public record UpdateBranchRequest(
    string Name,
    string BranchType,
    string Phone,
    string? Email = null,
    long? AddressId = null);

public record UpdateBranchStatusRequest(bool IsActive, string? Reason = null);

public record EmployeeDto(
    long Id,
    Guid PublicId,
    long BranchId,
    string BranchCode,
    string BranchName,
    long? DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    string Code,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string JobTitle,
    DateOnly HireDate,
    string Status,
    long? AddressId,
    DateTime CreatedAt,
    bool HasUserAccount,
    Guid? UserAccountPublicId = null,
    string? Username = null);

public record EmployeeAccountDto(
    bool HasAccount,
    long? UserId,
    Guid? UserPublicId,
    string? Username,
    string? Email,
    bool IsActive,
    bool IsLockedOut,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> BranchNames,
    DateTime? LastLoginAt);

public record EmployeeProfileDto(
    long Id,
    Guid PublicId,
    string Code,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string JobTitle,
    DateOnly HireDate,
    string Status,
    DateTime CreatedAt,
    BranchDto Branch,
    DepartmentDto? Department,
    string? FormattedAddress,
    EmployeeAccountDto Account);

public record CreateEmployeeRequest(
    long BranchId,
    long? DepartmentId,
    string Code,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string JobTitle,
    DateOnly HireDate,
    long? AddressId = null);

public record UpdateEmployeeRequest(
    long BranchId,
    long? DepartmentId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string JobTitle,
    DateOnly HireDate,
    long? AddressId = null);

public record UpdateEmployeeStatusRequest(string Status, string? Reason = null);

public record CreateEmployeeAccountRequest(
    string Username,
    string Password,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<long>? BranchIds = null,
    long? DefaultBranchId = null);

public record UpdateEmployeeAccountStatusRequest(bool IsActive, string? Reason = null);
