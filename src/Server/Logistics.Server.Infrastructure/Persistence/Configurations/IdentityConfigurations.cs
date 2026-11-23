using Logistics.Server.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Server.Infrastructure.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts", "identity");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Username).IsUnique();

        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SecurityStamp).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "identity");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PublicId).IsRequired();
        builder.HasIndex(e => e.PublicId).IsUnique();

        builder.Property(e => e.Module).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "identity");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        builder.Property(e => e.AssignedBy).HasMaxLength(100);
        builder.Property(e => e.AssignedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "identity");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        builder.Property(e => e.GrantedBy).HasMaxLength(100);
        builder.Property(e => e.GrantedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserBranchAccessConfiguration : IEntityTypeConfiguration<UserBranchAccess>
{
    public void Configure(EntityTypeBuilder<UserBranchAccess> builder)
    {
        builder.ToTable("UserBranchAccess", "identity");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.UserId, e.BranchId }).IsUnique();
        builder.Property(e => e.GrantedBy).HasMaxLength(100);
        builder.Property(e => e.GrantedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.User)
            .WithMany(u => u.BranchAccesses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "identity");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(e => e.TokenHash);

        builder.Property(e => e.ReplacedByToken).HasMaxLength(256);
        builder.Property(e => e.CreatedByIp).HasMaxLength(50);
        builder.Property(e => e.ExpiresAt).HasPrecision(7);
        builder.Property(e => e.RevokedAt).HasPrecision(7);
        builder.Property(e => e.CreatedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", "identity");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionTokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(e => e.SessionTokenHash);

        builder.Property(e => e.DeviceInfo).HasMaxLength(250);
        builder.Property(e => e.IpAddress).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StartedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.LastActivityAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.ExpiresAt).HasPrecision(7);

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserSessions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories", "identity");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UsernameAttempted).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(50).IsRequired();
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.FailureReason).HasMaxLength(200);
        builder.Property(e => e.AttemptedAt).HasPrecision(7).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.User)
            .WithMany(u => u.LoginHistories)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
