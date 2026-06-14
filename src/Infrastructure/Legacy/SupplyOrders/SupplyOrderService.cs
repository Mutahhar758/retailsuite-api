using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.SupplyOrders;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.SupplyOrders;

internal class SupplyOrderService : ISupplyOrderService
{
    private readonly IRepository<SupplyOrderMaster> _masterRepository;
    private readonly IRepository<SupplyOrderDetail> _detailRepository;

    public SupplyOrderService(
        IRepository<SupplyOrderMaster> masterRepository,
        IRepository<SupplyOrderDetail> detailRepository)
    {
        _masterRepository = masterRepository;
        _detailRepository = detailRepository;
    }

    public async Task<List<SupplyOrderResponse>> GetAsync(CancellationToken cancellationToken)
    {
        return await _masterRepository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new SupplyOrderResponse
            {
                Id = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplyOrderResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var master = await _masterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SupplyOrderResponse
            {
                Id = x.Id,
                Title = x.Title
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (master is null)
            return null;

        master.Details = await _detailRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.SupplyOrderMasterId == id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CustomerAccountId)
            .Select(x => new SupplyOrderDetailResponse
            {
                CustomerId = x.CustomerAccountId,
                SortOrder = x.SortOrder ?? 0
            })
            .ToListAsync(cancellationToken);

        return master;
    }

    public async Task<int> CreateAsync(SupplyOrderUpsertRequest request, CancellationToken cancellationToken)
    {
        var master = new SupplyOrderMaster
        {
            Title = request.Title.Trim()
        };

        await _masterRepository.AddAsync(master);
        await ReplaceDetailsAsync(master.Id, request.Details, cancellationToken);

        return master.Id;
    }

    public async Task<int> UpdateAsync(int id, SupplyOrderUpsertRequest request, CancellationToken cancellationToken)
    {
        var master = await _masterRepository.GetByIdAsync(id, cancellationToken);
        if (master is null)
            throw new NotFoundException($"Supply order '{id}' not found.");

        master.Title = request.Title.Trim();
        await _masterRepository.UpdateAsync(master);

        await ReplaceDetailsAsync(id, request.Details, cancellationToken);
        return id;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var master = await _masterRepository.GetByIdAsync(id, cancellationToken);
        if (master is null)
            return;

        var existingDetails = await _detailRepository.GetAll()
            .Where(x => x.SupplyOrderMasterId == id)
            .ToListAsync(cancellationToken);

        foreach (var detail in existingDetails)
        {
            await _detailRepository.DeleteAsync(detail);
        }

        await _masterRepository.DeleteAsync(master);
    }

    private async Task ReplaceDetailsAsync(int supplyOrderId, IEnumerable<SupplyOrderDetailUpsertRequest> details, CancellationToken cancellationToken)
    {
        var incomingDetails = details
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .Select(x => new
            {
                CustomerId = x.CustomerId.Trim(),
                SortOrder = x.SortOrder
            })
            .GroupBy(x => BuildDetailKey(x.CustomerId, x.SortOrder), StringComparer.Ordinal)
            .Select(x => x.First())
            .ToList();

        var existingDetails = await _detailRepository.GetAll()
            .Where(x => x.SupplyOrderMasterId == supplyOrderId)
            .ToListAsync(cancellationToken);

        var incomingKeys = new HashSet<string>(
            incomingDetails.Select(x => BuildDetailKey(x.CustomerId, x.SortOrder)),
            StringComparer.Ordinal);

        foreach (var existing in existingDetails)
        {
            var existingKey = BuildDetailKey(existing.CustomerAccountId, existing.SortOrder ?? 0);
            if (!incomingKeys.Contains(existingKey))
            {
                await _detailRepository.DeleteAsync(existing, false);
            }
        }

        var existingKeySet = new HashSet<string>(
            existingDetails.Select(x => BuildDetailKey(x.CustomerAccountId, x.SortOrder ?? 0)),
            StringComparer.Ordinal);

        foreach (var incoming in incomingDetails)
        {
            var key = BuildDetailKey(incoming.CustomerId, incoming.SortOrder);
            if (existingKeySet.Contains(key))
                continue;

            var detail = new SupplyOrderDetail
            {
                SupplyOrderMasterId = supplyOrderId,
                CustomerAccountId = incoming.CustomerId,
                SortOrder = incoming.SortOrder
            };

            await _detailRepository.AddAsync(detail, false);
        }

        await _detailRepository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildDetailKey(string? customerId, int sortOrder)
        => (customerId ?? string.Empty) + "|" + sortOrder;
}
