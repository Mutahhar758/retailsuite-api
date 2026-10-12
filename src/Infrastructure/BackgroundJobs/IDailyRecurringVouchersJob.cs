using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.BackgroundJobs;

public interface IDailyRecurringVouchersJob : ITransientService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
