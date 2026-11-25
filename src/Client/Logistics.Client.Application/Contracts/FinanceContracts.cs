namespace Logistics.Client.Application.Contracts.Finance;

public record ShippingRateCardDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long BaseCurrencyId { get; init; }
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int RulesCount { get; init; }
}

public record ShippingRateRuleDto
{
    public long Id { get; init; }
    public long ShippingRateCardId { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public string? OriginProvinceName { get; init; }
    public string? DestinationProvinceName { get; init; }
    public decimal FromWeightKg { get; init; }
    public decimal ToWeightKg { get; init; }
    public decimal BaseRate { get; init; }
    public decimal AdditionalPerKgRate { get; init; }
    public decimal SurchargePercent { get; init; }
    public decimal MinimumFee { get; init; }
    public int EstimatedDays { get; init; }
}

public record CreateShippingRateCardRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long BaseCurrencyId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateShippingRateCardRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long BaseCurrencyId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
}

public record CreateShippingRateRuleRequest
{
    public string ServiceType { get; init; } = "StandardGround";
    public decimal FromWeightKg { get; init; }
    public decimal ToWeightKg { get; init; }
    public decimal BaseRate { get; init; }
    public decimal AdditionalPerKgRate { get; init; }
    public decimal SurchargePercent { get; init; }
    public decimal MinimumFee { get; init; }
    public int EstimatedDays { get; init; } = 2;
}

public record CalculateShippingFeeRequest
{
    public long? ShipmentId { get; init; }
    public long CustomerId { get; init; }
    public string ServiceType { get; init; } = "StandardGround";
    public decimal WeightKg { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
    public long? CurrencyId { get; init; }
}

public record ShippingFeeCalculationResultDto
{
    public decimal BaseRate { get; init; }
    public decimal WeightCharge { get; init; }
    public decimal SurchargeAmount { get; init; }
    public decimal InsuranceFee { get; init; }
    public decimal CodFee { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CustomerDiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string? AppliedRateCardCode { get; init; }
    public string? AppliedPricingRuleName { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public record ShipmentChargeDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string ShipmentTrackingNumber { get; init; } = string.Empty;
    public string ChargeType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsInvoiced { get; init; }
    public long? InvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateChargeAdjustmentRequest
{
    public string ChargeType { get; init; } = "Other";
    public decimal Amount { get; init; }
    public long CurrencyId { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record InvoiceItemDto
{
    public long Id { get; init; }
    public long InvoiceId { get; init; }
    public long? ShipmentId { get; init; }
    public string? ShipmentTrackingNumber { get; init; }
    public long? ShipmentChargeId { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
}

public record PaymentAllocationDto
{
    public long Id { get; init; }
    public long PaymentId { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public long InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal AllocatedAmount { get; init; }
    public DateTime AllocatedAt { get; init; }
}

public record InvoiceDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal SubTotal { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public int PaymentTermsDays { get; init; }
    public string? Notes { get; init; }
    public List<InvoiceItemDto> Items { get; init; } = [];
    public List<PaymentAllocationDto> Allocations { get; init; } = [];
}

public record CreateInvoiceItemRequest
{
    public long? ShipmentId { get; init; }
    public long? ShipmentChargeId { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal? TaxAmount { get; init; }
}

public record CreateInvoiceRequest
{
    public long CustomerId { get; init; }
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public decimal TaxRate { get; init; } = 8.0m;
    public int PaymentTermsDays { get; init; } = 30;
    public string? Notes { get; init; }
    public List<CreateInvoiceItemRequest> Items { get; init; } = [];
}

public record CancelInvoiceRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record PaymentDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal AllocatedAmount { get; init; }
    public decimal UnallocatedAmount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string? TransactionReference { get; init; }
    public string? BankName { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<PaymentAllocationDto> Allocations { get; init; } = [];
}

public record CreatePaymentAllocationRequest
{
    public long InvoiceId { get; init; }
    public decimal Amount { get; init; }
}

public record RegisterPaymentRequest
{
    public long CustomerId { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = "BankTransfer";
    public decimal Amount { get; init; }
    public long CurrencyId { get; init; }
    public string? TransactionReference { get; init; }
    public string? BankName { get; init; }
    public string? BankAccount { get; init; }
    public string? Notes { get; init; }
    public List<CreatePaymentAllocationRequest>? InitialAllocations { get; init; }
}

public record CustomerBalanceDto
{
    public long Id { get; init; }
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal CurrentOutstandingBalance { get; init; }
    public decimal UnbilledChargesAmount { get; init; }
    public decimal TotalCodHoldingAmount { get; init; }
    public decimal TotalExposure { get; init; }
    public DateTime LastUpdatedDate { get; init; }
}

public record CustomerBalanceReconciliationDto
{
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public decimal RecordedOutstandingBalance { get; init; }
    public decimal RecomputedOutstandingBalance { get; init; }
    public decimal RecordedUnbilledCharges { get; init; }
    public decimal RecomputedUnbilledCharges { get; init; }
    public bool IsConsistent { get; init; }
    public string Details { get; init; } = string.Empty;
}

public record FinancialLedgerEntryDto
{
    public long Id { get; init; }
    public string EntryNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string EntryType { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
    public string ReferenceNumber { get; init; } = string.Empty;
    public decimal DebitAmount { get; init; }
    public decimal CreditAmount { get; init; }
    public decimal RunningBalance { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = string.Empty;
}
