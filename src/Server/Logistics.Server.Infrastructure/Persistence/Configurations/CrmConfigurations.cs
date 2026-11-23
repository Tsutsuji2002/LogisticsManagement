using Logistics.Server.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(e => e.AddressLine2).HasMaxLength(250);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(10, 7);
        builder.Property(e => e.FormattedAddress).HasMaxLength(500).IsRequired();

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

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.CustomerCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.CustomerCode).IsUnique();

        builder.Property(e => e.CustomerType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TradeName).HasMaxLength(200);
        builder.Property(e => e.TaxCode).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreditLimit).HasPrecision(19, 4);
        builder.Property(e => e.PaymentType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AccountManagerEmployee)
            .WithMany()
            .HasForeignKey(e => e.AccountManagerEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.JobTitle).HasMaxLength(100);
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Contacts)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.AddressType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AddressLabel).HasMaxLength(100);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.CustomerAddresses)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerContractConfiguration : IEntityTypeConfiguration<CustomerContract>
{
    public void Configure(EntityTypeBuilder<CustomerContract> builder)
    {
        builder.ToTable("CustomerContracts", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.ContractNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.ContractNumber).IsUnique();

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DiscountRate).HasPrecision(5, 2);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Contracts)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FileAttachment)
            .WithMany()
            .HasForeignKey(e => e.FileAttachmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CustomerPricingRuleConfiguration : IEntityTypeConfiguration<CustomerPricingRule>
{
    public void Configure(EntityTypeBuilder<CustomerPricingRule> builder)
    {
        builder.ToTable("CustomerPricingRules", "crm");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.DiscountPercent).HasPrecision(5, 2);
        builder.Property(e => e.FlatDiscountAmount).HasPrecision(19, 4);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.PricingRules)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CustomerContract)
            .WithMany()
            .HasForeignKey(e => e.CustomerContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ShippingRateCard)
            .WithMany()
            .HasForeignKey(e => e.ShippingRateCardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
