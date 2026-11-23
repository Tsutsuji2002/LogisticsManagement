using Logistics.Server.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public static class EntityTypeBuilderExtensions
{
    public static void ConfigureBaseEntity<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PublicId).IsRequired();
        builder.HasIndex(e => e.PublicId).IsUnique();
        builder.Property(e => e.CreatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.UpdatedAt).HasPrecision(7);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.RowVersion).IsRowVersion();
    }

    public static void ConfigureBaseHistoricalEntity<T>(this EntityTypeBuilder<T> builder) where T : BaseHistoricalEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
