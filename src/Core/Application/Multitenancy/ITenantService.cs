namespace Retailer.Application.Multitenancy;

public interface ITenantService
{
    Task<List<TenantDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<TenantDto> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken);
    Task ActivateAsync(string id, CancellationToken cancellationToken);
    Task DeactivateAsync(string id, CancellationToken cancellationToken);
    Task<TenantDto> GetTenantIdByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken);
}
