namespace Logistics.Client.Application.Contracts.Sales;

public record OrderDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public DateTime? RequestedPickupDate { get; init; }
    public DateTime? RequiredDeliveryDate { get; init; }
    public string OrderStatus { get; init; } = "Draft";
    public string PaymentStatus { get; init; } = "Pending";
    public decimal TotalWeightKg { get; init; }
    public decimal TotalVolumeCbm { get; init; }
    public decimal DeclaredValue { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal CodAmount { get; init; }
    public string? Notes { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancelledBy { get; init; }
    public int ItemCount { get; init; }
    public int PackageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record OrderDetailDto : OrderDto
{
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
    public IReadOnlyList<OrderPackageDto> Packages { get; init; } = [];
    public IReadOnlyList<OrderAddressDto> Addresses { get; init; } = [];
}

public record OrderItemDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long OrderId { get; init; }
    public long? ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public long UnitOfMeasureId { get; init; }
    public string UnitOfMeasureCode { get; init; } = string.Empty;
    public string UnitOfMeasureName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal WeightKg { get; init; }
    public string? Notes { get; init; }
}

public record OrderPackageDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long OrderId { get; init; }
    public string PackageCode { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal VolumetricWeightKg { get; init; }
    public decimal DeclaredValue { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public string? SpecialInstructions { get; init; }
}

public record OrderAddressDto
{
    public long Id { get; init; }
    public long OrderId { get; init; }
    public string AddressType { get; init; } = "Sender";
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public long CountryId { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public string FormattedAddress { get; init; } = string.Empty;
}

public record OrderAddressRequest
{
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public long? CountryId { get; init; }
    public string? PostalCode { get; init; }
    public string? FormattedAddress { get; init; }
}

public record CreateOrderItemRequest
{
    public long? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public long? UnitOfMeasureId { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal WeightKg { get; init; }
    public string? Notes { get; init; }
}

public record CreateOrderPackageRequest
{
    public string? PackageCode { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal DeclaredValue { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public string? SpecialInstructions { get; init; }
}

public record CreateOrderRequest
{
    public long CustomerId { get; init; }
    public long? BranchId { get; init; }
    public DateTime? RequestedPickupDate { get; init; }
    public DateTime? RequiredDeliveryDate { get; init; }
    public long? CurrencyId { get; init; }
    public decimal CodAmount { get; init; }
    public string? Notes { get; init; }
    public OrderAddressRequest? PickupAddress { get; init; }
    public OrderAddressRequest? DeliveryAddress { get; init; }
    public List<CreateOrderItemRequest>? Items { get; init; }
    public List<CreateOrderPackageRequest>? Packages { get; init; }
}

public record UpdateOrderRequest
{
    public DateTime? RequestedPickupDate { get; init; }
    public DateTime? RequiredDeliveryDate { get; init; }
    public decimal CodAmount { get; init; }
    public string? Notes { get; init; }
}

public record CancelOrderRequest
{
    public string Reason { get; init; } = string.Empty;
}
