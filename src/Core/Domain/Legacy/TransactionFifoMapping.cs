using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Contracts;

namespace Retailer.Domain.Legacy;

[Table("transaction_fifo_mapping")]
public class TransactionFifoMapping : AuditableEntity, IAggregateRoot
{
    public int OutTransactionId { get; set; }
    public int InTransactionId { get; set; }
    public decimal QtyConsumed { get; set; }
    public decimal CostRate { get; set; }
    public decimal CostAmount { get; set; }

    public ItemTransaction OutTransaction { get; set; } = default!;
    public ItemTransaction InTransaction { get; set; } = default!;
}
