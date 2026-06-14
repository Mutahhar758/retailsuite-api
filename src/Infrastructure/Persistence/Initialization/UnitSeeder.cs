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
        if (!await _repository.GetAll().IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete]).AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Units for Milk Shop.");
            var units = new List<Unit>
            {
                new() { Id = "001", Title = "Litre" },
                new() { Id = "002", Title = "Kilogram" },
                new() { Id = "003", Title = "Gram" },
                new() { Id = "004", Title = "Pack" },
                new() { Id = "005", Title = "Piece" },
                new() { Id = "006", Title = "Dozen" }
            };

            await _repository.AddRangeAsync(units);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}
