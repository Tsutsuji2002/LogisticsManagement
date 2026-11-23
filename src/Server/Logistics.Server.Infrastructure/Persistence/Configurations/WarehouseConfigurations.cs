using Logistics.Server.Domain.Entities.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class WarehouseZoneConfiguration : IEntityTypeConfiguration<WarehouseZone>
{
    public void Configure(EntityTypeBuilder<WarehouseZone> builder)
    {
        builder.ToTable("WarehouseZones", "warehouse");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ZoneType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TemperatureType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Warehouse)
            .WithMany(w => w.Zones)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
    {
        builder.ToTable("WarehouseLocations", "warehouse");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Aisle).HasMaxLength(20);
        builder.Property(e => e.Rack).HasMaxLength(20);
        builder.Property(e => e.Shelf).HasMaxLength(20);
        builder.Property(e => e.Bin).HasMaxLength(20);
        builder.Property(e => e.Barcode).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Barcode);

        builder.Property(e => e.MaxWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.MaxVolumeCbm).HasPrecision(12, 4);

        builder.HasOne(e => e.WarehouseZone)
            .WithMany(z => z.Locations)
            .HasForeignKey(e => e.WarehouseZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseReceiptConfiguration : IEntityTypeConfiguration<WarehouseReceipt>
{
    public void Configure(EntityTypeBuilder<WarehouseReceipt> builder)
    {
        builder.ToTable("WarehouseReceipts", "warehouse");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.ReceiptNumber).IsUnique();

        builder.Property(e => e.ReceiptType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SourceVendor).HasMaxLength(200);
        builder.Property(e => e.ExpectedArrivalDate).HasPrecision(7);
        builder.Property(e => e.ActualArrivalDate).HasPrecision(7);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SourceBranch)
            .WithMany()
            .HasForeignKey(e => e.SourceBranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ReceivedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.ReceivedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseReceiptItemConfiguration : IEntityTypeConfiguration<WarehouseReceiptItem>
{
    public void Configure(EntityTypeBuilder<WarehouseReceiptItem> builder)
    {
        builder.ToTable("WarehouseReceiptItems", "warehouse");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LotNumber).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.WarehouseReceipt)
            .WithMany(r => r.ReceiptItems)
            .HasForeignKey(e => e.WarehouseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TargetLocation)
            .WithMany()
            .HasForeignKey(e => e.TargetLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class WarehouseDispatchConfiguration : IEntityTypeConfiguration<WarehouseDispatch>
{
    public void Configure(EntityTypeBuilder<WarehouseDispatch> builder)
    {
        builder.ToTable("WarehouseDispatches", "warehouse");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.DispatchNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.DispatchNumber).IsUnique();

        builder.Property(e => e.DispatchType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DispatchedAt).HasPrecision(7);
        builder.Property(e => e.VehicleNumber).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(e => e.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DestinationBranch)
            .WithMany()
            .HasForeignKey(e => e.DestinationBranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AssignedCarrier)
            .WithMany()
            .HasForeignKey(e => e.AssignedCarrierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.DispatchedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.DispatchedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseDispatchItemConfiguration : IEntityTypeConfiguration<WarehouseDispatchItem>
{
    public void Configure(EntityTypeBuilder<WarehouseDispatchItem> builder)
    {
        builder.ToTable("WarehouseDispatchItems", "warehouse");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LotNumber).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.WarehouseDispatch)
            .WithMany(d => d.DispatchItems)
            .HasForeignKey(e => e.WarehouseDispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SourceLocation)
            .WithMany()
            .HasForeignKey(e => e.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseDispatchPackageConfiguration : IEntityTypeConfiguration<WarehouseDispatchPackage>
{
    public void Configure(EntityTypeBuilder<WarehouseDispatchPackage> builder)
    {
        builder.ToTable("WarehouseDispatchPackages", "warehouse");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TargetContainerCode).HasMaxLength(50);
        builder.Property(e => e.ScannedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.WarehouseDispatch)
            .WithMany(d => d.DispatchPackages)
            .HasForeignKey(e => e.WarehouseDispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ShipmentPackage)
            .WithMany()
            .HasForeignKey(e => e.ShipmentPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ScannedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.ScannedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParcelHandlingEventConfiguration : IEntityTypeConfiguration<ParcelHandlingEvent>
{
    public void Configure(EntityTypeBuilder<ParcelHandlingEvent> builder)
    {
        builder.ToTable("ParcelHandlingEvents", "warehouse");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BagNumber).HasMaxLength(50);
        builder.Property(e => e.DeviceId).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.ScannedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.ShipmentPackage)
            .WithMany()
            .HasForeignKey(e => e.ShipmentPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Shipment)
            .WithMany()
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NextDestinationWarehouse)
            .WithMany()
            .HasForeignKey(e => e.NextDestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
