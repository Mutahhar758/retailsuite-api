using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Contracts;
using Retailer.Domain.Public;

namespace Retailer.Domain.Legacy;

[Table("SaleSupplyAttachments")]
public class SaleSupplyAttachment : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = "SP";
    public string VNo { get; set; } = default!;
    public int DocumentId { get; set; }
    public string? Description { get; set; }
    public Document? Document { get; set; }
}
