using Logistics.Server.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class BackgroundJobLogConfiguration : IEntityTypeConfiguration<BackgroundJobLog>
{
    public void Configure(EntityTypeBuilder<BackgroundJobLog> builder)
    {
        builder.ToTable("BackgroundJobLogs", "integration");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.JobName).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.JobName);

        builder.Property(e => e.ExecutionId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TriggerType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StartedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.CompletedAt).HasPrecision(7);
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "integration");
        builder.ConfigureBaseHistoricalEntity();

        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.EventType);

        builder.Property(e => e.AggregateType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AggregateId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.ProcessedAt).HasPrecision(7);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
    }
}
