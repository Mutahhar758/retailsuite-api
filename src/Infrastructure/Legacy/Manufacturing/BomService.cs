using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Manufacturing;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Manufacturing;

internal class BomService : IBomService
{
    private readonly IRepository<BomMaster> _bomMasterRepository;
    private readonly IRepository<BomDetail> _bomDetailRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<Unit> _unitRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;

    public BomService(
        IRepository<BomMaster> bomMasterRepository,
        IRepository<BomDetail> bomDetailRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<Unit> unitRepository,
        IRepository<ItemTransaction> itemTransactionRepository)
    {
        _bomMasterRepository = bomMasterRepository;
        _bomDetailRepository = bomDetailRepository;
        _itemRepository = itemRepository;
        _unitRepository = unitRepository;
        _itemTransactionRepository = itemTransactionRepository;
    }

    public async Task<List<BomResponse>> GetListAsync(BomListFilter filter, CancellationToken cancellationToken)
    {
        var query = _bomMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.FinishedItem)
            .Include(x => x.Unit)
            .Include(x => x.Details)
                .ThenInclude(d => d.RawItem)
            .Include(x => x.Details)
                .ThenInclude(d => d.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Title.Contains(keyword) || x.FinishedItem.Title.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(filter.FinishedItemId))
        {
            query = query.Where(x => x.FinishedItemId == filter.FinishedItemId);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        var boms = await query.OrderByDescending(x => x.Id).ToListAsync(cancellationToken);

        // Fetch latest cost/rates for raw materials
        var rawItemIds = boms.SelectMany(b => b.Details.Select(d => d.RawItemId)).Distinct().ToList();
        var itemRates = await GetItemRatesAsync(rawItemIds, cancellationToken);

        return boms.Select(b => MapToResponse(b, itemRates)).ToList();
    }

