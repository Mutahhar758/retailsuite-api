using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Persistence.Configuration;

public class AccountingPeriodCloseConfig : IEntityTypeConfiguration<AccountingPeriodClose>
{
    public void Configure(EntityTypeBuilder<AccountingPeriodClose> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PeriodName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.ClosingVoucherNo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetEquityAccountId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ReopenedBy).HasMaxLength(200);
        builder.Property(x => x.ReopenReason).HasMaxLength(1000);

        builder.Property(x => x.OpeningStockValue).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ClosingStockValue).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalIncome).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalCogs).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalExpenses).HasColumnType("numeric(18,2)");
        builder.Property(x => x.NetProfitLoss).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalAssets).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalLiabilities).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalEquity).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => new { x.Status, x.ClosingDate });
    }
}
