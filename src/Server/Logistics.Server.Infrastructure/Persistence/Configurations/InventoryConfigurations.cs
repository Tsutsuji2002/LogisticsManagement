using Logistics.Server.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", "inventory");
        builder.ConfigureBaseEntity();

        builder.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable("InventoryLots", "inventory");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.LotNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => new { e.ProductId, e.LotNumber }).IsUnique();

        builder.Property(e => e.VendorBatchCode).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventorySerialNumberConfiguration : IEntityTypeConfiguration<InventorySerialNumber>
{
    public void Configure(EntityTypeBuilder<InventorySerialNumber> builder)
    {
        builder.ToTable("InventorySerialNumbers", "inventory");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SerialNumber).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => new { e.ProductId, e.SerialNumber }).IsUnique();

        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReceivedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.DispatchedAt).HasPrecision(7);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances", "inventory");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.WarehouseId, e.WarehouseLocationId, e.ProductId, e.LotId });
        builder.Property(e => e.QuantityAvailable).HasComputedColumnSql("[QuantityOnHand] - [QuantityAllocated]");
        builder.Property(e => e.LastCountDate).HasPrecision(7);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WarehouseLocation)
            .WithMany()
            .HasForeignKey(e => e.WarehouseLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lot)
            .WithMany()
            .HasForeignKey(e => e.LotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions", "inventory");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.TransactionNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.TransactionNumber).IsUnique();

        builder.Property(e => e.TransactionType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReferenceNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TransactionDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PerformedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventoryTransactionItemConfiguration : IEntityTypeConfiguration<InventoryTransactionItem>
{
    public void Configure(EntityTypeBuilder<InventoryTransactionItem> builder)
    {
        builder.ToTable("InventoryTransactionItems", "inventory");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UnitCost).HasPrecision(19, 4);
        builder.Property(e => e.TotalCost).HasPrecision(19, 4);

        builder.HasOne(e => e.InventoryTransaction)
            .WithMany(t => t.TransactionItems)
            .HasForeignKey(e => e.InventoryTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lot)
            .WithMany()
            .HasForeignKey(e => e.LotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", "inventory");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.AdjustmentNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.AdjustmentNumber).IsUnique();

        builder.Property(e => e.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReasonDescription).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApprovedAt).HasPrecision(7);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", "inventory");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.TransferNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.TransferNumber).IsUnique();

        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ShippedAt).HasPrecision(7);
        builder.Property(e => e.ReceivedAt).HasPrecision(7);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.SourceWarehouse)
            .WithMany()
            .HasForeignKey(e => e.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(e => e.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RequestedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.RequestedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations", "inventory");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReservedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.ExpiresAt).HasPrecision(7);
        builder.Property(e => e.FulfilledAt).HasPrecision(7);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lot)
            .WithMany()
            .HasForeignKey(e => e.LotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
