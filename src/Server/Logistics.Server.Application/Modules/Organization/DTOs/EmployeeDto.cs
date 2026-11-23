namespace Logistics.Server.Application.Modules.Organization.DTOs;

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
    string? Username = null
);

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
    EmployeeAccountDto Account
);

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
    DateTime? LastLoginAt
);

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
    long? AddressId = null
);

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
    long? AddressId = null
);

public record UpdateEmployeeStatusRequest(
    string Status,
    string? Reason = null
);

public record CreateEmployeeAccountRequest(
    string Username,
    string Password,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<long>? BranchIds = null,
    long? DefaultBranchId = null
);

public record UpdateEmployeeAccountStatusRequest(
    bool IsActive,
    string? Reason = null
);

public record EmployeeBranchAccessDto(
    long EmployeeId,
    Guid EmployeePublicId,
    string FullName,
    long PrimaryBranchId,
    string PrimaryBranchName,
    IReadOnlyList<EmployeeAssignedBranchDto> AuthorizedBranches
);

public record EmployeeAssignedBranchDto(
    long BranchId,
    Guid PublicId,
    string Code,
    string Name,
    bool IsDefault
);
