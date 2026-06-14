using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class ItemCategorySeeder : ICustomSeeder
{
    private readonly IRepository<ItemCategory> _repository;
    private readonly ILogger<ItemCategorySeeder> _logger;

    public ItemCategorySeeder(IRepository<ItemCategory> repository, ILogger<ItemCategorySeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!await _repository.GetAll().IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete]).AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Item Categories for Milk Shop.");
            var categories = new List<ItemCategory>
            {
                new() { Id = "001", Title = "Dairy", ItemType = "Milk Products", Active = true },
                new() { Id = "002", Title = "Bakery", ItemType = "Food", Active = true },
                new() { Id = "003", Title = "Beverages/Drinks", ItemType = "Drinks", Active = true },
                new() { Id = "004", Title = "Snacks", ItemType = "Food", Active = true },
                new() { Id = "005", Title = "Eggs", ItemType = "Poultry", Active = true }
            };

            await _repository.AddRangeAsync(categories);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}
