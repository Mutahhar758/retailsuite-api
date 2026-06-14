using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Retailer.Domain.Identity;

namespace Retailer.Infrastructure.Persistence.Configuration;

public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder
            .ToTable("users", SchemaNames.Identity)
            .IsMultiTenant()
                .AdjustUniqueIndexes();

        builder
            .Property(u => u.ObjectId)
                .HasMaxLength(256);
    }
}

public class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder) =>
        builder
            .ToTable("roles", SchemaNames.Identity)
            .IsMultiTenant()
                .AdjustUniqueIndexes();
}

public class ApplicationRoleClaimConfig : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder) =>
        builder
            .ToTable("role_claims", SchemaNames.Identity)
            .IsMultiTenant();
}

public class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder) =>
        builder
            .ToTable("user_roles", SchemaNames.Identity)
            .IsMultiTenant();
}

public class IdentityUserClaimConfig : IEntityTypeConfiguration<IdentityUserClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder) =>
        builder
            .ToTable("user_claims", SchemaNames.Identity)
            .IsMultiTenant();
}

public class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder) =>
        builder
            .ToTable("user_logins", SchemaNames.Identity)
            .IsMultiTenant();
}

public class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder) =>
        builder
            .ToTable("user_tokens", SchemaNames.Identity)
            .IsMultiTenant();
}

public class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder
            .ToTable("user_sessions")
            .IsMultiTenant();

        builder
            .Property(s => s.Id)
            .HasColumnType("nvarchar(36)")
            .ValueGeneratedNever();
    }
}