namespace Logistics.Server.Application.Modules.Organization.DTOs;

public record WarehouseDto(
    long Id,
    Guid PublicId,
    long BranchId,
    string BranchCode,
    string BranchName,
    string Code,
    string Name,
    string Type,
    decimal TotalAreaSqM,
    long? AddressId,
    long? ManagerEmployeeId,
    string? ManagerEmployeeName,
    bool IsActive,
    DateTime CreatedAt,
    int ZoneCount = 0
);

public record WarehouseDetailDto(
    WarehouseDto Warehouse,
    IReadOnlyList<WarehouseZoneDto> Zones
);

public record WarehouseZoneDto(
    long Id,
    Guid PublicId,
    string Code,
    string Name,
    string ZoneType,
    string TemperatureType,
    bool IsActive
);
