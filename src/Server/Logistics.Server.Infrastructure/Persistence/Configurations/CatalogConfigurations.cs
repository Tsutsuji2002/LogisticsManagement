using Logistics.Server.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories", "catalog");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.ParentCategory)
            .WithMany(p => p.SubCategories)
            .HasForeignKey(e => e.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "catalog");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Sku).IsUnique();

        builder.Property(e => e.Barcode).HasMaxLength(100);
        builder.HasIndex(e => e.Barcode);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.WeightKg).HasPrecision(12, 3);
        builder.Property(e => e.LengthCm).HasPrecision(12, 2);
        builder.Property(e => e.WidthCm).HasPrecision(12, 2);
        builder.Property(e => e.HeightCm).HasPrecision(12, 2);
        builder.Property(e => e.DeclaredValue).HasPrecision(19, 4);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
