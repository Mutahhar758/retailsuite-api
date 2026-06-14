namespace Retailer.Domain.Common.Contracts;

public abstract class AuditableEntity : AuditableEntity<DefaultIdType>
{
}

public abstract class AuditableEntity<T> : BaseEntity<T>, IAuditableEntity, ISoftDelete
{
    [IgnoreAuditTrail]
    public string CreatedBy { get; set; }
    [IgnoreAuditTrail]
    public DateTime CreatedOn { get; private set; }
    [IgnoreAuditTrail]
    public string LastModifiedBy { get; set; }
    [IgnoreAuditTrail]
    public DateTime? LastModifiedOn { get; set; }
    [IgnoreAuditTrail]
    public DateTime? DeletedOn { get; set; }
    [IgnoreAuditTrail]
    public string? DeletedBy { get; set; }

    protected AuditableEntity()
    {
        CreatedOn = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
    }
}