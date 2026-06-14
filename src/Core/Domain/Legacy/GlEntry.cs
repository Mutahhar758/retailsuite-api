using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("GL1")]
public class GlEntry : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly VTime { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string VType { get; set; } = default!;
    public int VSeq { get; set; }
    public decimal Amount { get; set; }
    public string? NarrationId { get; set; }
    public string? Remarks { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? CheckStatus { get; set; }
    public decimal Clear { get; set; }

    public string? DrAccountId { get; set; }
    public string? CrAccountId { get; set; }
    public ChartOfAccount? DrAccount { get; set; }
    public ChartOfAccount? CrAccount { get; set; }
    public Narration? Narration { get; set; }
}
