using Logistics.Server.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments", "shipping");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.TrackingNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.TrackingNumber).IsUnique();

        builder.Property(e => e.CarrierTrackingNumber).HasMaxLength(100);
        builder.HasIndex(e => e.CarrierTrackingNumber);

        builder.Property(e => e.ShipmentStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ServiceType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ActualWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.ChargeableWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.DeclaredValue).HasPrecision(19, 4);
        builder.Property(e => e.CodAmount).HasPrecision(19, 4);
        builder.Property(e => e.ShippingFee).HasPrecision(19, 4);
        builder.Property(e => e.InsuranceFee).HasPrecision(19, 4);
        builder.Property(e => e.SurchargesTotal).HasPrecision(19, 4);
        builder.Property(e => e.TotalAmount).HasPrecision(19, 4);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OriginBranch)
            .WithMany()
            .HasForeignKey(e => e.OriginBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DestinationBranch)
            .WithMany()
            .HasForeignKey(e => e.DestinationBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedCarrier)
            .WithMany()
            .HasForeignKey(e => e.AssignedCarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CarrierService)
            .WithMany()
            .HasForeignKey(e => e.CarrierServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShipmentPackageConfiguration : IEntityTypeConfiguration<ShipmentPackage>
{
    public void Configure(EntityTypeBuilder<ShipmentPackage> builder)
    {
        builder.ToTable("ShipmentPackages", "shipping");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.PackageCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Barcode).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Barcode);

        builder.Property(e => e.WeightKg).HasPrecision(12, 3);
        builder.Property(e => e.LengthCm).HasPrecision(12, 2);
        builder.Property(e => e.WidthCm).HasPrecision(12, 2);
        builder.Property(e => e.HeightCm).HasPrecision(12, 2);
        builder.Property(e => e.VolumetricWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.PackageStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SealNumber).HasMaxLength(50);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Packages)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.OrderPackage)
            .WithMany()
            .HasForeignKey(e => e.OrderPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShipmentAddressConfiguration : IEntityTypeConfiguration<ShipmentAddress>
{
    public void Configure(EntityTypeBuilder<ShipmentAddress> builder)
    {
        builder.ToTable("ShipmentAddresses", "shipping");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AddressType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(e => e.AddressLine2).HasMaxLength(250);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.FormattedAddress).HasMaxLength(500).IsRequired();

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Addresses)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Country)
            .WithMany()
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Province)
            .WithMany()
            .HasForeignKey(e => e.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.District)
            .WithMany()
            .HasForeignKey(e => e.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Ward)
            .WithMany()
            .HasForeignKey(e => e.WardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShipmentStatusHistoryConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistories", "shipping");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SubStatus).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ChangedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.StatusHistories)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LocationWarehouse)
            .WithMany()
            .HasForeignKey(e => e.LocationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LocationBranch)
            .WithMany()
            .HasForeignKey(e => e.LocationBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ChangedByUser)
            .WithMany()
            .HasForeignKey(e => e.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ChangedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.ChangedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ShipmentAssignmentConfiguration : IEntityTypeConfiguration<ShipmentAssignment>
{
    public void Configure(EntityTypeBuilder<ShipmentAssignment> builder)
    {
        builder.ToTable("ShipmentAssignments", "shipping");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AssignmentType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AssignedVehicleId).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.AssignedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.CompletedAt).HasPrecision(7);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Assignments)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedToEmployee)
            .WithMany()
            .HasForeignKey(e => e.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedBranch)
            .WithMany()
            .HasForeignKey(e => e.AssignedBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedByUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PickupRequestConfiguration : IEntityTypeConfiguration<PickupRequest>
{
    public void Configure(EntityTypeBuilder<PickupRequest> builder)
    {
        builder.ToTable("PickupRequests", "shipping");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.PickupCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.PickupCode).IsUnique();

        builder.Property(e => e.TimeSlot).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ContactPhone).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RequestedAddress)
            .WithMany()
            .HasForeignKey(e => e.RequestedAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedEmployee)
            .WithMany()
            .HasForeignKey(e => e.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("DeliveryAttempts", "shipping");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FailureReasonCode).HasMaxLength(50);
        builder.Property(e => e.FailureNotes).HasMaxLength(500);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(10, 7);
        builder.Property(e => e.AttemptedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.DeliveryAttempts)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AttemptedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.AttemptedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProofOfDeliveryConfiguration : IEntityTypeConfiguration<ProofOfDelivery>
{
    public void Configure(EntityTypeBuilder<ProofOfDelivery> builder)
    {
        builder.ToTable("ProofOfDeliveries", "shipping");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RecipientName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.RecipientRelation).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RecipientNationalId).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(10, 7);
        builder.Property(e => e.DeliveredAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.VerifiedAt).HasPrecision(7);

        builder.HasOne(e => e.Shipment)
            .WithOne(s => s.ProofOfDelivery)
            .HasForeignKey<ProofOfDelivery>(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DeliveryAttempt)
            .WithOne(d => d.ProofOfDelivery)
            .HasForeignKey<ProofOfDelivery>(e => e.DeliveryAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SignatureAttachment)
            .WithMany()
            .HasForeignKey(e => e.SignatureAttachmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PhotoAttachment)
            .WithMany()
            .HasForeignKey(e => e.PhotoAttachmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VerifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequests", "shipping");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ReturnCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.ReturnCode).IsUnique();

        builder.Property(e => e.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReasonDescription).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.RequestedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.ApprovedAt).HasPrecision(7);

        builder.HasOne(e => e.OriginalShipment)
            .WithMany()
            .HasForeignKey(e => e.OriginalShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NewReturnShipment)
            .WithMany()
            .HasForeignKey(e => e.NewReturnShipmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ShipmentRouteConfiguration : IEntityTypeConfiguration<ShipmentRoute>
{
    public void Configure(EntityTypeBuilder<ShipmentRoute> builder)
    {
        builder.ToTable("ShipmentRoutes", "shipping");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransportType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.VehicleNumber).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EstimatedDeparture).HasPrecision(7);
        builder.Property(e => e.ActualDeparture).HasPrecision(7);
        builder.Property(e => e.EstimatedArrival).HasPrecision(7);
        builder.Property(e => e.ActualArrival).HasPrecision(7);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Routes)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FromWarehouse)
            .WithMany()
            .HasForeignKey(e => e.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToWarehouse)
            .WithMany()
            .HasForeignKey(e => e.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
