using Logistics.Server.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "audit");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Username).HasMaxLength(100);
        builder.Property(e => e.UserRole).HasMaxLength(50);
        builder.Property(e => e.Action).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TableName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SchemaName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PrimaryKey).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AffectedColumns).HasMaxLength(1000);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.Timestamp).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("FileAttachments", "audit");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.Property(e => e.FileName).HasMaxLength(250).IsRequired();
        builder.Property(e => e.StoredFileName).HasMaxLength(250).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FileHashSha256).HasMaxLength(64).IsRequired();
        builder.Property(e => e.UploadedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
