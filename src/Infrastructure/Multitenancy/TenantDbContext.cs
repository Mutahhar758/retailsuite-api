using Retailer.Domain.Multitenancy;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Multitenancy;

public class TenantDbContext : EFCoreStoreDbContext<Domain.Multitenancy.TenantInfo>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Multitenancy.TenantInfo>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Identifier).IsUnique();
            entity.Property(t => t.Id).HasMaxLength(64);
            entity.Property(t => t.Identifier).HasMaxLength(64).IsRequired();
            entity.Property(t => t.Name).HasMaxLength(256).IsRequired();
            entity.Property(t => t.ConnectionString).HasMaxLength(1024);
            entity.Property(t => t.DbProvider).HasMaxLength(64);
            entity.Property(t => t.AdminEmail).HasMaxLength(256);
            entity.Property(t => t.LicenseKey).HasMaxLength(256);
            entity.Property(t => t.HasSupplyFeature).HasDefaultValue(true);
            entity.Property(t => t.HasSecondaryQty).HasDefaultValue(false);
            entity.Property(t => t.HasKotFeature).HasDefaultValue(false);
            entity.Property(t => t.HasVariablePackFeature).HasDefaultValue(false);
        });
    }
}
