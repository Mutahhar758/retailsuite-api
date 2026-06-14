using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;

namespace Retailer.Domain.Common.Contracts;

public abstract class BaseEntity : BaseEntity<DefaultIdType>
{
}

public abstract class BaseEntity<TId> : IEntity<TId>
{
    //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public TId Id { get; set; } = default!;
}