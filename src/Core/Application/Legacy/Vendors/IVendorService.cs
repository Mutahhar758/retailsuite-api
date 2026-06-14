namespace Retailer.Application.Legacy.Vendors;

public interface IVendorService : ITransientService
{
    Task<List<VendorResponse>> GetAsync(CancellationToken cancellationToken);
    Task UpsertAsync(string account, VendorUpsertRequest request, CancellationToken cancellationToken);
    Task<string> CreateAsync(VendorCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, VendorUpdateRequest request, CancellationToken cancellationToken);
}
