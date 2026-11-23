namespace Logistics.Server.Application.Modules.Organization.DTOs;

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
    int EmployeeCount = 0
);

public record CreateDepartmentRequest(
    long BranchId,
    string Code,
    string Name,
    string? Description = null,
    long? ManagerEmployeeId = null
);

public record UpdateDepartmentRequest(
    string Name,
    string? Description = null,
    long? ManagerEmployeeId = null
);

public record UpdateDepartmentStatusRequest(
    bool IsActive,
    string? Reason = null
);
