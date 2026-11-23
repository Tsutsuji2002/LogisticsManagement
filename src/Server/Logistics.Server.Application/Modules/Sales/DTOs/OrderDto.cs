namespace Logistics.Server.Application.Modules.Sales.DTOs;

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
    public OrderAddressDto? PickupAddress => Addresses.FirstOrDefault(a => a.AddressType.Equals("Sender", StringComparison.OrdinalIgnoreCase) || a.AddressType.Equals("Pickup", StringComparison.OrdinalIgnoreCase));
    public OrderAddressDto? DeliveryAddress => Addresses.FirstOrDefault(a => a.AddressType.Equals("Recipient", StringComparison.OrdinalIgnoreCase) || a.AddressType.Equals("Delivery", StringComparison.OrdinalIgnoreCase));
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
