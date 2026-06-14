using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class NarrationSeeder : ICustomSeeder
{
    private readonly IRepository<Narration> _repository;
    private readonly ILogger<NarrationSeeder> _logger;

    public NarrationSeeder(IRepository<Narration> repository, ILogger<NarrationSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!await _repository.GetAll().IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete]).AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Narrations.");
            var narrations = new List<Narration>
            {
                new() { Id = "001", Title = "Opening Balance" },
                new() { Id = "002", Title = "Cash Sale" },
                new() { Id = "003", Title = "Credit Sale" },
                new() { Id = "004", Title = "Cash Purchase" },
                new() { Id = "005", Title = "Credit Purchase" },
                new() { Id = "006", Title = "Stock Adjustment" }
            };

            await _repository.AddRangeAsync(narrations);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}
