using Logistics.Server.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "sales");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.OrderNumber).IsUnique();

        builder.Property(e => e.OrderStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PaymentStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.TotalVolumeCbm).HasPrecision(12, 4);
        builder.Property(e => e.DeclaredValue).HasPrecision(19, 4);
        builder.Property(e => e.CodAmount).HasPrecision(19, 4);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);
        builder.Property(e => e.CancelledBy).HasMaxLength(100);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", "sales");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Sku).HasMaxLength(50);
        builder.Property(e => e.UnitPrice).HasPrecision(19, 4);
        builder.Property(e => e.TotalPrice).HasPrecision(19, 4);
        builder.Property(e => e.DeclaredValue).HasPrecision(19, 4);
        builder.Property(e => e.WeightKg).HasPrecision(12, 3);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> builder)
    {
        builder.ToTable("OrderAddresses", "sales");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AddressType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(e => e.AddressLine2).HasMaxLength(250);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.FormattedAddress).HasMaxLength(500).IsRequired();

        builder.HasOne(e => e.Order)
            .WithMany(o => o.OrderAddresses)
            .HasForeignKey(e => e.OrderId)
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

public class OrderPackageConfiguration : IEntityTypeConfiguration<OrderPackage>
{
    public void Configure(EntityTypeBuilder<OrderPackage> builder)
    {
        builder.ToTable("OrderPackages", "sales");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.PackageCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.WeightKg).HasPrecision(12, 3);
        builder.Property(e => e.LengthCm).HasPrecision(12, 2);
        builder.Property(e => e.WidthCm).HasPrecision(12, 2);
        builder.Property(e => e.HeightCm).HasPrecision(12, 2);
        builder.Property(e => e.VolumetricWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.DeclaredValue).HasPrecision(19, 4);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Barcode).HasMaxLength(100);
        builder.Property(e => e.SpecialInstructions).HasMaxLength(500);

        builder.HasOne(e => e.Order)
            .WithMany(o => o.OrderPackages)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
