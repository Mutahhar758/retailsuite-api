using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Retailer.Application.Legacy.RecurringChains;
using Retailer.Infrastructure.Auth;
using Retailer.Infrastructure.Multitenancy;
using Retailer.Infrastructure.Persistence.Transactions;
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
                var currentUserInitializer = scope.ServiceProvider.GetService<ICurrentUserInitializer>();
                currentUserInitializer?.SetCurrentUserId("system");

                var txManager = scope.ServiceProvider.GetService<EfTransactionManager>();

                try
                {
                    await recurringService.ProcessTenantDueChainsAsync(today, cancellationToken);

                    // Commit any open transaction started by EfTransactionManager during processing.
                    // Normally EfTransactionMiddleware handles this, but Hangfire jobs run outside the HTTP pipeline.
                    if (txManager?.Transaction != null)
                    {
                        await txManager.Transaction.CommitAsync(cancellationToken);
                        await txManager.Transaction.DisposeAsync();
                        txManager.Transaction = null;
                    }
                }
                catch
                {
                    if (txManager?.Transaction != null)
                    {
                        await txManager.Transaction.RollbackAsync(cancellationToken);
                        await txManager.Transaction.DisposeAsync();
                        txManager.Transaction = null;
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring vouchers for tenant '{TenantIdentifier}'.", tenant.Identifier);
            }
        }

        _logger.LogInformation("Completed DailyRecurringVouchersJob for date {Date}.", today);
    }
}
