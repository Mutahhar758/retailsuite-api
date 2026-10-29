using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Vendors;

public interface IVendorService : ITransientService
{
    Task<List<VendorResponse>> GetAsync(CancellationToken cancellationToken);
    Task<Retailer.Application.Common.Models.PaginationResponse<VendorResponse>> GetPagedListAsync(VendorPagedListFilter filter, CancellationToken cancellationToken);
    Task UpsertAsync(string account, VendorUpsertRequest request, CancellationToken cancellationToken);
    Task<string> CreateAsync(VendorCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, VendorUpdateRequest request, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken);
    Task<BulkOperationResult> BulkUpsertAsync(VendorBulkUpsertRequest request, CancellationToken cancellationToken);
}
