using Logistics.Server.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class ShippingRateCardConfiguration : IEntityTypeConfiguration<ShippingRateCard>
{
    public void Configure(EntityTypeBuilder<ShippingRateCard> builder)
    {
        builder.ToTable("ShippingRateCards", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.EffectiveFrom).HasPrecision(7);
        builder.Property(e => e.EffectiveTo).HasPrecision(7);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.BaseCurrency)
            .WithMany()
            .HasForeignKey(e => e.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShippingRateRuleConfiguration : IEntityTypeConfiguration<ShippingRateRule>
{
    public void Configure(EntityTypeBuilder<ShippingRateRule> builder)
    {
        builder.ToTable("ShippingRateRules", "finance");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ServiceType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FromWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.ToWeightKg).HasPrecision(12, 3);
        builder.Property(e => e.BaseRate).HasPrecision(19, 4);
        builder.Property(e => e.AdditionalPerKgRate).HasPrecision(19, 4);
        builder.Property(e => e.SurchargePercent).HasPrecision(5, 2);
        builder.Property(e => e.MinimumFee).HasPrecision(19, 4);

        builder.HasOne(e => e.ShippingRateCard)
            .WithMany(c => c.Rules)
            .HasForeignKey(e => e.ShippingRateCardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.OriginProvince)
            .WithMany()
            .HasForeignKey(e => e.OriginProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DestinationProvince)
            .WithMany()
            .HasForeignKey(e => e.DestinationProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShipmentChargeConfiguration : IEntityTypeConfiguration<ShipmentCharge>
{
    public void Configure(EntityTypeBuilder<ShipmentCharge> builder)
    {
        builder.ToTable("ShipmentCharges", "finance");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ChargeType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(19, 4);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.CreatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Charges)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Invoice)
            .WithMany()
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.InvoiceNumber).IsUnique();

        builder.Property(e => e.SubTotal).HasPrecision(19, 4);
        builder.Property(e => e.TaxRate).HasPrecision(5, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(19, 4);
        builder.Property(e => e.TotalAmount).HasPrecision(19, 4);
        builder.Property(e => e.PaidAmount).HasPrecision(19, 4);
        builder.Property(e => e.OutstandingAmount).HasPrecision(19, 4).HasComputedColumnSql("[TotalAmount] - [PaidAmount]");
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems", "finance");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(250).IsRequired();
        builder.Property(e => e.UnitPrice).HasPrecision(19, 4);
        builder.Property(e => e.Amount).HasPrecision(19, 4);
        builder.Property(e => e.TaxAmount).HasPrecision(19, 4);
        builder.Property(e => e.TotalAmount).HasPrecision(19, 4);

        builder.HasOne(e => e.Invoice)
            .WithMany(i => i.InvoiceItems)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Shipment)
            .WithMany()
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ShipmentCharge)
            .WithMany()
            .HasForeignKey(e => e.ShipmentChargeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.PaymentNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.PaymentNumber).IsUnique();

        builder.Property(e => e.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(19, 4);
        builder.Property(e => e.TransactionReference).HasMaxLength(100);
        builder.Property(e => e.BankName).HasMaxLength(100);
        builder.Property(e => e.BankAccount).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PaymentDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.ConfirmedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(e => e.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", "finance");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AllocatedAmount).HasPrecision(19, 4);
        builder.Property(e => e.AllocatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Payment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Invoice)
            .WithMany(i => i.PaymentAllocations)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AllocatedByUser)
            .WithMany()
            .HasForeignKey(e => e.AllocatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CODCollectionConfiguration : IEntityTypeConfiguration<CODCollection>
{
    public void Configure(EntityTypeBuilder<CODCollection> builder)
    {
        builder.ToTable("CODCollections", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.CollectedAmount).HasPrecision(19, 4);
        builder.Property(e => e.CollectionDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DriverReceiptNumber).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.VerifiedAt).HasPrecision(7);

        builder.HasOne(e => e.Shipment)
            .WithMany()
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CollectedByEmployee)
            .WithMany()
            .HasForeignKey(e => e.CollectedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VerifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CODRemittanceConfiguration : IEntityTypeConfiguration<CODRemittance>
{
    public void Configure(EntityTypeBuilder<CODRemittance> builder)
    {
        builder.ToTable("CODRemittances", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.RemittanceNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.RemittanceNumber).IsUnique();

        builder.Property(e => e.RemittanceDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.TotalCodAmount).HasPrecision(19, 4);
        builder.Property(e => e.TotalServiceFeesDeducted).HasPrecision(19, 4);
        builder.Property(e => e.NetRemittanceAmount).HasPrecision(19, 4);
        builder.Property(e => e.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PaymentReference).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BankName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.BankAccount).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AccountHolderName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.RemittedAt).HasPrecision(7);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RemittedByUser)
            .WithMany()
            .HasForeignKey(e => e.RemittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CODRemittanceItemConfiguration : IEntityTypeConfiguration<CODRemittanceItem>
{
    public void Configure(EntityTypeBuilder<CODRemittanceItem> builder)
    {
        builder.ToTable("CODRemittanceItems", "finance");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CodAmount).HasPrecision(19, 4);
        builder.Property(e => e.DeductedShippingFee).HasPrecision(19, 4);
        builder.Property(e => e.NetAmount).HasPrecision(19, 4);

        builder.HasOne(e => e.CODRemittance)
            .WithMany(r => r.RemittanceItems)
            .HasForeignKey(e => e.CODRemittanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CODCollection)
            .WithOne(c => c.RemittanceItem)
            .HasForeignKey<CODRemittanceItem>(e => e.CODCollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Shipment)
            .WithMany()
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerBalanceConfiguration : IEntityTypeConfiguration<CustomerBalance>
{
    public void Configure(EntityTypeBuilder<CustomerBalance> builder)
    {
        builder.ToTable("CustomerBalances", "finance");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CustomerId).IsUnique();
        builder.Property(e => e.CurrentOutstandingBalance).HasPrecision(19, 4);
        builder.Property(e => e.UnbilledChargesAmount).HasPrecision(19, 4);
        builder.Property(e => e.TotalCodHoldingAmount).HasPrecision(19, 4);
        builder.Property(e => e.LastUpdatedDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.Customer)
            .WithOne()
            .HasForeignKey<CustomerBalance>(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FinancialLedgerEntryConfiguration : IEntityTypeConfiguration<FinancialLedgerEntry>
{
    public void Configure(EntityTypeBuilder<FinancialLedgerEntry> builder)
    {
        builder.ToTable("FinancialLedgerEntries", "finance");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.EntryNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.EntryNumber).IsUnique();

        builder.Property(e => e.EntryType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReferenceNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DebitAmount).HasPrecision(19, 4);
        builder.Property(e => e.CreditAmount).HasPrecision(19, 4);
        builder.Property(e => e.RunningBalance).HasPrecision(19, 4);
        builder.Property(e => e.EntryDate).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
