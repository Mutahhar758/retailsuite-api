namespace Retailer.Domain.Common.Contracts;

public interface ISoftDelete
{
    DateTime? DeletedOn { get; set; }
    string? DeletedBy { get; set; }
}