    public async Task<BomResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var bom = await _bomMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.FinishedItem)
            .Include(x => x.Unit)
            .Include(x => x.Details)
                .ThenInclude(d => d.RawItem)
            .Include(x => x.Details)
                .ThenInclude(d => d.Unit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (bom is null)
            throw new NotFoundException($"BOM with ID '{id}' was not found.");

        var rawItemIds = bom.Details.Select(d => d.RawItemId).Distinct().ToList();
        var itemRates = await GetItemRatesAsync(rawItemIds, cancellationToken);

        return MapToResponse(bom, itemRates);
    }

    public async Task<BomResponse?> GetDefaultByFinishedItemIdAsync(string finishedItemId, CancellationToken cancellationToken)
    {
        var bom = await _bomMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.FinishedItem)
            .Include(x => x.Unit)
            .Include(x => x.Details)
                .ThenInclude(d => d.RawItem)
            .Include(x => x.Details)
                .ThenInclude(d => d.Unit)
            .Where(x => x.FinishedItemId == finishedItemId && x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (bom is null) return null;

        var rawItemIds = bom.Details.Select(d => d.RawItemId).Distinct().ToList();
        var itemRates = await GetItemRatesAsync(rawItemIds, cancellationToken);

        return MapToResponse(bom, itemRates);
    }

    public async Task<int> CreateAsync(BomCreateRequest request, CancellationToken cancellationToken)
    {
        // If this is set as default, clear any other default BOM for the same finished item
        if (request.IsDefault)
        {
            var existingDefaults = await _bomMasterRepository.GetAll()
                .Where(x => x.FinishedItemId == request.FinishedItemId && x.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                await _bomMasterRepository.UpdateAsync(existing, false);
            }
        }

        var master = new BomMaster
        {
            Code = request.Code,
            Title = request.Title,
            FinishedItemId = request.FinishedItemId,
            OutputQty = request.OutputQty > 0 ? request.OutputQty : 1.0m,
            UnitId = request.UnitId,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            LaborCost = request.LaborCost,
            OverheadCost = request.OverheadCost,
            PackagingCost = request.PackagingCost,
            Notes = request.Notes
        };

        foreach (var line in request.Lines)
        {
            master.Details.Add(new BomDetail
            {
                Seq = line.Seq,
                RawItemId = line.RawItemId,
                Qty = line.Qty,
                UnitId = line.UnitId,
                ScrapPercentage = line.ScrapPercentage,
                IsOptional = line.IsOptional
            });
        }

        await _bomMasterRepository.AddAsync(master);
        return master.Id;
    }

    public async Task UpdateAsync(int id, BomUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _bomMasterRepository.GetAll()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (master is null)
            throw new NotFoundException($"BOM with ID '{id}' was not found.");

        if (request.IsDefault && !master.IsDefault)
        {
            var existingDefaults = await _bomMasterRepository.GetAll()
                .Where(x => x.FinishedItemId == request.FinishedItemId && x.Id != id && x.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                await _bomMasterRepository.UpdateAsync(existing, false);
            }
        }

        master.Code = request.Code;
        master.Title = request.Title;
        master.FinishedItemId = request.FinishedItemId;
        master.OutputQty = request.OutputQty > 0 ? request.OutputQty : 1.0m;
        master.UnitId = request.UnitId;
        master.IsActive = request.IsActive;
        master.IsDefault = request.IsDefault;
        master.LaborCost = request.LaborCost;
        master.OverheadCost = request.OverheadCost;
        master.PackagingCost = request.PackagingCost;
        master.Notes = request.Notes;

        // Sync lines
        master.Details.Clear();
        foreach (var line in request.Lines)
        {
            master.Details.Add(new BomDetail
            {
                BomMasterId = id,
                Seq = line.Seq,
                RawItemId = line.RawItemId,
                Qty = line.Qty,
                UnitId = line.UnitId,
                ScrapPercentage = line.ScrapPercentage,
                IsOptional = line.IsOptional
            });
        }

        await _bomMasterRepository.UpdateAsync(master);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var master = await _bomMasterRepository.GetAll()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (master is null)
            throw new NotFoundException($"BOM with ID '{id}' was not found.");

        await _bomMasterRepository.DeleteAsync(master);
    }

    private async Task<Dictionary<string, decimal>> GetItemRatesAsync(List<string> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return [];

        var items = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PriRate > 0 ? x.PriRate : (x.OpnRate ?? 0), cancellationToken);

        // Check if there are recent purchase or in transactions
        var recentTxs = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId!) && x.QtyIn > 0 && x.Rate > 0)
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.Id)
            .GroupBy(x => x.ItemId!)
            .Select(g => new { ItemId = g.Key, Rate = g.Select(x => x.Rate).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        foreach (var tx in recentTxs)
        {
            if (tx.Rate > 0)
            {
                items[tx.ItemId] = tx.Rate;
            }
        }

        return items;
    }

    private static BomResponse MapToResponse(BomMaster b, Dictionary<string, decimal> itemRates)
    {
        decimal totalMatCost = 0;
        var lines = b.Details.OrderBy(d => d.Seq).Select(d =>
        {
            var rate = itemRates.GetValueOrDefault(d.RawItemId, 0);
            var effectiveQty = d.Qty * (1 + (d.ScrapPercentage / 100m));
            var cost = effectiveQty * rate;
            totalMatCost += cost;

            return new BomLineResponse
            {
                Id = d.Id,
                Seq = d.Seq,
                RawItemId = d.RawItemId,
                RawItemTitle = d.RawItem?.Title ?? d.RawItemId,
                RawItemBarcode = d.RawItem?.Barcode,
                Qty = d.Qty,
                UnitId = d.UnitId ?? d.RawItem?.DefaultUnitId ?? d.RawItem?.PrimaryUnitId,
                UnitTitle = d.Unit?.Title ?? d.RawItem?.DefaultUnit?.Title ?? d.RawItem?.PrimaryUnit?.Title,
                ScrapPercentage = d.ScrapPercentage,
                IsOptional = d.IsOptional,
                CurrentRate = rate,
                TotalEstimatedCost = Math.Round(cost, 2)
            };
        }).ToList();

        var totalOverheads = b.LaborCost + b.OverheadCost + b.PackagingCost;
        var totalCost = totalMatCost + totalOverheads;
        var unitCost = b.OutputQty > 0 ? (totalCost / b.OutputQty) : 0;

        return new BomResponse
        {
            Id = b.Id,
            Code = b.Code,
            Title = b.Title,
            FinishedItemId = b.FinishedItemId,
            FinishedItemTitle = b.FinishedItem?.Title ?? b.FinishedItemId,
            FinishedItemBarcode = b.FinishedItem?.Barcode,
            OutputQty = b.OutputQty,
            UnitId = b.UnitId ?? b.FinishedItem?.DefaultUnitId ?? b.FinishedItem?.PrimaryUnitId,
            UnitTitle = b.Unit?.Title ?? b.FinishedItem?.DefaultUnit?.Title ?? b.FinishedItem?.PrimaryUnit?.Title,
            IsActive = b.IsActive,
            IsDefault = b.IsDefault,
            LaborCost = b.LaborCost,
            OverheadCost = b.OverheadCost,
            PackagingCost = b.PackagingCost,
            TotalEstimatedMaterialCost = Math.Round(totalMatCost, 2),
            TotalEstimatedCost = Math.Round(totalCost, 2),
            UnitEstimatedCost = Math.Round(unitCost, 2),
            Notes = b.Notes,
            Lines = lines
        };
    }
}
