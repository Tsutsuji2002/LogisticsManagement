namespace Logistics.Server.Application.Modules.Crm.DTOs;

public record CustomerContactDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string ContactName,
    string? JobTitle,
    string PhoneNumber,
    string? Email,
    bool IsPrimary,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCustomerContactRequest(
    string ContactName,
    string PhoneNumber,
    string? JobTitle = null,
    string? Email = null,
    bool IsPrimary = false,
    string? Notes = null
);

public record UpdateCustomerContactRequest(
    string ContactName,
    string PhoneNumber,
    string? JobTitle = null,
    string? Email = null,
    bool IsPrimary = false,
    string? Notes = null
);

public record UpdateContactStatusRequest(
    bool IsActive,
    string? Reason = null
);
