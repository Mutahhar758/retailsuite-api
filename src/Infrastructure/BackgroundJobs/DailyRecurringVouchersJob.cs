using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Retailer.Application.Legacy.RecurringChains;
using Retailer.Infrastructure.Multitenancy;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.BackgroundJobs;

public class DailyRecurringVouchersJob : IDailyRecurringVouchersJob
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyRecurringVouchersJob> _logger;

    public DailyRecurringVouchersJob(
        TenantDbContext tenantDbContext,
        IServiceProvider serviceProvider,
        ILogger<DailyRecurringVouchersJob> logger)
    {
        _tenantDbContext = tenantDbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _logger.LogInformation("Starting DailyRecurringVouchersJob for date {Date}.", today);

        var tenants = await _tenantDbContext.TenantInfo
            .AsNoTracking()
            .Where(t => t.IsActive && t.ValidFrom <= DateTime.UtcNow && (t.ValidUntil == null || t.ValidUntil >= DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tenantContextAccessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
                if (tenantContextAccessor is IMultiTenantContextSetter tenantContextSetter)
                {
                    tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
                }

                var recurringService = scope.ServiceProvider.GetRequiredService<IRecurringChainService>();
                await recurringService.ProcessTenantDueChainsAsync(today, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring vouchers for tenant '{TenantIdentifier}'.", tenant.Identifier);
            }
        }

        _logger.LogInformation("Completed DailyRecurringVouchersJob for date {Date}.", today);
    }
}
