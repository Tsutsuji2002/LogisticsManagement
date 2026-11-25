using Logistics.Server.Domain.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates", "notification");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Channel).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SubjectTemplate).HasMaxLength(500);
        builder.Property(e => e.BodyTemplate).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "notification");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.RecipientEmail).HasMaxLength(150);
        builder.Property(e => e.RecipientPhone).HasMaxLength(50);
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Channel).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Priority).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NotificationType).HasMaxLength(100);
        builder.Property(e => e.ReferenceType).HasMaxLength(50);
        builder.Property(e => e.IsRead).HasDefaultValue(false);
        builder.Property(e => e.ReadAt).HasPrecision(7);
        builder.Property(e => e.ExpiresAt).HasPrecision(7);
        builder.Property(e => e.EventKey).HasMaxLength(200);

        builder.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.CreatedAt });
        builder.HasIndex(e => e.EventKey)
            .IsUnique()
            .HasFilter("[EventKey] IS NOT NULL");

        builder.HasOne(e => e.Template)
            .WithMany()
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecipientUser)
            .WithMany()
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries", "notification");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.Channel).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.GatewayResponse).HasMaxLength(1000);
        builder.Property(e => e.ErrorDetails).HasMaxLength(1000);
        builder.Property(e => e.AttemptedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Notification)
            .WithMany(n => n.Deliveries)
            .HasForeignKey(e => e.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("ReportDefinitions", "notification");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.QueryDefinition).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("ScheduledReports", "notification");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ScheduleCron).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TimeZoneId).HasMaxLength(100).HasDefaultValue("UTC").IsRequired();
        builder.Property(e => e.OutputFormat).HasMaxLength(20).IsRequired();
        builder.Property(e => e.RecipientEmails).HasMaxLength(500).IsRequired();
        builder.Property(e => e.LastRunAt).HasPrecision(7);
        builder.Property(e => e.NextRunAt).HasPrecision(7);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.ReportDefinition)
            .WithMany(r => r.ScheduledReports)
            .HasForeignKey(e => e.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
