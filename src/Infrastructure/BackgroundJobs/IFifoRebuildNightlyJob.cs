using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.BackgroundJobs;

public interface IFifoRebuildNightlyJob : ITransientService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
