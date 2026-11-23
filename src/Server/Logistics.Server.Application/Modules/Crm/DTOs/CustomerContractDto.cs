namespace Logistics.Server.Application.Modules.Crm.DTOs;

public record CustomerContractDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string? CustomerCode,
    string ContractNumber,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    decimal DiscountRate,
    int PaymentTermDays,
    DateOnly? SignedAt,
    long? FileAttachmentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCustomerContractRequest(
    string ContractNumber,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status = "Draft",
    decimal DiscountRate = 0,
    int PaymentTermDays = 30,
    DateOnly? SignedAt = null,
    long? FileAttachmentId = null
);

public record UpdateCustomerContractRequest(
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    decimal DiscountRate,
    int PaymentTermDays,
    DateOnly? SignedAt,
    long? FileAttachmentId
);

public record UpdateCustomerContractStatusRequest(
    string Status,
    string? Reason = null
);
