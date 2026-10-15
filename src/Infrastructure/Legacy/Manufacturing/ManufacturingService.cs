using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Inventory;
using Retailer.Application.Legacy.Manufacturing;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Manufacturing;

internal class ManufacturingService : IManufacturingService
{
    private const string VType = "MF";

    private readonly IRepository<ProductionMaster> _productionMasterRepository;
    private readonly IRepository<ProductionDetail> _productionDetailRepository;
    private readonly IRepository<BomMaster> _bomMasterRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IItemTransactionBalanceService _balanceService;
    private readonly ICurrentTenant _currentTenant;

    public ManufacturingService(
        IRepository<ProductionMaster> productionMasterRepository,
        IRepository<ProductionDetail> productionDetailRepository,
        IRepository<BomMaster> bomMasterRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<GlEntry> glRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IItemTransactionBalanceService balanceService,
        ICurrentTenant currentTenant)
    {
        _productionMasterRepository = productionMasterRepository;
        _productionDetailRepository = productionDetailRepository;
        _bomMasterRepository = bomMasterRepository;
        _itemRepository = itemRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _glRepository = glRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _balanceService = balanceService;
        _currentTenant = currentTenant;
    }

    public async Task<List<ProductionResponse>> GetListAsync(ProductionListFilter filter, CancellationToken cancellationToken)
    {
        var query = _productionMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.FinishedItem)
            .Include(x => x.Unit)
            .Include(x => x.BomMaster)
            .Include(x => x.Narration)
            .Where(x => x.VType == (filter.VType ?? VType));

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.VoucherNo))
            query = query.Where(x => x.VNo == filter.VoucherNo);

        if (!string.IsNullOrWhiteSpace(filter.FinishedItemId))
            query = query.Where(x => x.FinishedItemId == filter.FinishedItemId);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        var list = await query
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.VNo)
            .ToListAsync(cancellationToken);

        return list.Select(m => new ProductionResponse
        {
            Id = m.Id,
            Date = m.VDate,
            Time = m.VTime,
            VType = m.VType,
            VoucherNo = m.VNo,
            BomMasterId = m.BomMasterId,
            BomTitle = m.BomMaster?.Title,
            FinishedItemId = m.FinishedItemId,
            FinishedItemTitle = m.FinishedItem?.Title ?? m.FinishedItemId,
            FinishedItemBarcode = m.FinishedItem?.Barcode,
            TargetQty = m.TargetQty,
            ProducedQty = m.ProducedQty,
            UnitId = m.UnitId,
            UnitTitle = m.Unit?.Title ?? m.FinishedItem?.DefaultUnit?.Title ?? m.FinishedItem?.PrimaryUnit?.Title,
            BatchNo = m.BatchNo,
            ExpiryDate = m.ExpiryDate,
            TotalMaterialCost = m.TotalMaterialCost,
            TotalOverheadCost = m.TotalOverheadCost,
            TotalProductionCost = m.TotalProductionCost,
            UnitProductionCost = m.UnitProductionCost,
            NarrationId = m.NarrationId,
            Narration = m.Narration?.Title,
            Remarks = m.Remarks,
            Status = m.Status,
            CurrentStage = m.CurrentStage,
            IssuedDate = m.IssuedDate,
            CompletedDate = m.CompletedDate,
            CreatedBy = m.CreatedBy,
            CreatedOn = m.CreatedOn,
            LastModifiedBy = m.LastModifiedBy,
            LastModifiedOn = m.LastModifiedOn
        }).ToList();
    }

    public async Task<ProductionResponse> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var master = await _productionMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.FinishedItem)
            .Include(x => x.Unit)
            .Include(x => x.BomMaster)
            .Include(x => x.Narration)
            .Include(x => x.Details)
                .ThenInclude(d => d.RawItem)
            .Include(x => x.Details)
                .ThenInclude(d => d.Unit)
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Manufacturing voucher '{voucherNo}' was not found.");

        var rawItemIds = master.Details.Select(d => d.RawItemId).Distinct().ToList();
        var stockBalances = await GetStockBalancesAsync(rawItemIds, cancellationToken);

        var lines = master.Details.OrderBy(d => d.Seq).Select(d => new ProductionLineResponse
        {
            Seq = d.Seq,
            RawItemId = d.RawItemId,
            RawItemTitle = d.RawItem?.Title ?? d.RawItemId,
            RawItemBarcode = d.RawItem?.Barcode,
            PlannedQty = d.PlannedQty,
            ActualQty = d.ActualQty,
            UnitId = d.UnitId ?? d.RawItem?.DefaultUnitId ?? d.RawItem?.PrimaryUnitId,
            UnitTitle = d.Unit?.Title ?? d.RawItem?.DefaultUnit?.Title ?? d.RawItem?.PrimaryUnit?.Title,
            UnitCost = d.UnitCost,
            TotalCost = d.TotalCost,
            AvailableStock = stockBalances.GetValueOrDefault(d.RawItemId, 0),
            IsIssued = d.IsIssued
        }).ToList();

        return new ProductionResponse
        {
            Id = master.Id,
            Date = master.VDate,
            Time = master.VTime,
            VType = master.VType,
            VoucherNo = master.VNo,
            BomMasterId = master.BomMasterId,
            BomTitle = master.BomMaster?.Title,
            FinishedItemId = master.FinishedItemId,
            FinishedItemTitle = master.FinishedItem?.Title ?? master.FinishedItemId,
            FinishedItemBarcode = master.FinishedItem?.Barcode,
            TargetQty = master.TargetQty,
            ProducedQty = master.ProducedQty,
            UnitId = master.UnitId,
            UnitTitle = master.Unit?.Title ?? master.FinishedItem?.DefaultUnit?.Title ?? master.FinishedItem?.PrimaryUnit?.Title,
            BatchNo = master.BatchNo,
            ExpiryDate = master.ExpiryDate,
            TotalMaterialCost = master.TotalMaterialCost,
            TotalOverheadCost = master.TotalOverheadCost,
            TotalProductionCost = master.TotalProductionCost,
            UnitProductionCost = master.UnitProductionCost,
            NarrationId = master.NarrationId,
            Narration = master.Narration?.Title,
            Remarks = master.Remarks,
            Status = master.Status,
            CurrentStage = master.CurrentStage,
            IssuedDate = master.IssuedDate,
            CompletedDate = master.CompletedDate,
            CreatedBy = master.CreatedBy,
            CreatedOn = master.CreatedOn,
            LastModifiedBy = master.LastModifiedBy,
            LastModifiedOn = master.LastModifiedOn,
            Lines = lines
        };
    }

    public async Task<ProductionEstimateResponse> CalculateEstimateAsync(ProductionEstimateRequest request, CancellationToken cancellationToken)
    {
        BomMaster? bom = null;
        if (request.BomMasterId.HasValue && request.BomMasterId.Value > 0)
        {
            bom = await _bomMasterRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.Details)
                    .ThenInclude(d => d.RawItem)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Unit)
                .FirstOrDefaultAsync(x => x.Id == request.BomMasterId.Value, cancellationToken);
        }
        else
        {
            bom = await _bomMasterRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.Details)
                    .ThenInclude(d => d.RawItem)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Unit)
                .Where(x => x.FinishedItemId == request.FinishedItemId && x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (bom is null)
            throw new NotFoundException($"No active BOM found for item '{request.FinishedItemId}'.");

        var ratio = bom.OutputQty > 0 ? (request.TargetQty / bom.OutputQty) : 1.0m;
        var rawItemIds = bom.Details.Select(d => d.RawItemId).Distinct().ToList();

        var stockBalances = await GetStockBalancesAsync(rawItemIds, cancellationToken);
        var itemRates = await GetItemRatesAsync(rawItemIds, cancellationToken);

        decimal totalMaterialCost = 0;
        var lines = bom.Details.OrderBy(d => d.Seq).Select(d =>
        {
            var rawQty = d.Qty * ratio;
            var scrapQty = rawQty * (d.ScrapPercentage / 100m);
            var reqQty = rawQty + scrapQty;
            var rate = itemRates.GetValueOrDefault(d.RawItemId, 0);
            var cost = reqQty * rate;
            totalMaterialCost += cost;

            var avail = stockBalances.GetValueOrDefault(d.RawItemId, 0);

            return new ProductionEstimateLine
            {
                Seq = d.Seq,
                RawItemId = d.RawItemId,
                RawItemTitle = d.RawItem?.Title ?? d.RawItemId,
                RawItemBarcode = d.RawItem?.Barcode,
                RequiredQty = Math.Round(reqQty, 4),
                UnitId = d.UnitId ?? d.RawItem?.DefaultUnitId ?? d.RawItem?.PrimaryUnitId,
                UnitTitle = d.Unit?.Title ?? d.RawItem?.DefaultUnit?.Title ?? d.RawItem?.PrimaryUnit?.Title,
                ScrapPercentage = d.ScrapPercentage,
                AvailableStock = avail,
                IsShortage = avail < reqQty,
                EstimatedUnitCost = rate,
                EstimatedTotalCost = Math.Round(cost, 2)
            };
        }).ToList();

        var laborCost = bom.LaborCost * ratio;
        var overheadCost = bom.OverheadCost * ratio;
        var packagingCost = bom.PackagingCost * ratio;
        var totalOverhead = laborCost + overheadCost + packagingCost;
        var totalCost = totalMaterialCost + totalOverhead;
        var unitCost = request.TargetQty > 0 ? (totalCost / request.TargetQty) : 0;

        return new ProductionEstimateResponse
        {
            TargetQty = request.TargetQty,
            EstimatedMaterialCost = Math.Round(totalMaterialCost, 2),
            EstimatedLaborCost = Math.Round(laborCost, 2),
            EstimatedOverheadCost = Math.Round(overheadCost, 2),
            EstimatedPackagingCost = Math.Round(packagingCost, 2),
            EstimatedTotalCost = Math.Round(totalCost, 2),
            EstimatedUnitCost = Math.Round(unitCost, 2),
            Lines = lines
        };
    }

    public async Task<string> CreateAsync(ProductionCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _productionMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var rawItemIds = request.Lines.Select(x => x.RawItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var itemMap = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => rawItemIds.Contains(x.Id) || x.Id == request.FinishedItemId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        decimal totalMaterialCost = 0;
        foreach (var line in request.Lines)
        {
            var lineCost = line.ActualQty * line.UnitCost;
            line.TotalCost = lineCost;
            totalMaterialCost += lineCost;
        }

        var totalOverheads = request.LaborCost + request.OverheadCost + request.PackagingCost;
        var totalProductionCost = totalMaterialCost + totalOverheads;
        var unitProductionCost = request.ProducedQty > 0 ? (totalProductionCost / request.ProducedQty) : 0;

        var now = DateTime.UtcNow;
        var master = new ProductionMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            BomMasterId = request.BomMasterId,
            FinishedItemId = request.FinishedItemId,
            TargetQty = request.TargetQty,
            ProducedQty = request.ProducedQty,
            UnitId = string.IsNullOrWhiteSpace(request.UnitId) ? null : request.UnitId,
            BatchNo = request.BatchNo,
            ExpiryDate = request.ExpiryDate,
            TotalMaterialCost = totalMaterialCost,
            TotalOverheadCost = totalOverheads,
            TotalProductionCost = totalProductionCost,
            UnitProductionCost = unitProductionCost,
            NarrationId = request.Narration,
            Remarks = request.Remarks,
            Status = request.Status,
            CurrentStage = request.CurrentStage ?? (request.Status == ProductionStatus.InProgress ? "In Progress" : request.Status.ToString()),
            IssuedDate = (request.Status == ProductionStatus.InProgress || request.Status == ProductionStatus.Completed) ? now : null,
            CompletedDate = request.Status == ProductionStatus.Completed ? now : null
        };

        foreach (var line in request.Lines)
        {
            master.Details.Add(new ProductionDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                RawItemId = line.RawItemId,
                PlannedQty = line.PlannedQty,
                ActualQty = line.ActualQty,
                UnitId = string.IsNullOrWhiteSpace(line.UnitId) ? null : line.UnitId,
                UnitCost = line.UnitCost,
                TotalCost = line.TotalCost,
                IsIssued = request.Status == ProductionStatus.InProgress || request.Status == ProductionStatus.Completed
            });
        }

        await _productionMasterRepository.AddAsync(master);

        // Inventory and Costing
        if (request.Status == ProductionStatus.InProgress || request.Status == ProductionStatus.Completed)
        {
            await ProcessInventoryPostingAsync(master, request.Lines, itemMap, cancellationToken);
            await PostGlEntriesAsync(master, cancellationToken);
        }

        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, ProductionUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _productionMasterRepository.GetAll()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Manufacturing voucher '{voucherNo}' was not found.");

        var rawItemIds = request.Lines.Select(x => x.RawItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var itemMap = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => rawItemIds.Contains(x.Id) || x.Id == request.FinishedItemId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        decimal totalMaterialCost = 0;
        foreach (var line in request.Lines)
        {
            var lineCost = line.ActualQty * line.UnitCost;
            line.TotalCost = lineCost;
            totalMaterialCost += lineCost;
        }

        var totalOverheads = request.LaborCost + request.OverheadCost + request.PackagingCost;
        var totalProductionCost = totalMaterialCost + totalOverheads;
        var unitProductionCost = request.ProducedQty > 0 ? (totalProductionCost / request.ProducedQty) : 0;

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.BomMasterId = request.BomMasterId;
        master.FinishedItemId = request.FinishedItemId;
        master.TargetQty = request.TargetQty;
        master.ProducedQty = request.ProducedQty;
        master.UnitId = string.IsNullOrWhiteSpace(request.UnitId) ? null : request.UnitId;
        master.BatchNo = request.BatchNo;
        master.ExpiryDate = request.ExpiryDate;
        master.TotalMaterialCost = totalMaterialCost;
        master.TotalOverheadCost = totalOverheads;
        master.TotalProductionCost = totalProductionCost;
        master.UnitProductionCost = unitProductionCost;
        master.NarrationId = request.Narration;
        master.Remarks = request.Remarks;
        master.Status = request.Status;
        master.CurrentStage = request.CurrentStage;

        if ((master.Status == ProductionStatus.InProgress || master.Status == ProductionStatus.Completed) && master.IssuedDate == null)
            master.IssuedDate = DateTime.UtcNow;

        if (master.Status == ProductionStatus.Completed && master.CompletedDate == null)
            master.CompletedDate = DateTime.UtcNow;

        master.Details.Clear();
        foreach (var line in request.Lines)
        {
            master.Details.Add(new ProductionDetail
            {
                ProductionMasterId = master.Id,
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                RawItemId = line.RawItemId,
                PlannedQty = line.PlannedQty,
                ActualQty = line.ActualQty,
                UnitId = string.IsNullOrWhiteSpace(line.UnitId) ? null : line.UnitId,
                UnitCost = line.UnitCost,
                TotalCost = line.TotalCost,
                IsIssued = master.Status == ProductionStatus.InProgress || master.Status == ProductionStatus.Completed
            });
        }

        await _productionMasterRepository.UpdateAsync(master);

        if (master.Status == ProductionStatus.InProgress || master.Status == ProductionStatus.Completed)
        {
            await ProcessInventoryPostingAsync(master, request.Lines, itemMap, cancellationToken);
            await PostGlEntriesAsync(master, cancellationToken);
        }
        else
        {
            // If switched back to Planned or Cancelled, remove inventory and GL entries
            await RemoveInventoryTransactionsAsync(voucherNo, cancellationToken);
            await RemoveGlEntriesAsync(voucherNo, cancellationToken);
        }
    }

    public async Task UpdateStatusAsync(string voucherNo, ProductionStatus newStatus, CancellationToken cancellationToken)
    {
        var master = await _productionMasterRepository.GetAll()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Manufacturing voucher '{voucherNo}' was not found.");

        if (master.Status == newStatus) return;

        master.Status = newStatus;
        if (newStatus == ProductionStatus.InProgress)
        {
            master.IssuedDate = DateTime.UtcNow;
            master.CurrentStage = "In Progress";
        }
        else if (newStatus == ProductionStatus.Completed)
        {
            master.CompletedDate = DateTime.UtcNow;
            master.CurrentStage = "Completed";
        }
        else if (newStatus == ProductionStatus.Cancelled)
        {
            master.CurrentStage = "Cancelled";
        }

        await _productionMasterRepository.UpdateAsync(master);

        var rawItemIds = master.Details.Select(x => x.RawItemId).Distinct().ToList();
        var itemMap = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => rawItemIds.Contains(x.Id) || x.Id == master.FinishedItemId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var lines = master.Details.Select(d => new ProductionLineRequest
        {
            Seq = d.Seq,
            RawItemId = d.RawItemId,
            PlannedQty = d.PlannedQty,
            ActualQty = d.ActualQty,
            UnitId = d.UnitId,
            UnitCost = d.UnitCost,
            TotalCost = d.TotalCost,
            IsIssued = newStatus == ProductionStatus.InProgress || newStatus == ProductionStatus.Completed
        }).ToList();

        if (newStatus == ProductionStatus.InProgress || newStatus == ProductionStatus.Completed)
        {
            await ProcessInventoryPostingAsync(master, lines, itemMap, cancellationToken);
            await PostGlEntriesAsync(master, cancellationToken);
        }
        else
        {
            await RemoveInventoryTransactionsAsync(voucherNo, cancellationToken);
            await RemoveGlEntriesAsync(voucherNo, cancellationToken);
        }
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var master = await _productionMasterRepository.GetAll()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Manufacturing voucher '{voucherNo}' was not found.");

        await RemoveInventoryTransactionsAsync(voucherNo, cancellationToken);
        await RemoveGlEntriesAsync(voucherNo, cancellationToken);
        await _productionMasterRepository.DeleteAsync(master);
    }

    private async Task ProcessInventoryPostingAsync(
        ProductionMaster master,
        List<ProductionLineRequest> lines,
        Dictionary<string, ItemDetail> itemMap,
        CancellationToken cancellationToken)
    {
        var txsToProcess = new List<ItemTransaction>();

        // 1. Raw Materials Outflow
        foreach (var line in lines)
        {
            var item = itemMap.GetValueOrDefault(line.RawItemId);
            var resolvedUnitId = line.UnitId ?? item?.DefaultUnitId ?? item?.PrimaryUnitId;

            var existingTx = await _itemTransactionRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == master.VNo && x.Seq == line.Seq, cancellationToken);

            if (existingTx is null)
            {
                existingTx = new ItemTransaction
                {
                    VDate = master.VDate,
                    VTime = master.VTime,
                    VType = VType,
                    VNo = master.VNo,
                    Seq = line.Seq,
                    TranType = "out",
                    AccountId = null,
                    ItemId = line.RawItemId,
                    UnitId = resolvedUnitId,
                    QtyIn = 0,
                    QtyOut = line.ActualQty,
                    Rate = line.UnitCost,
                    Amount = line.ActualQty * line.UnitCost,
                    RemainingQty = 0,
                    Counter = "001"
                };
                await _itemTransactionRepository.AddAsync(existingTx, false);
            }
            else
            {
                existingTx.DeletedOn = null;
                existingTx.DeletedBy = null;
                existingTx.VDate = master.VDate;
                existingTx.VTime = master.VTime;
                existingTx.TranType = "out";
                existingTx.ItemId = line.RawItemId;
                existingTx.UnitId = resolvedUnitId;
                existingTx.QtyIn = 0;
                existingTx.QtyOut = line.ActualQty;
                existingTx.Rate = line.UnitCost;
                existingTx.Amount = line.ActualQty * line.UnitCost;
                await _itemTransactionRepository.UpdateAsync(existingTx, false);
            }

            txsToProcess.Add(existingTx);
        }

        // 2. Finished Good Inflow (only when Completed)
        const int finishedGoodSeq = 99999;
        var existingFgTx = await _itemTransactionRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == master.VNo && x.Seq == finishedGoodSeq, cancellationToken);

        if (master.Status == ProductionStatus.Completed && master.ProducedQty > 0)
        {
            var fgItem = itemMap.GetValueOrDefault(master.FinishedItemId);
            var resolvedFgUnitId = master.UnitId ?? fgItem?.DefaultUnitId ?? fgItem?.PrimaryUnitId;

            if (existingFgTx is null)
            {
                existingFgTx = new ItemTransaction
                {
                    VDate = master.VDate,
                    VTime = master.VTime,
                    VType = VType,
                    VNo = master.VNo,
                    Seq = finishedGoodSeq,
                    TranType = "in",
                    AccountId = null,
                    ItemId = master.FinishedItemId,
                    UnitId = resolvedFgUnitId,
                    QtyIn = master.ProducedQty,
                    QtyOut = 0,
                    RemainingQty = master.ProducedQty,
                    Rate = master.UnitProductionCost,
                    Amount = master.TotalProductionCost,
                    Counter = "001"
                };
                await _itemTransactionRepository.AddAsync(existingFgTx, false);
            }
            else
            {
                existingFgTx.DeletedOn = null;
                existingFgTx.DeletedBy = null;
                existingFgTx.VDate = master.VDate;
                existingFgTx.VTime = master.VTime;
                existingFgTx.TranType = "in";
                existingFgTx.ItemId = master.FinishedItemId;
                existingFgTx.UnitId = resolvedFgUnitId;
                existingFgTx.QtyIn = master.ProducedQty;
                existingFgTx.QtyOut = 0;
                existingFgTx.RemainingQty = master.ProducedQty;
                existingFgTx.Rate = master.UnitProductionCost;
                existingFgTx.Amount = master.TotalProductionCost;
                await _itemTransactionRepository.UpdateAsync(existingFgTx, false);
            }

            txsToProcess.Add(existingFgTx);
        }
        else if (existingFgTx is not null)
        {
            await _itemTransactionRepository.DeleteAsync(existingFgTx, false);
        }

        await _itemTransactionRepository.SaveChangesAsync(cancellationToken);
        await _balanceService.ProcessVoucherRunningBalancesAsync(_currentTenant.Id, VType, master.VNo, master.VDate, txsToProcess, cancellationToken);
    }

    private async Task PostGlEntriesAsync(ProductionMaster master, CancellationToken cancellationToken)
    {
        await RemoveGlEntriesAsync(master.VNo, cancellationToken);

        // Fetch default accounts if configured
        var accounts = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var wipAccount = accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Work in Progress", StringComparison.OrdinalIgnoreCase))?.AccountId;
        var rawMaterialAccount = accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Raw Material", StringComparison.OrdinalIgnoreCase))?.AccountId
            ?? accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Purchases", StringComparison.OrdinalIgnoreCase))?.AccountId;
        var finishedGoodsAccount = accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Finished Goods", StringComparison.OrdinalIgnoreCase))?.AccountId
            ?? accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Stock", StringComparison.OrdinalIgnoreCase))?.AccountId;
        var overheadAccount = accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Manufacturing Absorption", StringComparison.OrdinalIgnoreCase))?.AccountId
            ?? accounts.FirstOrDefault(x => x.Title != null && x.Title.Contains("Direct Expense", StringComparison.OrdinalIgnoreCase))?.AccountId;

        if (string.IsNullOrWhiteSpace(rawMaterialAccount) || string.IsNullOrWhiteSpace(finishedGoodsAccount))
        {
            // If GL accounts are not mapped yet, inventory transaction remains accurate
            return;
        }

        var seq = 1;
        if (master.Status == ProductionStatus.InProgress)
        {
            // DR WIP, CR Raw Materials
            var targetDr = wipAccount ?? finishedGoodsAccount;
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = master.VDate,
                VTime = master.VTime,
                VType = VType,
                VoucherNo = master.VNo,
                VSeq = seq++,
                DrAccountId = targetDr,
                CrAccountId = rawMaterialAccount,
                Amount = master.TotalMaterialCost,
                NarrationId = master.NarrationId,
                Remarks = $"Material Issued for Batch {master.VNo} - {master.Remarks}"
            });
        }
        else if (master.Status == ProductionStatus.Completed)
        {
            // DR Finished Goods
            // CR Raw Materials (or CR WIP)
            // CR Overheads
            var sourceCr = wipAccount ?? rawMaterialAccount;
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = master.VDate,
                VTime = master.VTime,
                VType = VType,
                VoucherNo = master.VNo,
                VSeq = seq++,
                DrAccountId = finishedGoodsAccount,
                CrAccountId = sourceCr,
                Amount = master.TotalMaterialCost,
                NarrationId = master.NarrationId,
                Remarks = $"Finished Goods from Batch {master.VNo} - {master.Remarks}"
            });

            if (master.TotalOverheadCost > 0 && !string.IsNullOrWhiteSpace(overheadAccount))
            {
                await _glRepository.AddAsync(new GlEntry
                {
                    VDate = master.VDate,
                    VTime = master.VTime,
                    VType = VType,
                    VoucherNo = master.VNo,
                    VSeq = seq++,
                    DrAccountId = finishedGoodsAccount,
                    CrAccountId = overheadAccount,
                    Amount = master.TotalOverheadCost,
                    NarrationId = master.NarrationId,
                    Remarks = $"Labor/Overheads applied to Batch {master.VNo}"
                });
            }
        }
    }

    private async Task RemoveInventoryTransactionsAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var txs = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        if (txs.Count > 0)
        {
            await _itemTransactionRepository.DeleteRangeAsync(txs);
            var date = txs.First().VDate;
            await _balanceService.ProcessVoucherRunningBalancesAsync(_currentTenant.Id, VType, voucherNo, date, [], cancellationToken);
        }
    }

    private async Task RemoveGlEntriesAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var gls = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        if (gls.Count > 0)
        {
            await _glRepository.DeleteRangeAsync(gls);
        }
    }

    private async Task<Dictionary<string, decimal>> GetStockBalancesAsync(List<string> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return [];

        var latestTxs = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId!))
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.Id)
            .GroupBy(x => x.ItemId!)
            .Select(g => new { ItemId = g.Key, Balance = g.Select(x => x.RunningQtyBalance).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        return latestTxs.ToDictionary(x => x.ItemId, x => x.Balance);
    }

    private async Task<Dictionary<string, decimal>> GetItemRatesAsync(List<string> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return [];

        var items = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PriRate > 0 ? x.PriRate : (x.OpnRate ?? 0), cancellationToken);

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
}
