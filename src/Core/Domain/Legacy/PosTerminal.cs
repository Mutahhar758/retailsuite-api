using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("PosTerminals")]
public class PosTerminal : AuditableEntity, IAggregateRoot
{
    public string MachineId { get; set; } = default!;
    public string TerminalCode { get; set; } = default!;
    public string? MachineName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenOn { get; set; }
}
