using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("SupplyOrderDetail")]
public class SupplyOrderDetail : AuditableEntity, IAggregateRoot
{
    public int? SortOrder { get; set; }
    public int? SupplyOrderMasterId { get; set; }
    public string? CustomerAccountId { get; set; }
    public SupplyOrderMaster? SupplyOrderMaster { get; set; }
    public ChartOfAccount? CustomerAccount { get; set; }
}
