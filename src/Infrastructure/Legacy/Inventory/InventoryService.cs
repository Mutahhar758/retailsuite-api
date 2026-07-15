using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Inventory;

internal class InventoryService : IInventoryService
{
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public InventoryService(
        IRepository<ItemDetail> itemRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IMediaServiceClient mediaServiceClient)
    {
        _itemRepository = itemRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<List<InventoryItemLookupResponse>> GetItemsLookupAsync(string? itemCategoryCode, CancellationToken cancellationToken)
    {
        var query = _itemRepository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(itemCategoryCode))
            query = query.Where(x => x.ItemCategoryId == itemCategoryCode);

        var items = await query
            .OrderBy(x => x.ItemCategoryId)
            .ThenBy(x => x.Title)
            .Select(x => new InventoryItemLookupResponse
            {
                Id = x.Id,
                Barcode = x.Barcode,
                ItemCategoryCode = x.ItemCategoryId,
                Title = x.Title,
                ItemKey = x.ItemKey,
                PriRate = x.PriRate,
                SecRate = x.SecRate,
                PrimaryUnit = x.PrimaryUnitId,
                SecondaryUnit = x.SecondaryUnitId,
                DefaultUnit = x.DefaultUnitId
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<List<InventoryItemResponse>> GetItemsAsync(string? itemCategoryCode, CancellationToken cancellationToken)
    {
        var query = _itemRepository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(itemCategoryCode))
            query = query.Where(x => x.ItemCategoryId == itemCategoryCode);

        var items = await query
            .Include(x => x.ItemCategory)
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
            SecondaryUnit = x.SecondaryUnitId,
            DefaultUnit = x.DefaultUnitId,
            QtyInPack = x.QtyInPack,
            Alert = x.Alert ?? false,
            LowStockAlert = x.LowStockAlert,
            OpnStock = x.OpnStock,
            OpnRate = x.OpnRate,
            ItemType = x.ItemType,
            MediaId = x.MediaId,
            QuickQtyPresets = x.QuickQtyPresets
        }).ToList();

        await PopulateMediaUrlsAsync(mappedItems, cancellationToken);

        return mappedItems;
    }

    public async Task<InventoryItemResponse?> GetItemAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.ItemCategory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;

        var response = new InventoryItemResponse
        {
            Id = item.Id,
            Barcode = item.Barcode,
            ItemCategoryCode = item.ItemCategoryId,
            ItemCategoryTitle = item.ItemCategory?.Title,
            Title = item.Title,
            ItemKey = item.ItemKey,
            PriRate = item.PriRate,
            SecRate = item.SecRate,
            PrimaryUnit = item.PrimaryUnitId,
            SecondaryUnit = item.SecondaryUnitId,
            DefaultUnit = item.DefaultUnitId,
            QtyInPack = item.QtyInPack,
            Alert = item.Alert ?? false,
            LowStockAlert = item.LowStockAlert,
            OpnStock = item.OpnStock,
            OpnRate = item.OpnRate,
            ItemType = item.ItemType,
            MediaId = item.MediaId,
            QuickQtyPresets = item.QuickQtyPresets
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
                    : request.Barcode,
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
                OpnStock = isNewService ? 0 : request.OpnStock,
                OpnRate = isNewService ? 0 : request.OpnRate,
                MediaId = request.MediaId,
                QuickQtyPresets = request.QuickQtyPresets
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
        existing.OpnStock = isExistingService ? 0 : request.OpnStock;
        existing.OpnRate = isExistingService ? 0 : request.OpnRate;
        existing.MediaId = request.MediaId;
        existing.QuickQtyPresets = request.QuickQtyPresets;

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
            ? category + id + "\r\n"
            : itemCategoryCode + id + "\r\n";
    }

    private async Task CreateOpeningStockTransactionAsync(string itemId, string unit, decimal qty, decimal rate, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        await _itemTransactionRepository.AddAsync(new ItemTransaction
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
            Rate = rate,
            Amount = qty * rate,
            Counter = "001"
        }, false);

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
            tx.Rate = rate;
            tx.Amount = qty * rate;

            await _itemTransactionRepository.UpdateAsync(tx);
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

        return await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "product", cancellationToken);
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
}
