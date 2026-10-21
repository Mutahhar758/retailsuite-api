using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Models;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Inventory;

internal class InventoryService : IInventoryService
{
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<Unit> _unitRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<ItemCustomerCategoryPrice> _categoryPriceRepository;
    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly IItemTransactionBalanceService _balanceService;
    private readonly ICurrentTenant _currentTenant;

    public InventoryService(
        IRepository<ItemDetail> itemRepository,
        IRepository<Unit> unitRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<ItemCustomerCategoryPrice> categoryPriceRepository,
        IMediaServiceClient mediaServiceClient,
        IItemTransactionBalanceService balanceService,
        ICurrentTenant currentTenant)
    {
        _itemRepository = itemRepository;
        _unitRepository = unitRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _categoryPriceRepository = categoryPriceRepository;
        _mediaServiceClient = mediaServiceClient;
        _balanceService = balanceService;
        _currentTenant = currentTenant;
    }

    public async Task<List<InventoryItemLookupResponse>> GetItemsLookupAsync(string? itemCategoryCode, CancellationToken cancellationToken)
    {
        var query = _itemRepository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(itemCategoryCode))
            query = query.Where(x => x.ItemCategoryId == itemCategoryCode);

        var rawItems = await query
            .OrderBy(x => x.ItemCategoryId)
            .ThenBy(x => x.Title)
            .Select(x => new
            {
                x.Id,
                Barcode = x.Barcode != null ? x.Barcode.Trim() : null,
                ItemCategoryCode = x.ItemCategoryId,
                x.Title,
                x.ItemKey,
                x.PriRate,
                x.SecRate,
                PrimaryUnit = x.PrimaryUnitId,
                PrimaryUnitTitle = x.PrimaryUnit != null ? (x.PrimaryUnit.ShortTitle ?? x.PrimaryUnit.Title) : null,
                SecondaryUnit = x.SecondaryUnitId,
                DefaultUnit = x.DefaultUnitId,
                DefaultUnitTitle = x.DefaultUnit != null ? (x.DefaultUnit.ShortTitle ?? x.DefaultUnit.Title) : null,
                x.QtyInPack,
                x.MediaId,
                x.QuickQtyPresets,
                x.AdditionalUnits,
                x.RequireImei,
                x.BrandId,
                BrandTitle = x.Brand != null ? x.Brand.Title : null,
                x.ModelName,
                x.Storage,
                x.Ram,
                x.Color
            })
            .ToListAsync(cancellationToken);

        var primaryUnitIds = rawItems
            .Where(x => string.IsNullOrWhiteSpace(x.AdditionalUnits) && !string.IsNullOrWhiteSpace(x.PrimaryUnit))
            .Select(x => x.PrimaryUnit!)
            .Distinct()
            .ToList();

