using Retailer.Domain.Multitenancy;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Retailer.Infrastructure.Multitenancy;

public class TenantDbContext : EFCoreStoreDbContext<Domain.Multitenancy.TenantInfo>
{
    public DbSet<AiTool> AiTools => Set<AiTool>();
    public DbSet<GeminiApiLog> GeminiApiLogs => Set<GeminiApiLog>();

    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

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
            entity.Property(t => t.HasMobileShopFeature).HasDefaultValue(false);
            entity.Property(t => t.IndustryType).HasMaxLength(64);
            entity.Property(t => t.BusinessSubType).HasMaxLength(64);
            entity.Property(t => t.AiScanMonthlyLimit).HasDefaultValue(50);
            entity.Property(t => t.AiScanUsedThisMonth).HasDefaultValue(0);
            entity.Property(t => t.AiScanCurrentMonth).HasMaxLength(16);
        });

        modelBuilder.Entity<AiTool>(entity =>
        {
            entity.ToTable("AiTools");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasMaxLength(64);
            entity.Property(t => t.ToolName).HasMaxLength(128).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(1024).IsRequired();
            entity.Property(t => t.Category).HasMaxLength(64).IsRequired();
            entity.Property(t => t.JsonSchema).HasColumnType("text").IsRequired();
            entity.Property(t => t.RouteHint).HasMaxLength(256);
            entity.Property(t => t.RequiredPermission).HasMaxLength(128);
            entity.Property(t => t.Embedding).HasColumnType("vector(768)");
            entity.HasIndex(t => t.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
            entity.HasIndex(t => t.RouteHint);
            entity.HasIndex(t => t.Category);
        });

        modelBuilder.Entity<GeminiApiLog>(entity =>
        {
            entity.ToTable("GeminiApiLogs");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(t => t.TenantId).HasMaxLength(64);
            entity.Property(t => t.UserId).HasMaxLength(64);
            entity.Property(t => t.Source).HasMaxLength(64).IsRequired();
            entity.Property(t => t.Model).HasMaxLength(128).IsRequired();
            entity.Property(t => t.Endpoint).HasMaxLength(512).IsRequired();
            entity.Property(t => t.Prompt).HasMaxLength(2048);
            entity.Property(t => t.CurrentRoute).HasMaxLength(256);
            entity.Property(t => t.RequestBody).HasColumnType("text").IsRequired();
            entity.Property(t => t.ResponseBody).HasColumnType("text");
            entity.Property(t => t.ToolCalls).HasMaxLength(512);
            entity.Property(t => t.InputTokens);
            entity.Property(t => t.OutputTokens);
            entity.Property(t => t.TotalTokens);
            entity.Property(t => t.ErrorMessage).HasColumnType("text");
            entity.HasIndex(t => t.CorrelationId);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.TenantId);
        });
    }
}
