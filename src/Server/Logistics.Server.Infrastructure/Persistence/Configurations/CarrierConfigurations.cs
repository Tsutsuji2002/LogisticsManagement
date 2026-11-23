using Logistics.Server.Domain.Entities.Carrier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class CarrierEntityConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("Carriers", "carrier");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.Property(e => e.ContactEmail).HasMaxLength(150);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(250);
        builder.Property(e => e.ApiBaseUrl).HasMaxLength(250).IsRequired();
        builder.Property(e => e.TrackingUrlTemplate).HasMaxLength(500);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class CarrierServiceConfiguration : IEntityTypeConfiguration<CarrierService>
{
    public void Configure(EntityTypeBuilder<CarrierService> builder)
    {
        builder.ToTable("CarrierServices", "carrier");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ServiceCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ServiceName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.MaxWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.MaxLengthCm).HasPrecision(12, 2);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Carrier)
            .WithMany(c => c.Services)
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarrierConfigurationConfiguration : IEntityTypeConfiguration<CarrierConfiguration>
{
    public void Configure(EntityTypeBuilder<CarrierConfiguration> builder)
    {
        builder.ToTable("CarrierConfigurations", "carrier");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Environment).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApiKey).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ApiSecret).HasMaxLength(500);
        builder.Property(e => e.ClientId).HasMaxLength(100);
        builder.Property(e => e.Username).HasMaxLength(100);
        builder.Property(e => e.EncryptedPassword).HasMaxLength(500);
        builder.Property(e => e.WebhookSecret).HasMaxLength(500);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Carrier)
            .WithMany(c => c.Configurations)
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarrierTrackingMappingConfiguration : IEntityTypeConfiguration<CarrierTrackingMapping>
{
    public void Configure(EntityTypeBuilder<CarrierTrackingMapping> builder)
    {
        builder.ToTable("CarrierTrackingMappings", "carrier");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalStatusCode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ExternalStatusName).HasMaxLength(150);
        builder.Property(e => e.InternalShipmentStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.InternalSubStatus).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Carrier)
            .WithMany(c => c.TrackingMappings)
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarrierTrackingEventConfiguration : IEntityTypeConfiguration<CarrierTrackingEvent>
{
    public void Configure(EntityTypeBuilder<CarrierTrackingEvent> builder)
    {
        builder.ToTable("CarrierTrackingEvents", "carrier");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CarrierTrackingNumber).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ExternalEventId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EventCode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EventDescription).HasMaxLength(500).IsRequired();
        builder.Property(e => e.LocationName).HasMaxLength(200);
        builder.Property(e => e.ProcessedStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EventTimestamp).HasPrecision(7);
        builder.Property(e => e.ProcessedAt).HasPrecision(7);

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedShipment)
            .WithMany()
            .HasForeignKey(e => e.ProcessedShipmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CarrierWebhookLogConfiguration : IEntityTypeConfiguration<CarrierWebhookLog>
{
    public void Configure(EntityTypeBuilder<CarrierWebhookLog> builder)
    {
        builder.ToTable("CarrierWebhookLogs", "carrier");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.ExternalEventId).HasMaxLength(100);
        builder.Property(e => e.WebhookUrl).HasMaxLength(500).IsRequired();
        builder.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ClientIpAddress).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProcessedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarrierApiLogConfiguration : IEntityTypeConfiguration<CarrierApiLog>
{
    public void Configure(EntityTypeBuilder<CarrierApiLog> builder)
    {
        builder.ToTable("CarrierApiLogs", "carrier");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Timestamp).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarrierSyncJobConfiguration : IEntityTypeConfiguration<CarrierSyncJob>
{
    public void Configure(EntityTypeBuilder<CarrierSyncJob> builder)
    {
        builder.ToTable("CarrierSyncJobs", "carrier");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.JobType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StartTime).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.EndTime).HasPrecision(7);

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
