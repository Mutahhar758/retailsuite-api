using Microsoft.Extensions.Logging;
using Retailer.Application.Legacy.Inventory;
using Retailer.Shared.Common;

namespace Retailer.Infrastructure.BackgroundJobs;

public class FifoRebuildNightlyJob : IFifoRebuildNightlyJob
{
    private readonly IFifoCostingService _fifoCostingService;
    private readonly ILogger<FifoRebuildNightlyJob> _logger;

    public FifoRebuildNightlyJob(
        IFifoCostingService fifoCostingService,
        ILogger<FifoRebuildNightlyJob> logger)
    {
        _fifoCostingService = fifoCostingService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = PakistanTime.Today;
        _logger.LogInformation("Starting FifoRebuildNightlyJob for date {Date}.", today);

        await _fifoCostingService.FlushAllTenantsAsync(cancellationToken);

        _logger.LogInformation("Completed FifoRebuildNightlyJob for date {Date}.", today);
    }
}
