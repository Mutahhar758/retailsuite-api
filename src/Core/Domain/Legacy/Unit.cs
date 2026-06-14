using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("Units")]
public class Unit : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;

    public ICollection<ItemDetail> PrimaryUnitItems { get; set; } = new List<ItemDetail>();
    public ICollection<ItemDetail> SecondaryUnitItems { get; set; } = new List<ItemDetail>();
    public ICollection<ItemDetail> DefaultUnitItems { get; set; } = new List<ItemDetail>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<SaleRetDetail> SaleRetDetails { get; set; } = new List<SaleRetDetail>();
    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
    public ICollection<PurchaseRetDetail> PurchaseRetDetails { get; set; } = new List<PurchaseRetDetail>();
    public ICollection<SaleSupplyDetail> SaleSupplyDetails { get; set; } = new List<SaleSupplyDetail>();
    public ICollection<ItemTransaction> ItemTransactions { get; set; } = new List<ItemTransaction>();
}
