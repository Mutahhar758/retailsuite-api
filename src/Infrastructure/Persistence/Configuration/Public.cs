using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Retailer.Domain.Public;

namespace Retailer.Infrastructure.Persistence.Configuration;

public class DocumentConfig : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder
            .ToTable("documents")
            .IsMultiTenant();
    }
}
