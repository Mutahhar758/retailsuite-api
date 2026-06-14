using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Public;
using Retailer.Infrastructure.Persistence.Configuration;
using Retailer.Infrastructure.Persistence.Transactions;
using Finbuckle.MultiTenant.Abstractions;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, EfTransactionManager transactionManager, IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor = null)
        : base(options, currentUser, serializer, dbSettings, transactionManager, multiTenantContextAccessor)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (string.Equals(Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            modelBuilder.HasDefaultSchema(SchemaNames.Public);
        }
    }
}