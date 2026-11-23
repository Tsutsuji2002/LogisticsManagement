namespace Logistics.Server.Application.Modules.Crm.DTOs;

public record CustomerPricingRuleDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string? CustomerCode,
    long? CustomerContractId,
    string? ContractNumber,
    long? ShippingRateCardId,
    string? ShippingRateCardName,
    string Name,
    decimal DiscountPercent,
    decimal FlatDiscountAmount,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCustomerPricingRuleRequest(
    string Name,
    DateTime EffectiveFrom,
    long? CustomerContractId = null,
    long? ShippingRateCardId = null,
    decimal DiscountPercent = 0,
    decimal FlatDiscountAmount = 0,
    int Priority = 1,
    DateTime? EffectiveTo = null,
    bool IsActive = true
);

public record UpdateCustomerPricingRuleRequest(
    string Name,
    DateTime EffectiveFrom,
    long? CustomerContractId = null,
    long? ShippingRateCardId = null,
    decimal DiscountPercent = 0,
    decimal FlatDiscountAmount = 0,
    int Priority = 1,
    DateTime? EffectiveTo = null,
    bool IsActive = true
);

public record UpdateCustomerPricingRuleStatusRequest(
    bool IsActive
);
