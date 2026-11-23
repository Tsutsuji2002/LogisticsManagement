namespace Logistics.Server.Application.Modules.Organization.DTOs;

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
    DateTime CreatedAt
);

public record CreateCompanyRequest(
    string Code,
    string LegalName,
    string TradingName,
    string TaxNumber,
    string Email,
    string Phone,
    string? Website = null,
    string? LogoUrl = null
);

public record UpdateCompanyRequest(
    string LegalName,
    string TradingName,
    string TaxNumber,
    string Email,
    string Phone,
    string? Website = null,
    string? LogoUrl = null
);

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
    int AuthorizedBranchCount
);
