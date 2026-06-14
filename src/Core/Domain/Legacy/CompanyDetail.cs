using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("CompanyDetail")]
public class CompanyDetail : AuditableEntity, IAggregateRoot
{
    public string CompanyName { get; set; } = default!;
    public string? UrCompanyName { get; set; }
    public string? Descr { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Cell { get; set; }
    public string? Cell2 { get; set; }
    public string? ContactHeader { get; set; }
}
