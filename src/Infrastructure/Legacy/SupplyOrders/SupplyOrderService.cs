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
        // Deduplicate incoming by CustomerId (last entry wins for sort order)
        var incomingByCustomer = details
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .GroupBy(x => x.CustomerId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().SortOrder, StringComparer.OrdinalIgnoreCase);

        var existingDetails = await _detailRepository.GetAll()
            .Where(x => x.SupplyOrderMasterId == supplyOrderId)
            .ToListAsync(cancellationToken);

        // Remove rows whose customer is no longer in the incoming list
        foreach (var existing in existingDetails)
        {
            if (existing.CustomerAccountId == null || !incomingByCustomer.ContainsKey(existing.CustomerAccountId))
            {
                await _detailRepository.DeleteAsync(existing, false);
            }
        }

        var existingByCustomer = existingDetails
            .Where(x => x.CustomerAccountId != null)
            .ToDictionary(x => x.CustomerAccountId!, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var (customerId, sortOrder) in incomingByCustomer)
        {
            if (existingByCustomer.TryGetValue(customerId, out var existing))
            {
                // Customer already exists — only update SortOrder if it changed.
                // This avoids a soft-delete + re-insert which would hit the unique
                // index on (SupplyOrderMasterId, CustomerAccountId, SortOrder).
                if (existing.SortOrder != sortOrder)
                {
                    existing.SortOrder = sortOrder;
                    await _detailRepository.UpdateAsync(existing, false);
                }
            }
            else
            {
                // New customer — insert a fresh row
                var detail = new SupplyOrderDetail
                {
                    SupplyOrderMasterId = supplyOrderId,
                    CustomerAccountId = customerId,
                    SortOrder = sortOrder
                };

                await _detailRepository.AddAsync(detail, false);
            }
        }

        await _detailRepository.SaveChangesAsync(cancellationToken);
    }
}
