namespace Logistics.Server.Application.Modules.Warehouse.DTOs;

// ───────────── Warehouse DTOs ─────────────

public class WarehouseDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal TotalAreaSqM { get; set; }
    public long? AddressId { get; set; }
    public long? ManagerEmployeeId { get; set; }
    public string? ManagerEmployeeName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ZoneCount { get; set; }
    public int LocationCount { get; set; }
}

public class WarehouseDetailDto : WarehouseDto
{
    public IReadOnlyList<WarehouseZoneDto> Zones { get; set; } = [];
}

public class CreateWarehouseRequest
{
    public long BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hybrid"; // InventoryWarehouse, ParcelHub, Hybrid
    public decimal TotalAreaSqM { get; set; }
    public long? AddressId { get; set; }
    public long? ManagerEmployeeId { get; set; }
}

public class UpdateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hybrid";
    public decimal TotalAreaSqM { get; set; }
    public long? AddressId { get; set; }
    public long? ManagerEmployeeId { get; set; }
}

// ───────────── Warehouse Zone DTOs ─────────────

public class WarehouseZoneDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty; // Receiving, BulkStorage, Picking, Staging, ParcelSorting, CrossDock, Return, Packing
    public string TemperatureType { get; set; } = string.Empty; // Ambient, Chilled, Frozen, AirConditioned
    public bool IsActive { get; set; }
    public int LocationCount { get; set; }
}

public class CreateWarehouseZoneRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = "BulkStorage";
    public string TemperatureType { get; set; } = "Ambient";
}

public class UpdateWarehouseZoneRequest
{
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = "BulkStorage";
    public string TemperatureType { get; set; } = "Ambient";
}

// ───────────── Warehouse Location DTOs ─────────────

public class WarehouseLocationDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long WarehouseZoneId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; }
    public decimal MaxVolumeCbm { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsBlocked { get; set; }
}

public class CreateWarehouseLocationRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; } = 1000m;
    public decimal MaxVolumeCbm { get; set; } = 5m;
}

public class UpdateWarehouseLocationRequest
{
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; }
    public decimal MaxVolumeCbm { get; set; }
    public bool IsBlocked { get; set; }
}
