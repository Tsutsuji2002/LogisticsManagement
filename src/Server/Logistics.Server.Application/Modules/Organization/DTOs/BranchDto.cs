namespace Logistics.Server.Application.Modules.Organization.DTOs;

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
    string? FormattedAddress = null
);

public record BranchDetailDto(
    BranchDto Branch,
    int EmployeeCount,
    int DepartmentCount,
    int WarehouseCount,
    IReadOnlyList<DepartmentDto> Departments,
    IReadOnlyList<WarehouseDto> Warehouses
);

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
    int ActiveWarehouseCount
);

public record CreateBranchRequest(
    long? CompanyId,
    string Code,
    string Name,
    string BranchType,
    string Phone,
    string? Email = null,
    long? AddressId = null
);

public record UpdateBranchRequest(
    string Name,
    string BranchType,
    string Phone,
    string? Email = null,
    long? AddressId = null
);

public record UpdateBranchStatusRequest(
    bool IsActive,
    string? Reason = null
);
