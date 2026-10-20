using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Contracts;

namespace Retailer.Domain.Legacy;

[Table("SaleSupplyAttachments")]
public class SaleSupplyAttachment : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = "SP";
    public string VNo { get; set; } = default!;
    public string MediaId { get; set; } = default!;
    public string? FileName { get; set; }
    public string? Description { get; set; }
}
