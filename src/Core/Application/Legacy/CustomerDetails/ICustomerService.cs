using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.CustomerDetails;

public interface ICustomerService : ITransientService
{
    Task<List<CustomerResponse>> GetAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, CustomerUpdateRequest request, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken);
    Task<List<CustomerSupplyItemDto>> GetSupplyItemsAsync(string? customerId, string? itemId, CancellationToken cancellationToken);
}