        var subUnits = primaryUnitIds.Count == 0
            ? new List<Unit>()
            : await _unitRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.ParentUnitId != null && u.ConversionRatio > 1m && primaryUnitIds.Contains(u.ParentUnitId))
                .ToListAsync(cancellationToken);

        var items = rawItems.Select(x => new InventoryItemLookupResponse
        {
            Id = x.Id,
            Barcode = x.Barcode,
            ItemCategoryCode = x.ItemCategoryCode ?? string.Empty,
            Title = x.Title,
            ItemKey = x.ItemKey,
            PriRate = x.PriRate,
            SecRate = x.SecRate,
            PrimaryUnit = x.PrimaryUnit,
            PrimaryUnitTitle = x.PrimaryUnitTitle,
            SecondaryUnit = x.SecondaryUnit,
            DefaultUnit = x.DefaultUnit,
            DefaultUnitTitle = x.DefaultUnitTitle,
            QtyInPack = x.QtyInPack,
            MediaId = x.MediaId,
            QuickQtyPresets = x.QuickQtyPresets,
            AdditionalUnits = ItemUnitDisplayHelper.ResolveEffectiveAdditionalUnitsJson(x.PrimaryUnit, x.AdditionalUnits, subUnits),
            RequireImei = x.RequireImei,
            BrandId = x.BrandId,
            BrandTitle = x.BrandTitle,
            ModelName = x.ModelName,
            Storage = x.Storage,
            Ram = x.Ram,
            Color = x.Color
        }).ToList();

        await PopulateMediaUrlsAsync(items, cancellationToken);

        return items;
    }

    public async Task<List<InventoryItemResponse>> GetItemsAsync(string? itemCategoryCode, CancellationToken cancellationToken)
    {
        var query = _itemRepository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(itemCategoryCode))
            query = query.Where(x => x.ItemCategoryId == itemCategoryCode);

        var items = await query
            .Include(x => x.ItemCategory)
            .Include(x => x.Brand)
            .Include(x => x.PrimaryUnit)
            .Include(x => x.SecondaryUnit)
            .Include(x => x.DefaultUnit)
            .OrderBy(x => x.ItemCategoryId)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var mappedItems = items.Select(x => new InventoryItemResponse
        {
            Id = x.Id,
            Barcode = x.Barcode,
            ItemCategoryCode = x.ItemCategoryId,
            ItemCategoryTitle = x.ItemCategory?.Title,
            Title = x.Title,
            ItemKey = x.ItemKey,
            PriRate = x.PriRate,
            SecRate = x.SecRate,
            PrimaryUnit = x.PrimaryUnitId,
            PrimaryUnitTitle = x.PrimaryUnit?.Title,
            SecondaryUnit = x.SecondaryUnitId,
            SecondaryUnitTitle = x.SecondaryUnit?.Title,
            DefaultUnit = x.DefaultUnitId,
            DefaultUnitTitle = x.DefaultUnit?.Title,
            QtyInPack = x.QtyInPack,
            Alert = x.Alert ?? false,
            LowStockAlert = x.LowStockAlert,
            LowStockQty = x.LowStockQty,
            OpnStock = x.OpnStock,
            OpnRate = x.OpnRate,
            ItemType = x.ItemType,
            MediaId = x.MediaId,
            QuickQtyPresets = x.QuickQtyPresets,
            AdditionalUnits = x.AdditionalUnits,
            RequireImei = x.RequireImei,
            BrandId = x.BrandId,
            BrandTitle = x.Brand?.Title,
            ModelName = x.ModelName,
            Storage = x.Storage,
            Ram = x.Ram,
            Color = x.Color
        }).ToList();

        await PopulateMediaUrlsAsync(mappedItems, cancellationToken);

        return mappedItems;
    }

    public async Task<InventoryItemResponse?> GetItemAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.ItemCategory)
            .Include(x => x.Brand)
            .Include(x => x.PrimaryUnit)
            .Include(x => x.SecondaryUnit)
            .Include(x => x.DefaultUnit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;

        var response = new InventoryItemResponse
        {
            Id = item.Id,
            Barcode = item.Barcode != null ? item.Barcode.Trim() : null,
            ItemCategoryCode = item.ItemCategoryId,
            ItemCategoryTitle = item.ItemCategory?.Title,
            Title = item.Title,
            ItemKey = item.ItemKey,
            PriRate = item.PriRate,
            SecRate = item.SecRate,
            PrimaryUnit = item.PrimaryUnitId,
            PrimaryUnitTitle = item.PrimaryUnit?.Title,
            SecondaryUnit = item.SecondaryUnitId,
            SecondaryUnitTitle = item.SecondaryUnit?.Title,
            DefaultUnit = item.DefaultUnitId,
            DefaultUnitTitle = item.DefaultUnit?.Title,
            QtyInPack = item.QtyInPack,
            Alert = item.Alert ?? false,
            LowStockAlert = item.LowStockAlert,
            LowStockQty = item.LowStockQty,
            OpnStock = item.OpnStock,
            OpnRate = item.OpnRate,
            ItemType = item.ItemType,
            MediaId = item.MediaId,
            QuickQtyPresets = item.QuickQtyPresets,
            AdditionalUnits = item.AdditionalUnits,
            RequireImei = item.RequireImei,
            BrandId = item.BrandId,
            BrandTitle = item.Brand?.Title,
            ModelName = item.ModelName,
            Storage = item.Storage,
            Ram = item.Ram,
            Color = item.Color
        };

        if (!string.IsNullOrEmpty(response.MediaId))
        {
            await PopulateMediaUrlsAsync(new List<InventoryItemResponse> { response }, cancellationToken);
        }

        return response;
    }

    public async Task<string> UpsertItemAsync(InventoryItemUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ItemCategoryCode))
            throw new BadRequestException("Item category is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Item title is required.");

        if (request.ItemType != ItemType.Service && string.IsNullOrWhiteSpace(request.PrimaryUnit))
            throw new BadRequestException("Primary unit is required.");

        var normalizedId = (request.Id ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedId) || normalizedId == "0")
        {
            var newId = await GenerateNextItemIdAsync(cancellationToken);

            var isNewService = request.ItemType == ItemType.Service;
            var item = new ItemDetail
            {
                Id = newId,
                ItemType = request.ItemType ?? ItemType.Product,
                ItemCategoryId = request.ItemCategoryCode,
                Barcode = string.IsNullOrWhiteSpace(request.Barcode)
                    ? BuildDefaultBarcode(request.ItemCategoryCode, newId)
                    : request.Barcode.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim(),
                Title = request.Title,
                ItemKey = request.ItemKey,
                PriRate = request.PriRate,
                SecRate = request.SecRate,
                PrimaryUnitId = isNewService ? null : request.PrimaryUnit,
                SecondaryUnitId = isNewService ? null : (string.IsNullOrWhiteSpace(request.SecondaryUnit) ? request.PrimaryUnit : request.SecondaryUnit),
                DefaultUnitId = isNewService ? null : (string.IsNullOrWhiteSpace(request.DefaultUnit)
                    ? (string.IsNullOrWhiteSpace(request.SecondaryUnit) ? request.PrimaryUnit : request.SecondaryUnit)
                    : request.DefaultUnit),
                QtyInPack = isNewService ? null : request.QtyInPack,
                Alert = request.Alert,
                LowStockAlert = isNewService ? null : request.LowStockAlert,
                LowStockQty = isNewService || request.LowStockAlert != true ? null : request.LowStockQty,
                OpnStock = isNewService ? 0 : request.OpnStock,
                OpnRate = isNewService ? 0 : request.OpnRate,
                MediaId = request.MediaId,
                QuickQtyPresets = request.QuickQtyPresets,
                AdditionalUnits = isNewService ? null : (string.IsNullOrWhiteSpace(request.AdditionalUnits) ? null : request.AdditionalUnits),
                RequireImei = request.RequireImei,
                BrandId = request.BrandId,
                ModelName = request.ModelName,
                Storage = request.Storage,
                Ram = request.Ram,
                Color = request.Color
            };

            await _itemRepository.AddAsync(item);

            if (!isNewService && request.OpnStock > 0)
            {
                await CreateOpeningStockTransactionAsync(newId, request.PrimaryUnit, request.OpnStock.Value, request.OpnRate ?? 0, cancellationToken);
            }

            return item.Id;
        }

        var existing = await _itemRepository.GetByIdAsync(normalizedId, cancellationToken);

        if (existing is null)
            throw new NotFoundException($"Item id '{normalizedId}' not found.");

        var isExistingService = existing.ItemType == ItemType.Service;
        var opnStockChanged = existing.OpnStock != request.OpnStock || existing.OpnRate != request.OpnRate;

        existing.ItemCategoryId = request.ItemCategoryCode;
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            existing.Barcode = request.Barcode.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        }
        existing.Title = request.Title;
        existing.ItemKey = request.ItemKey;
        existing.PriRate = request.PriRate;
        existing.SecRate = request.SecRate;
        existing.PrimaryUnitId = isExistingService ? null : request.PrimaryUnit;
        existing.SecondaryUnitId = isExistingService ? null : (string.IsNullOrWhiteSpace(request.SecondaryUnit) ? request.PrimaryUnit : request.SecondaryUnit);
        existing.DefaultUnitId = isExistingService ? null : (string.IsNullOrWhiteSpace(request.DefaultUnit)
            ? (string.IsNullOrWhiteSpace(request.SecondaryUnit) ? request.PrimaryUnit : request.SecondaryUnit)
            : request.DefaultUnit);
        existing.QtyInPack = isExistingService ? null : request.QtyInPack;
        existing.Alert = request.Alert;
        existing.LowStockAlert = isExistingService ? null : request.LowStockAlert;
        existing.LowStockQty = isExistingService || request.LowStockAlert != true ? null : request.LowStockQty;
        existing.OpnStock = isExistingService ? 0 : request.OpnStock;
        existing.OpnRate = isExistingService ? 0 : request.OpnRate;
        existing.MediaId = request.MediaId;
        existing.QuickQtyPresets = request.QuickQtyPresets;
        existing.AdditionalUnits = isExistingService ? null : (string.IsNullOrWhiteSpace(request.AdditionalUnits) ? null : request.AdditionalUnits);
        existing.RequireImei = request.RequireImei;
        existing.BrandId = request.BrandId;
        existing.ModelName = request.ModelName;
        existing.Storage = request.Storage;
        existing.Ram = request.Ram;
        existing.Color = request.Color;

        await _itemRepository.UpdateAsync(existing);

        if (!isExistingService && opnStockChanged && request.OpnStock > 0)
        {
            await UpsertOpeningStockTransactionAsync(normalizedId, request.PrimaryUnit, request.OpnStock.Value, request.OpnRate ?? 0, cancellationToken);
        }

        return existing.Id;
    }

    public async Task DeleteItemAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new BadRequestException("Item id is required.");

        var item = await _itemRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return;

        await _itemRepository.DeleteAsync(item);
    }

    private async Task<string> GenerateNextItemIdAsync(CancellationToken cancellationToken)
    {
        var ids = await _itemRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var max = ids
            .Select(ParseLongOrZero)
            .DefaultIfEmpty(0)
            .Max();

        return (max + 1).ToString("D6");
    }

    private static long ParseLongOrZero(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static string BuildDefaultBarcode(string itemCategoryCode, string id)
    {
        return int.TryParse(itemCategoryCode, out var category)
            ? $"{category}{id}"
            : $"{itemCategoryCode}{id}";
    }

    private async Task CreateOpeningStockTransactionAsync(string itemId, string unit, decimal qty, decimal rate, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var tx = new ItemTransaction
        {
            VDate = today,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = "OP",
            VNo = itemId,
            Seq = 1,
            TranType = "in",
            ItemId = itemId,
            UnitId = unit,
            QtyIn = qty,
            QtyOut = 0,
            RemainingQty = qty,
            Rate = rate,
            Amount = qty * rate,
            Counter = "001"
        };

        await _itemTransactionRepository.AddAsync(tx, false);
        await _balanceService.ProcessVoucherRunningBalancesAsync(_currentTenant.Id, "OP", itemId, today, new[] { tx }, cancellationToken);
        await _itemTransactionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertOpeningStockTransactionAsync(string itemId, string unit, decimal qty, decimal rate, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tx = await _itemTransactionRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == "OP" && x.VNo == itemId && x.Seq == 1, cancellationToken);

        if (tx is null)
        {
            await CreateOpeningStockTransactionAsync(itemId, unit, qty, rate, cancellationToken);
        }
        else
        {
            tx.DeletedOn = null;
            tx.DeletedBy = null;
            tx.VDate = today;
            tx.VTime = TimeOnly.FromDateTime(DateTime.Now);
            tx.UnitId = unit;
            tx.QtyIn = qty;
            tx.QtyOut = 0;
            tx.RemainingQty = qty;
            tx.Rate = rate;
            tx.Amount = qty * rate;

            await _itemTransactionRepository.UpdateAsync(tx, false);
            await _balanceService.ProcessVoucherRunningBalancesAsync(_currentTenant.Id, "OP", itemId, today, new[] { tx }, cancellationToken);
            await _itemTransactionRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        // Enforce that only plain filenames are allowed, preventing path traversal or injection
        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        var response = await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "product", cancellationToken);
        return response ?? throw new InternalServerException("Failed to generate upload URL for product image.");
    }

    private async Task PopulateMediaUrlsAsync(List<InventoryItemResponse> items, CancellationToken cancellationToken)
    {
        var tasks = items
            .Where(x => !string.IsNullOrEmpty(x.MediaId))
            .Select(async item =>
            {
                try
                {
                    var sasResponse = await _mediaServiceClient.GetViewTokenAsync(item.MediaId!, 24, cancellationToken);
                    if (sasResponse != null)
                    {
                        item.MediaUrl = sasResponse.ViewUrl;
                    }
                }
                catch
                {
                    // Fail-safe: ignore media service exceptions to keep main application running
                }
            });
        await Task.WhenAll(tasks);
    }

    private async Task PopulateMediaUrlsAsync(List<InventoryItemLookupResponse> items, CancellationToken cancellationToken)
    {
        var tasks = items
            .Where(x => !string.IsNullOrEmpty(x.MediaId))
            .Select(async item =>
            {
                try
                {
                    var sasResponse = await _mediaServiceClient.GetViewTokenAsync(item.MediaId!, 24, cancellationToken);
                    if (sasResponse != null)
                    {
                        item.MediaUrl = sasResponse.ViewUrl;
                    }
                }
                catch
                {
                    // Fail-safe: ignore media service exceptions to keep main application running
                }
            });
        await Task.WhenAll(tasks);
    }

    public async Task<BulkOperationResult> BulkUpsertAsync(InventoryItemBulkUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult();
        if (request.Items == null || request.Items.Count == 0)
        {
            return result;
        }

        result.TotalProcessed = request.Items.Count;

        var ids = await _itemRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var currentMax = ids
            .Select(ParseLongOrZero)
            .DefaultIfEmpty(0)
            .Max();

        var updateCandidates = request.Items
            .Select((item, idx) => new { Item = item, RowIndex = idx + 1 })
            .Where(x => !string.IsNullOrWhiteSpace(x.Item.Id) && x.Item.Id != "0")
            .ToList();

        var updateIds = updateCandidates.Select(x => x.Item.Id!.Trim()).Distinct().ToList();

        var existingItems = await _itemRepository.GetAll()
            .Where(x => updateIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var newItemsToAdd = new List<ItemDetail>();
        var itemsToUpdate = new List<ItemDetail>();
        var openingStocksToCreate = new List<(string ItemId, string Unit, decimal Qty, decimal Rate)>();
        var openingStocksToUpsert = new List<(string ItemId, string Unit, decimal Qty, decimal Rate)>();

        for (int i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var rowIndex = i + 1;

            if (string.IsNullOrWhiteSpace(item.ItemCategoryCode))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkItemError
                {
                    RowIndex = rowIndex,
                    Identifier = item.Id ?? item.Barcode ?? $"Row #{rowIndex}",
                    ErrorMessage = "Product category is required."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkItemError
                {
                    RowIndex = rowIndex,
                    Identifier = item.Id ?? item.Barcode ?? $"Row #{rowIndex}",
                    ErrorMessage = "Product title/name is required."
                });
                continue;
            }

            var isService = item.ItemType == ItemType.Service;
            if (!isService && string.IsNullOrWhiteSpace(item.PrimaryUnit))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkItemError
                {
                    RowIndex = rowIndex,
                    Identifier = item.Id ?? item.Barcode ?? $"Row #{rowIndex}",
                    ErrorMessage = "Primary unit is required."
                });
                continue;
            }

            var normalizedId = (item.Id ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedId) && normalizedId != "0" && existingItems.TryGetValue(normalizedId, out var existing))
            {
                var isExistingService = existing.ItemType == ItemType.Service;
                var opnStockChanged = existing.OpnStock != item.OpnStock || existing.OpnRate != item.OpnRate;

                existing.ItemCategoryId = item.ItemCategoryCode;
                existing.Title = item.Title;
                existing.ItemKey = item.ItemKey;
                existing.PriRate = item.PriRate;
                existing.SecRate = item.SecRate;
                existing.PrimaryUnitId = isExistingService ? null : item.PrimaryUnit;
                existing.SecondaryUnitId = isExistingService ? null : (string.IsNullOrWhiteSpace(item.SecondaryUnit) ? item.PrimaryUnit : item.SecondaryUnit);
                existing.DefaultUnitId = isExistingService ? null : (string.IsNullOrWhiteSpace(item.DefaultUnit)
                    ? (string.IsNullOrWhiteSpace(item.SecondaryUnit) ? item.PrimaryUnit : item.SecondaryUnit)
                    : item.DefaultUnit);
                existing.QtyInPack = isExistingService ? null : item.QtyInPack;
                existing.Alert = item.Alert;
                existing.LowStockAlert = isExistingService ? null : item.LowStockAlert;
                existing.LowStockQty = isExistingService || item.LowStockAlert != true ? null : item.LowStockQty;
                existing.OpnStock = isExistingService ? 0 : item.OpnStock;
                existing.OpnRate = isExistingService ? 0 : item.OpnRate;
                existing.QuickQtyPresets = item.QuickQtyPresets;
                if (item.AdditionalUnits != null)
                {
                    existing.AdditionalUnits = isExistingService ? null : (string.IsNullOrWhiteSpace(item.AdditionalUnits) ? null : item.AdditionalUnits);
                }
                existing.RequireImei = item.RequireImei;
                existing.BrandId = item.BrandId;
                existing.ModelName = item.ModelName;
                existing.Storage = item.Storage;
                existing.Ram = item.Ram;
                existing.Color = item.Color;

                if (!string.IsNullOrWhiteSpace(item.Barcode))
                {
                    existing.Barcode = item.Barcode.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                }

                itemsToUpdate.Add(existing);

                if (!isExistingService && opnStockChanged)
                {
                    openingStocksToUpsert.Add((existing.Id, existing.PrimaryUnitId ?? item.PrimaryUnit, existing.OpnStock ?? 0, existing.OpnRate ?? 0));
                }

                result.UpdatedCount++;
            }
            else
            {
                currentMax++;
                var newId = currentMax.ToString("D6");
                var isNewService = item.ItemType == ItemType.Service;

                var newItem = new ItemDetail
                {
                    Id = newId,
                    ItemType = item.ItemType ?? ItemType.Product,
                    ItemCategoryId = item.ItemCategoryCode,
                    Barcode = string.IsNullOrWhiteSpace(item.Barcode)
                        ? BuildDefaultBarcode(item.ItemCategoryCode, newId)
                        : item.Barcode.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim(),
                    Title = item.Title,
                    ItemKey = item.ItemKey,
                    PriRate = item.PriRate,
                    SecRate = item.SecRate,
                    PrimaryUnitId = isNewService ? null : item.PrimaryUnit,
                    SecondaryUnitId = isNewService ? null : (string.IsNullOrWhiteSpace(item.SecondaryUnit) ? item.PrimaryUnit : item.SecondaryUnit),
                    DefaultUnitId = isNewService ? null : (string.IsNullOrWhiteSpace(item.DefaultUnit)
                        ? (string.IsNullOrWhiteSpace(item.SecondaryUnit) ? item.PrimaryUnit : item.SecondaryUnit)
                        : item.DefaultUnit),
                    QtyInPack = isNewService ? null : item.QtyInPack,
                    Alert = item.Alert,
                    LowStockAlert = isNewService ? null : item.LowStockAlert,
                    LowStockQty = isNewService || item.LowStockAlert != true ? null : item.LowStockQty,
                    OpnStock = isNewService ? 0 : item.OpnStock,
                    OpnRate = isNewService ? 0 : item.OpnRate,
                    QuickQtyPresets = item.QuickQtyPresets,
                    AdditionalUnits = isNewService ? null : (string.IsNullOrWhiteSpace(item.AdditionalUnits) ? null : item.AdditionalUnits),
                    RequireImei = item.RequireImei,
                    BrandId = item.BrandId,
                    ModelName = item.ModelName,
                    Storage = item.Storage,
                    Ram = item.Ram,
                    Color = item.Color
                };

                newItemsToAdd.Add(newItem);

                if (!isNewService && item.OpnStock > 0)
                {
                    openingStocksToCreate.Add((newId, item.PrimaryUnit, item.OpnStock.Value, item.OpnRate ?? 0));
                }

                result.CreatedCount++;
            }
        }

        if (newItemsToAdd.Count > 0)
            await _itemRepository.AddRangeAsync(newItemsToAdd, doSaveChanges: false);

        if (itemsToUpdate.Count > 0)
            await _itemRepository.UpdateRangeAsync(itemsToUpdate, doSaveChanges: false);

        await _itemRepository.SaveChangesAsync(cancellationToken);

        foreach (var op in openingStocksToCreate)
        {
            await CreateOpeningStockTransactionAsync(op.ItemId, op.Unit, op.Qty, op.Rate, cancellationToken);
        }

        foreach (var op in openingStocksToUpsert)
        {
            await UpsertOpeningStockTransactionAsync(op.ItemId, op.Unit, op.Qty, op.Rate, cancellationToken);
        }

        return result;
    }

    public async Task<List<ItemCustomerCategoryPriceDto>> GetCustomerCategoryPricesAsync(string? customerCategoryId, CancellationToken cancellationToken)
    {
        var query = _categoryPriceRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.CustomerCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerCategoryId))
            query = query.Where(x => x.CustomerCategoryId == customerCategoryId);

        return await query
            .OrderBy(x => x.ItemId)
            .ThenBy(x => x.CustomerCategoryId)
            .Select(x => new ItemCustomerCategoryPriceDto
            {
                Id = x.Id,
                ItemId = x.ItemId,
                CustomerCategoryId = x.CustomerCategoryId,
                CustomerCategoryTitle = x.CustomerCategory != null ? x.CustomerCategory.Title : string.Empty,
                PriRate = x.PriRate,
                SecRate = x.SecRate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ItemCustomerCategoryPriceDto>> GetItemCategoryPricesAsync(string itemId, CancellationToken cancellationToken)
    {
        return await _categoryPriceRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.CustomerCategory)
            .Where(x => x.ItemId == itemId)
            .OrderBy(x => x.CustomerCategory != null ? x.CustomerCategory.Title : x.CustomerCategoryId)
            .Select(x => new ItemCustomerCategoryPriceDto
            {
                Id = x.Id,
                ItemId = x.ItemId,
                CustomerCategoryId = x.CustomerCategoryId,
                CustomerCategoryTitle = x.CustomerCategory != null ? x.CustomerCategory.Title : string.Empty,
                PriRate = x.PriRate,
                SecRate = x.SecRate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveItemCategoryPricesAsync(string itemId, List<ItemCustomerCategoryPriceUpsertRequest> prices, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new BadRequestException("Item ID is required.");

        var existing = await _categoryPriceRepository.GetAll()
            .Where(x => x.ItemId == itemId)
            .ToListAsync(cancellationToken);

        var incomingList = prices ?? new List<ItemCustomerCategoryPriceUpsertRequest>();
        var incomingCatIds = incomingList
            .Where(p => !string.IsNullOrWhiteSpace(p.CustomerCategoryId))
            .Select(p => p.CustomerCategoryId.Trim())
            .ToHashSet();

        var toDelete = existing
            .Where(e => !incomingCatIds.Contains(e.CustomerCategoryId) ||
                        incomingList.Any(p => p.CustomerCategoryId.Trim() == e.CustomerCategoryId && p.PriRate <= 0))
            .ToList();

        if (toDelete.Count > 0)
            await _categoryPriceRepository.DeleteRangeAsync(toDelete, false);

        var existingMap = existing.ToDictionary(x => x.CustomerCategoryId);

        var toAdd = new List<ItemCustomerCategoryPrice>();
        var toUpdate = new List<ItemCustomerCategoryPrice>();

        foreach (var p in incomingList.Where(p => !string.IsNullOrWhiteSpace(p.CustomerCategoryId) && p.PriRate > 0))
        {
            var catId = p.CustomerCategoryId.Trim();
            if (existingMap.TryGetValue(catId, out var existingPrice))
            {
                if (existingPrice.PriRate != p.PriRate || existingPrice.SecRate != p.SecRate)
                {
                    existingPrice.PriRate = p.PriRate;
                    existingPrice.SecRate = p.SecRate;
                    toUpdate.Add(existingPrice);
                }
            }
            else
            {
                toAdd.Add(new ItemCustomerCategoryPrice
                {
                    ItemId = itemId,
                    CustomerCategoryId = catId,
                    PriRate = p.PriRate,
                    SecRate = p.SecRate
                });
            }
        }

        if (toAdd.Count > 0)
            await _categoryPriceRepository.AddRangeAsync(toAdd, false);

        if (toUpdate.Count > 0)
            await _categoryPriceRepository.UpdateRangeAsync(toUpdate, false);

        await _categoryPriceRepository.SaveChangesAsync(cancellationToken);
    }
}
