using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("KotOrders")]
public class KotOrder : AuditableEntity, IAggregateRoot
{
    public int TokenNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public TimeOnly OrderTime { get; set; }
    public string OrderType { get; set; } = "Takeaway"; // Takeaway, DineIn, Delivery
    
    public int? TableId { get; set; }
    public DiningTable? Table { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending, Preparing, Ready, Served, Cancelled
    public string? SaleVoucherNo { get; set; } // Nullable, links to SaleMaster once paid
    public string? CustomerId { get; set; } // Links to CustomerDetail
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    
    public ICollection<KotOrderItem> Details { get; set; } = new List<KotOrderItem>();
}
