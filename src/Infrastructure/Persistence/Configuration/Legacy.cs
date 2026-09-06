using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Persistence.Configuration;

public class ChartOfAccountConfig : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.ChildAccounts)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefaultAccountConfig : IEntityTypeConfiguration<DefaultAccount>
{
    public void Configure(EntityTypeBuilder<DefaultAccount> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MapAccount)
            .WithMany()
            .HasForeignKey(x => x.MapAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GlEntryConfig : IEntityTypeConfiguration<GlEntry>
{
    public void Configure(EntityTypeBuilder<GlEntry> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VoucherNo, x.VSeq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.DrAccount)
            .WithMany()
            .HasForeignKey(x => x.DrAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CrAccount)
            .WithMany()
            .HasForeignKey(x => x.CrAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.GlEntries)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HrInfoConfig : IEntityTypeConfiguration<HrInfo>
{
    public void Configure(EntityTypeBuilder<HrInfo> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasOne(x => x.ExpenseAccountRef)
            .WithMany()
            .HasForeignKey(x => x.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PayableAccountRef)
            .WithMany()
            .HasForeignKey(x => x.PayableAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerDetailConfig : IEntityTypeConfiguration<CustomerDetail>
{
    public void Configure(EntityTypeBuilder<CustomerDetail> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}

public class ItemCategoryConfig : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasOne(x => x.PrepStation)
            .WithMany()
            .HasForeignKey(x => x.PrepStationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ItemDetailConfig : IEntityTypeConfiguration<ItemDetail>
{
    public void Configure(EntityTypeBuilder<ItemDetail> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ItemType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.QuickQtyPresets)
            .HasMaxLength(1000);

        builder.HasOne(x => x.ItemCategory)
            .WithMany()
            .HasForeignKey(x => x.ItemCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PrimaryUnit)
            .WithMany(x => x.PrimaryUnitItems)
            .HasForeignKey(x => x.PrimaryUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecondaryUnit)
            .WithMany(x => x.SecondaryUnitItems)
            .HasForeignKey(x => x.SecondaryUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultUnit)
            .WithMany(x => x.DefaultUnitItems)
            .HasForeignKey(x => x.DefaultUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ItemTransactionConfig : IEntityTypeConfiguration<ItemTransaction>
{
    public void Configure(EntityTypeBuilder<ItemTransaction> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.ItemTransactions)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollConfig : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VoucherNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.HrInfo)
            .WithMany()
            .HasForeignKey(x => x.HrInfoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExpenseAccount)
            .WithMany()
            .HasForeignKey(x => x.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PayableAccount)
            .WithMany()
            .HasForeignKey(x => x.PayableAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseMasterConfig : IEntityTypeConfiguration<PurchaseMaster>
{
    public void Configure(EntityTypeBuilder<PurchaseMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.PurchaseMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseDetailConfig : IEntityTypeConfiguration<PurchaseDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseDetail> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.PurchaseMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.PurchaseMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.PurchaseDetails)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseRetMasterConfig : IEntityTypeConfiguration<PurchaseRetMaster>
{
    public void Configure(EntityTypeBuilder<PurchaseRetMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.PurchaseRetMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseRetDetailConfig : IEntityTypeConfiguration<PurchaseRetDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseRetDetail> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.PurchaseRetMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.PurchaseRetMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.PurchaseRetDetails)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleMasterConfig : IEntityTypeConfiguration<SaleMaster>
{
    public void Configure(EntityTypeBuilder<SaleMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.SaleMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleConfig : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.SaleMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SaleMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleRetMasterConfig : IEntityTypeConfiguration<SaleRetMaster>
{
    public void Configure(EntityTypeBuilder<SaleRetMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.SaleRetMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleRetDetailConfig : IEntityTypeConfiguration<SaleRetDetail>
{
    public void Configure(EntityTypeBuilder<SaleRetDetail> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.SaleRetMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SaleRetMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.SaleRetDetails)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleSupplyMasterConfig : IEntityTypeConfiguration<SaleSupplyMaster>
{
    public void Configure(EntityTypeBuilder<SaleSupplyMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.SaleSupplyMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupplyOrderMaster)
            .WithMany()
            .HasForeignKey(x => x.SupplyOrderMasterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleSupplyDetailConfig : IEntityTypeConfiguration<SaleSupplyDetail>
{
    public void Configure(EntityTypeBuilder<SaleSupplyDetail> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.SaleSupplyMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SaleSupplyMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CustomerAccount)
            .WithMany()
            .HasForeignKey(x => x.CustomerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.SaleSupplyDetails)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjMasterConfig : IEntityTypeConfiguration<StockAdjMaster>
{
    public void Configure(EntityTypeBuilder<StockAdjMaster> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Narration)
            .WithMany(x => x.StockAdjMasters)
            .HasForeignKey(x => x.NarrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjDetailConfig : IEntityTypeConfiguration<StockAdjDetail>
{
    public void Configure(EntityTypeBuilder<StockAdjDetail> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.VType, x.VNo, x.Seq }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.StockAdjMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.StockAdjMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecUnit)
            .WithMany()
            .HasForeignKey(x => x.SecUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplyOrderMasterConfig : IEntityTypeConfiguration<SupplyOrderMaster>
{
    public void Configure(EntityTypeBuilder<SupplyOrderMaster> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}

public class SupplyOrderDetailConfig : IEntityTypeConfiguration<SupplyOrderDetail>
{
    public void Configure(EntityTypeBuilder<SupplyOrderDetail> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasOne(x => x.SupplyOrderMaster)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SupplyOrderMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CustomerAccount)
            .WithMany()
            .HasForeignKey(x => x.CustomerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompanyDetailConfig : IEntityTypeConfiguration<CompanyDetail>
{
    public void Configure(EntityTypeBuilder<CompanyDetail> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}

public class NarrationConfig : IEntityTypeConfiguration<Narration>
{
    public void Configure(EntityTypeBuilder<Narration> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}

public class SupplierDetailConfig : IEntityTypeConfiguration<SupplierDetail>
{
    public void Configure(EntityTypeBuilder<SupplierDetail> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}

public class UnitConfig : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}

public class PrepStationConfig : IEntityTypeConfiguration<PrepStation>
{
    public void Configure(EntityTypeBuilder<PrepStation> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}

public class DiningTableConfig : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}

public class KotOrderConfig : IEntityTypeConfiguration<KotOrder>
{
    public void Configure(EntityTypeBuilder<KotOrder> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.Table)
            .WithMany()
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KotOrderItemConfig : IEntityTypeConfiguration<KotOrderItem>
{
    public void Configure(EntityTypeBuilder<KotOrderItem> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.KotOrder)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.KotOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerSupplyItemConfig : IEntityTypeConfiguration<CustomerSupplyItem>
{
    public void Configure(EntityTypeBuilder<CustomerSupplyItem> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.CustomerAccountId, x.ItemId }).IsUnique();
        mtBuilder.AdjustUniqueIndexes();

        builder.Property(x => x.Qty).HasPrecision(18, 4);
        builder.Property(x => x.SecQty).HasPrecision(18, 4);
        builder.Property(x => x.Rate).HasPrecision(18, 4);
        builder.Property(x => x.AddLess).HasPrecision(18, 4);
        builder.Property(x => x.Discount).HasPrecision(18, 4);

        builder.HasOne(x => x.CustomerAccount)
            .WithMany()
            .HasForeignKey(x => x.CustomerAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SettingConfig : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(1000);
        builder.Property(x => x.Description).HasMaxLength(250);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.HasIndex(x => x.Key).IsUnique();
        mtBuilder.AdjustUniqueIndexes();
    }
}
