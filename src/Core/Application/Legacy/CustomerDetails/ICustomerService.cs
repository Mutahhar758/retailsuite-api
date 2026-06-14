namespace Retailer.Application.Legacy.CustomerDetails;

public interface ICustomerService : ITransientService
{
    Task<List<CustomerResponse>> GetAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, CustomerUpdateRequest request, CancellationToken cancellationToken);
}
