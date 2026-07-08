namespace Retailer.Application.Common.Interfaces;

public interface ICurrentTenant
{
    string? Id { get; }
    string? Name { get; }
    string? ConnectionString { get; }
    bool IsValid { get; }
    bool HasSupplyFeature { get; }
    bool HasSecondaryQty { get; }
}
