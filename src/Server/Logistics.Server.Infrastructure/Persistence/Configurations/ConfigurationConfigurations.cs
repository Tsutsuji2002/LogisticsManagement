using Logistics.Server.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(2).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Alpha3Code).HasMaxLength(3).IsRequired();
        builder.HasIndex(e => e.Alpha3Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.NativeName).HasMaxLength(100);
        builder.Property(e => e.PhonePrefix).HasMaxLength(10);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Country)
            .WithMany(c => c.Provinces)
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("Districts", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Province)
            .WithMany(p => p.Districts)
            .HasForeignKey(e => e.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("Wards", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.District)
            .WithMany(d => d.Wards)
            .HasForeignKey(e => e.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(3).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ExchangeRateToDefault).HasPrecision(18, 6);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ConversionFactor).HasPrecision(18, 6);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();

        builder.Property(e => e.Value).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ValueType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("NumberSequences", "configuration");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Scope).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Scope).IsUnique();

        builder.Property(e => e.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Suffix).HasMaxLength(20);
        builder.Property(e => e.CurrentFormat).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ResetFrequency).HasMaxLength(20).IsRequired();
    }
}
