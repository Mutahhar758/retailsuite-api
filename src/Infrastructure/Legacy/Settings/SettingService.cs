using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Settings;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Settings;

internal class SettingService : ISettingService
{
    private const string BillThankYouKey = "Bill.ThankYouMessage";
    private const string BillThankYouDefault = "Thank you for shopping with us!";

    private readonly IRepository<Setting> _repository;

    public SettingService(IRepository<Setting> repository)
    {
        _repository = repository;
    }

    public async Task<List<SettingResponse>> GetAllAsync(string? category, CancellationToken cancellationToken)
    {
        var query = _repository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        var list = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Key)
            .Select(x => new SettingResponse
            {
                Key = x.Key,
                Value = x.Value,
                Description = x.Description,
                Category = x.Category
            })
            .ToListAsync(cancellationToken);

        // If Bill.ThankYouMessage is not yet in the DB, add it as default to the response
        if ((string.IsNullOrWhiteSpace(category) || category.Equals("Bill", StringComparison.OrdinalIgnoreCase)) &&
            !list.Any(x => x.Key == BillThankYouKey))
        {
            list.Add(new SettingResponse
            {
                Key = BillThankYouKey,
                Value = BillThankYouDefault,
                Description = "Customer bill and receipt thank you message",
                Category = "Bill"
            });
        }

        return list;
    }

    public async Task<SettingResponse?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (entity == null)
        {
            if (key == BillThankYouKey)
            {
                return new SettingResponse
                {
                    Key = BillThankYouKey,
                    Value = BillThankYouDefault,
                    Description = "Customer bill and receipt thank you message",
                    Category = "Bill"
                };
            }

            return null;
        }

        return new SettingResponse
        {
            Key = entity.Key,
            Value = entity.Value,
            Description = entity.Description,
            Category = entity.Category
        };
    }

    public async Task<SettingResponse> UpsertAsync(SettingUpdateRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetAll()
            .FirstOrDefaultAsync(x => x.Key == request.Key, cancellationToken);

        if (existing != null)
        {
            existing.Value = request.Value;
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                existing.Description = request.Description;
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                existing.Category = request.Category;
            }

            await _repository.UpdateAsync(existing);
        }
        else
        {
            var newSetting = new Setting
            {
                Key = request.Key,
                Value = request.Value,
                Description = request.Description ?? (request.Key == BillThankYouKey ? "Customer bill and receipt thank you message" : null),
                Category = request.Category ?? (request.Key.StartsWith("Bill.", StringComparison.OrdinalIgnoreCase) ? "Bill" : "General")
            };

            await _repository.AddAsync(newSetting);
        }

        return new SettingResponse
        {
            Key = request.Key,
            Value = request.Value,
            Description = request.Description,
            Category = request.Category
        };
    }

    public async Task<List<SettingResponse>> BatchUpsertAsync(List<SettingUpdateRequest> requests, CancellationToken cancellationToken)
    {
        var responses = new List<SettingResponse>();
        foreach (var req in requests)
        {
            var result = await UpsertAsync(req, cancellationToken);
            responses.Add(result);
        }

        return responses;
    }
}
