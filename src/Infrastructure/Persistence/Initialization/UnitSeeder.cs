using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class UnitSeeder : ICustomSeeder
{
    private readonly IRepository<Unit> _repository;
    private readonly ILogger<UnitSeeder> _logger;

    public UnitSeeder(IRepository<Unit> repository, ILogger<UnitSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (await _repository.GetAll().IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete]).AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding standard Units.");
        var units = new List<Unit>
        {
            new() { Id = "001", Title = "Litre", ShortTitle = "Ltr" },
            new() { Id = "002", Title = "Kilogram", ShortTitle = "kg" },
            new() { Id = "003", Title = "Gram", ShortTitle = "g", ParentUnitId = "002", ConversionRatio = 1000m },
            new() { Id = "004", Title = "Pack", ShortTitle = "Pkt" },
            new() { Id = "005", Title = "Piece", ShortTitle = "Pcs" },
            new() { Id = "006", Title = "Dozen", ShortTitle = "Dzn" },
            new() { Id = "007", Title = "Bag", ShortTitle = "Bag" }
        };

        await _repository.AddRangeAsync(units);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
