using Logistics.Server.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "organization");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TradingName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TaxNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.TaxNumber).IsUnique();
        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Website).HasMaxLength(200);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "organization");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.BranchType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Company)
            .WithMany(c => c.Branches)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "organization");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Departments)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ManagerEmployee)
            .WithMany()
            .HasForeignKey(e => e.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "organization");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.PhoneNumber).HasMaxLength(50);
        builder.Property(e => e.Gender).HasMaxLength(20);
        builder.Property(e => e.JobTitle).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Active");

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Employees)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UserAccount)
            .WithOne(u => u.Employee)
            .HasForeignKey<Domain.Entities.Identity.UserAccount>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", "organization");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalAreaSqM).HasPrecision(12, 2);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Warehouses)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ManagerEmployee)
            .WithMany()
            .HasForeignKey(e => e.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
