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
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;

    public SupplyOrderService(
        IRepository<SupplyOrderMaster> masterRepository,
        IRepository<SupplyOrderDetail> detailRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository)
    {
        _masterRepository = masterRepository;
        _detailRepository = detailRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
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
        await ValidateDetailsAsync(request.Details, cancellationToken);

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
        await ValidateDetailsAsync(request.Details, cancellationToken);

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

    private async Task ValidateDetailsAsync(IEnumerable<SupplyOrderDetailUpsertRequest>? details, CancellationToken cancellationToken)
    {
        if (details is null)
            return;

        var seenCustomers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var detail in details)
        {
            if (string.IsNullOrWhiteSpace(detail.CustomerId))
                continue;

            var customerId = detail.CustomerId.Trim();
            if (!seenCustomers.Add(customerId))
            {
                var customerTitle = await _chartOfAccountRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.Id == customerId)
                    .Select(x => x.Title)
                    .FirstOrDefaultAsync(cancellationToken);

                string customerDisplay = !string.IsNullOrWhiteSpace(customerTitle)
                    ? $"'{customerTitle}'"
                    : $"'{customerId}'";

                throw new BadRequestException($"Customer {customerDisplay} cannot be added more than once in the same supply order.");
            }
        }
    }

    private async Task ReplaceDetailsAsync(int supplyOrderId, IEnumerable<SupplyOrderDetailUpsertRequest> details, CancellationToken cancellationToken)
    {
        var incomingList = details
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .Select(x => new
            {
                CustomerId = x.CustomerId.Trim(),
                x.SortOrder
            })
            .ToList();

        var incomingCustomerIds = incomingList
            .Select(x => x.CustomerId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingDetails = await _detailRepository.GetAll()
            .Where(x => x.SupplyOrderMasterId == supplyOrderId)
            .ToListAsync(cancellationToken);

        // Delete rows for customers no longer in the supply order
        foreach (var existing in existingDetails)
        {
            if (existing.CustomerAccountId == null || !incomingCustomerIds.Contains(existing.CustomerAccountId))
            {
                await _detailRepository.DeleteAsync(existing, false);
            }
        }

        var existingByCustomer = existingDetails
            .Where(x => x.CustomerAccountId != null)
            .ToDictionary(x => x.CustomerAccountId!, StringComparer.OrdinalIgnoreCase);

        // Update existing rows or add new rows
        foreach (var item in incomingList)
        {
            if (existingByCustomer.TryGetValue(item.CustomerId, out var existing))
            {
                if (existing.SortOrder != item.SortOrder)
                {
                    existing.SortOrder = item.SortOrder;
                    await _detailRepository.UpdateAsync(existing, false);
                }
            }
            else
            {
                var detail = new SupplyOrderDetail
                {
                    SupplyOrderMasterId = supplyOrderId,
                    CustomerAccountId = item.CustomerId,
                    SortOrder = item.SortOrder
                };

                await _detailRepository.AddAsync(detail, false);
            }
        }

        await _detailRepository.SaveChangesAsync(cancellationToken);
    }
}
