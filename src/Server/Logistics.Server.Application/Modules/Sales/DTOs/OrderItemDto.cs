namespace Logistics.Server.Application.Modules.Sales.DTOs;

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
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
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

public record UpdateOrderItemRequest
{
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal WeightKg { get; init; }
    public string? Notes { get; init; }
}
