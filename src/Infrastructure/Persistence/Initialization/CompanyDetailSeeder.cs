using Retailer.Application.Common.Persistence;
using Retailer.Domain.Public;
using Retailer.Domain.Multitenancy;
using Retailer.Domain.Legacy;
using Finbuckle.MultiTenant.Abstractions;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class CompanyDetailSeeder : ICustomSeeder
{
    private readonly IRepository<CompanyDetail> _repository;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor;
    private readonly ILogger<CompanyDetailSeeder> _logger;

    public CompanyDetailSeeder(
        IRepository<CompanyDetail> repository,
        IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
        ILogger<CompanyDetailSeeder> logger)
    {
        _repository = repository;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!await _repository.GetAll().AnyAsync(cancellationToken))
        {
            var tenantInfo = _tenantContextAccessor.MultiTenantContext?.TenantInfo;
            if (tenantInfo != null)
            {
                _logger.LogInformation("Seeding Company Detail from Tenant Info.");
                var companyDetail = new CompanyDetail
                {
                    CompanyName = tenantInfo.Name,
                    Address = "",
                    Phone = "",
                    Descr = ""
                };

                await _repository.AddAsync(companyDetail);
                await _repository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
