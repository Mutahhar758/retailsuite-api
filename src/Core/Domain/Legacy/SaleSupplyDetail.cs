using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("SaleSupplyDetail")]
public class SaleSupplyDetail : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string? UnitId { get; set; }
    public decimal Qty { get; set; }
    public decimal? GrossRate { get; set; }
    public decimal? Discount { get; set; }
    public decimal? AddLess { get; set; }
    public string? SecUnitId { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }

    public int? SaleSupplyMasterId { get; set; }
    public string? CustomerAccountId { get; set; }
    public SaleSupplyMaster? SaleSupplyMaster { get; set; }
    public ChartOfAccount? CustomerAccount { get; set; }
    public Unit? Unit { get; set; }
    public Unit? SecUnit { get; set; }
}
