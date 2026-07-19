using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("KotOrderItems")]
public class KotOrderItem : AuditableEntity, IAggregateRoot
{
    public int KotOrderId { get; set; }
    public KotOrder KotOrder { get; set; } = default!;
    
    public string ItemId { get; set; } = default!;
    public ItemDetail Item { get; set; } = default!;
    
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Preparing, Ready, Served
}
