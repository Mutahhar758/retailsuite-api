using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("Narration")]
public class Narration : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;

    public ICollection<GlEntry> GlEntries { get; set; } = new List<GlEntry>();
    public ICollection<PurchaseMaster> PurchaseMasters { get; set; } = new List<PurchaseMaster>();
    public ICollection<PurchaseRetMaster> PurchaseRetMasters { get; set; } = new List<PurchaseRetMaster>();
    public ICollection<SaleMaster> SaleMasters { get; set; } = new List<SaleMaster>();
    public ICollection<SaleRetMaster> SaleRetMasters { get; set; } = new List<SaleRetMaster>();
    public ICollection<SaleSupplyMaster> SaleSupplyMasters { get; set; } = new List<SaleSupplyMaster>();
    public ICollection<StockAdjMaster> StockAdjMasters { get; set; } = new List<StockAdjMaster>();
}
