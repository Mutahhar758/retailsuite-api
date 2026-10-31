using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Legacy.Reports;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Reporting.QuestPdf.Documents;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Legacy.Reports;

internal partial class ReportService
{
    public async Task<List<StockLedgerLineResponse>> GetWandaStockLedgerAsync(StockLedgerFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.FkItem))
            throw new BadRequestException("Item is required.");
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var itemInfo = await _itemDetailRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.PrimaryUnit)
            .Include(x => x.SecondaryUnit)
            .Include(x => x.DefaultUnit)
            .FirstOrDefaultAsync(x => x.Id == filter.FkItem, cancellationToken);

        var effectiveDefaultUnitId = !string.IsNullOrWhiteSpace(itemInfo?.DefaultUnitId)
            ? itemInfo.DefaultUnitId
            : (!string.IsNullOrWhiteSpace(itemInfo?.SecondaryUnitId) ? itemInfo.SecondaryUnitId : itemInfo?.PrimaryUnitId);
        var useDefaultSecUnit = !string.IsNullOrWhiteSpace(itemInfo?.SecondaryUnitId)
            && string.Equals(effectiveDefaultUnitId, itemInfo.SecondaryUnitId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(itemInfo.SecondaryUnitId, itemInfo.PrimaryUnitId, StringComparison.OrdinalIgnoreCase);
        var defaultUnitTitle = useDefaultSecUnit
            ? (itemInfo?.DefaultUnit?.ShortTitle ?? itemInfo?.DefaultUnit?.Title ?? itemInfo?.SecondaryUnit?.ShortTitle ?? itemInfo?.SecondaryUnit?.Title ?? effectiveDefaultUnitId)
            : (itemInfo?.DefaultUnit?.ShortTitle ?? itemInfo?.DefaultUnit?.Title ?? itemInfo?.PrimaryUnit?.ShortTitle ?? itemInfo?.PrimaryUnit?.Title ?? effectiveDefaultUnitId);

        var priorAndOpRows = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem && (x.VDate < filter.FromDate || x.VType == "Op"))
            .Select(x => new
            {
                x.VType,
                x.QtyIn,
                x.QtyOut,
                SecQtyIn = x.SecQtyIn ?? 0m,
                SecQtyOut = x.SecQtyOut ?? 0m
            })
            .ToListAsync(cancellationToken);

        // Wanda: opening stock is entered in the item's default unit; other transactions use default unit (SecQty when default is secondary, else Qty).
        var openingStock = priorAndOpRows.Sum(x =>
        {
            if (string.Equals(x.VType, "Op", StringComparison.OrdinalIgnoreCase))
            {
                return useDefaultSecUnit && (x.SecQtyIn != 0m || x.SecQtyOut != 0m)
                    ? (x.SecQtyIn - x.SecQtyOut)
                    : (x.QtyIn - x.QtyOut);
            }
            return useDefaultSecUnit
                ? (x.SecQtyIn - x.SecQtyOut)
                : (x.QtyIn - x.QtyOut);
        });

        var rawMovements = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem
                && x.VType != "Op"
                && x.VDate >= filter.FromDate
                && x.VDate <= filter.ToDate)
            .Select(x => new
            {
                Vdate = x.VDate,
                Vno = x.VType + "-" + x.VNo,
                Particular = x.Account != null ? x.Account.Title : string.Empty,
                QtyIn = x.QtyIn,
                QtyOut = x.QtyOut,
                Rate = x.Rate,
                Amount = x.Amount,
                TranType = x.TranType,
                CostPrice = x.CostPrice,
                CostAmount = x.CostAmount,
                SecUnit = x.SecUnit != null ? x.SecUnit.Title : x.SecUnitId,
                SecQtyIn = x.SecQtyIn ?? 0,
                SecQtyOut = x.SecQtyOut ?? 0
            })
            .OrderBy(x => x.Vdate)
            .ThenBy(x => x.Vno)
            .ToListAsync(cancellationToken);

        var movement = rawMovements.Select(x =>
        {
            decimal? costPrice = null;
            decimal? costAmount = null;

            if (filter.ShowCostPrice)
            {
                if (x.QtyIn > 0 || x.TranType == "in")
                {
                    costPrice = x.Rate;
                    costAmount = x.Amount != 0 ? x.Amount : (x.QtyIn * x.Rate);
                }
                else
                {
                    costPrice = x.CostPrice ?? 0m;
                    costAmount = x.CostAmount ?? ((x.CostPrice ?? 0m) * x.QtyOut);
                }
            }

            var lineQtyIn = useDefaultSecUnit ? x.SecQtyIn : x.QtyIn;
            var lineQtyOut = useDefaultSecUnit ? x.SecQtyOut : x.QtyOut;

            return new StockLedgerLineResponse
            {
                Vdate = x.Vdate,
                Vno = x.Vno,
                Particular = x.Particular,
                QtyIn = lineQtyIn,
                QtyOut = lineQtyOut,
                Rate = x.Rate,
                CostPrice = costPrice,
                CostAmount = costAmount,
                SecUnit = x.SecUnit,
                SecQtyIn = x.SecQtyIn,
                SecQtyOut = x.SecQtyOut,
                PrimaryUnit = defaultUnitTitle
            };
        }).ToList();

        var result = new List<StockLedgerLineResponse>
        {
            new StockLedgerLineResponse
            {
                Vdate = filter.FromDate.AddDays(-1),
                Vno = null,
                Particular = "Openning Stock",
                QtyIn = openingStock >= 0 ? openingStock : 0m,
                QtyOut = openingStock < 0 ? -openingStock : 0m,
                Rate = null,
                CostPrice = null,
                CostAmount = null,
                SecQtyIn = openingStock >= 0 ? openingStock : 0m,
                SecQtyOut = openingStock < 0 ? -openingStock : 0m,
                PrimaryUnit = defaultUnitTitle
            }
        };

        result.AddRange(movement);

        decimal runningBal = 0m;
        decimal runningSecBal = 0m;
        foreach (var line in result)
        {
            runningBal += (line.QtyIn - line.QtyOut);
            runningSecBal += (line.SecQtyIn - line.SecQtyOut);
            line.SecQtyBal = runningSecBal;

            line.QtyInDisplay = line.QtyIn > 0 ? FormatWandaDefaultUnitQty(line.QtyIn, defaultUnitTitle) : null;
            line.QtyOutDisplay = line.QtyOut > 0 ? FormatWandaDefaultUnitQty(line.QtyOut, defaultUnitTitle) : null;
            line.BalanceDisplay = FormatWandaDefaultUnitQty(runningBal, defaultUnitTitle);
        }

        return result;
    }

    public async Task<byte[]> GetWandaStockLedgerPdfAsync(StockLedgerFilter filter, CancellationToken cancellationToken)
    {
        var rawLines = await GetWandaStockLedgerAsync(filter, cancellationToken);

        var itemEntity = await _itemDetailRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.PrimaryUnit)
            .Include(x => x.SecondaryUnit)
            .Include(x => x.DefaultUnit)
            .FirstOrDefaultAsync(x => x.Id == filter.FkItem, cancellationToken);

        var companyName = await _companyDetailRepository.GetAll()
            .AsNoTracking()
            .Select(x => x.CompanyName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Retail Suite Enterprise";

        var item = itemEntity?.Title ?? await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem && x.Item != null)
            .Select(x => x.Item!.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? filter.FkItem;

        var wandaUnitTitle = rawLines.FirstOrDefault()?.PrimaryUnit
            ?? itemEntity?.DefaultUnit?.ShortTitle
            ?? itemEntity?.DefaultUnit?.Title;

        decimal runningBalance = 0m;
        var items = new List<StockLedgerReportItem>();
        for (int i = 0; i < rawLines.Count; i++)
        {
            var line = rawLines[i];
            runningBalance += (line.QtyIn - line.QtyOut);

            items.Add(new StockLedgerReportItem
            {
                Date = line.Vdate,
                VoucherNo = line.Vno ?? "-",
                Particular = line.Particular,
                Rate = line.Rate,
                CostPrice = line.CostPrice,
                CostAmount = line.CostAmount,
                QtyIn = line.QtyIn,
                QtyOut = line.QtyOut,
                Balance = runningBalance,
                QtyInDisplay = line.QtyInDisplay,
                QtyOutDisplay = line.QtyOutDisplay,
                BalanceDisplay = line.BalanceDisplay
            });
        }

        decimal openingBal = items.Count > 0 && items[0].VoucherNo == "-" ? (items[0].QtyIn - items[0].QtyOut) : 0m;
        decimal totalIn = items.Where(x => x.VoucherNo != "-").Sum(x => x.QtyIn);
        decimal totalOut = items.Where(x => x.VoucherNo != "-").Sum(x => x.QtyOut);

        var header = new StockLedgerHeader
        {
            CompanyName = companyName,
            ItemId = filter.FkItem,
            ItemTitle = item,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            OpeningBalance = openingBal,
            TotalIn = totalIn,
            TotalOut = totalOut,
            ClosingBalance = runningBalance,
            OpeningBalanceDisplay = FormatWandaDefaultUnitQty(openingBal, wandaUnitTitle),
            TotalInDisplay = FormatWandaDefaultUnitQty(totalIn, wandaUnitTitle),
            TotalOutDisplay = FormatWandaDefaultUnitQty(totalOut, wandaUnitTitle),
            ClosingBalanceDisplay = FormatWandaDefaultUnitQty(runningBalance, wandaUnitTitle),
            ShowCostPrice = filter.ShowCostPrice,
            GeneratedAt = DateTime.Now
        };

        var document = new WandaStockLedgerDocument(header, items);
        return document.GeneratePdf();
    }

    public async Task<List<StockBalanceLineResponse>> GetWandaStockBalanceAsync(StockBalanceFilter filter, CancellationToken cancellationToken)
    {
        if (filter.FromDate == default && filter.ToDate == default)
        {
            filter.FromDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
            filter.ToDate = DateOnly.FromDateTime(DateTime.Today);
        }
        else if (filter.FromDate == default)
        {
            filter.FromDate = filter.ToDate.AddMonths(-1);
        }
        else if (filter.ToDate == default)
        {
            filter.ToDate = DateOnly.FromDateTime(DateTime.Today);
        }

        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var rows = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= filter.ToDate && !string.IsNullOrWhiteSpace(x.ItemId))
            .Where(x => x.Item != null && x.Item.ItemType != ItemType.Service)
            .Where(x => string.IsNullOrWhiteSpace(filter.Catagory) || (x.Item != null && x.Item.ItemCategoryId == filter.Catagory))
            .Select(x => new
            {
                x.ItemId,
                Item = x.Item != null ? x.Item.Title : string.Empty,
                RawPrimaryUnitId = x.Item != null ? x.Item.PrimaryUnitId : null,
                SecondaryUnitId = x.Item != null ? x.Item.SecondaryUnitId : null,
                DefaultUnitId = x.Item != null ? x.Item.DefaultUnitId : null,
                LowStockAlert = x.Item != null ? x.Item.LowStockAlert : null,
                LowStockQty = x.Item != null ? x.Item.LowStockQty : null,
                PrimaryUnitTitle = x.Item != null && x.Item.PrimaryUnit != null ? (x.Item.PrimaryUnit.ShortTitle ?? x.Item.PrimaryUnit.Title) : null,
                DefaultUnitTitle = x.Item != null && x.Item.DefaultUnit != null ? (x.Item.DefaultUnit.ShortTitle ?? x.Item.DefaultUnit.Title) : null,
                SecondaryUnitTitle = x.Item != null && x.Item.SecondaryUnit != null ? (x.Item.SecondaryUnit.ShortTitle ?? x.Item.SecondaryUnit.Title) : null,
                SecUnit = x.Item != null ? (x.Item.SecondaryUnit != null ? x.Item.SecondaryUnit.Title : x.Item.SecondaryUnitId) : string.Empty,
                x.VDate,
                x.VTime,
                x.VType,
                x.TranType,
                x.QtyIn,
                x.QtyOut,
                SecQtyIn = x.SecQtyIn ?? 0,
                SecQtyOut = x.SecQtyOut ?? 0,
                x.Rate,
                x.RemainingQty,
                x.Id
            })
            .ToListAsync(cancellationToken);

        var data = rows
            .GroupBy(x => new
            {
                x.ItemId,
                x.Item,
                x.RawPrimaryUnitId,
                x.SecondaryUnitId,
                x.DefaultUnitId,
                x.PrimaryUnitTitle,
                x.DefaultUnitTitle,
                x.SecondaryUnitTitle,
                x.SecUnit,
                x.LowStockAlert,
                x.LowStockQty
            })
            .Select(g =>
            {
                var effectiveDefaultUnitId = !string.IsNullOrWhiteSpace(g.Key.DefaultUnitId)
                    ? g.Key.DefaultUnitId
                    : (!string.IsNullOrWhiteSpace(g.Key.SecondaryUnitId) ? g.Key.SecondaryUnitId : g.Key.RawPrimaryUnitId);
                var useDefaultSecUnit = !string.IsNullOrWhiteSpace(g.Key.SecondaryUnitId)
                    && string.Equals(effectiveDefaultUnitId, g.Key.SecondaryUnitId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(g.Key.SecondaryUnitId, g.Key.RawPrimaryUnitId, StringComparison.OrdinalIgnoreCase);
                var defaultUnitTitle = useDefaultSecUnit
                    ? (g.Key.DefaultUnitTitle ?? g.Key.SecondaryUnitTitle ?? effectiveDefaultUnitId ?? string.Empty)
                    : (g.Key.DefaultUnitTitle ?? g.Key.PrimaryUnitTitle ?? effectiveDefaultUnitId ?? string.Empty);

                var priQty = g.Where(x => x.VDate < filter.FromDate || string.Equals(x.VType, "Op", StringComparison.OrdinalIgnoreCase))
                    .Sum(x =>
                    {
                        if (string.Equals(x.VType, "Op", StringComparison.OrdinalIgnoreCase))
                        {
                            return useDefaultSecUnit && (x.SecQtyIn != 0m || x.SecQtyOut != 0m)
                                ? (x.SecQtyIn - x.SecQtyOut)
                                : (x.QtyIn - x.QtyOut);
                        }
                        return useDefaultSecUnit ? (x.SecQtyIn - x.SecQtyOut) : (x.QtyIn - x.QtyOut);
                    });

                var qtyIn = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && !string.Equals(x.VType, "Op", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => useDefaultSecUnit ? x.SecQtyIn : x.QtyIn);
                var qtyOut = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && !string.Equals(x.VType, "Op", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => useDefaultSecUnit ? x.SecQtyOut : x.QtyOut);
                var qty = qtyIn - qtyOut;
                var qtyBal = priQty + qtyIn - qtyOut;

                var fifoStockValue = g
                    .Where(x => string.Equals(x.TranType, "in", StringComparison.OrdinalIgnoreCase) && x.RemainingQty > 0)
                    .Sum(x => x.RemainingQty * x.Rate);
                var rate = qtyBal > 0 ? Math.Round(fifoStockValue / qtyBal, 4) : 0m;

                var status = qtyBal <= 0
                    ? "Out of Stock"
                    : (g.Key.LowStockAlert == true && g.Key.LowStockQty.HasValue && qtyBal <= g.Key.LowStockQty.Value ? "Low Stock" : "In Stock");

                return new StockBalanceLineResponse
                {
                    ItemId = g.Key.ItemId,
                    Item = g.Key.Item,
                    Unit = defaultUnitTitle,
                    PriQty = priQty,
                    Qty = qty,
                    QtyIn = qtyIn,
                    QtyOut = qtyOut,
                    QtyBal = qtyBal,
                    Rate = rate,
                    SecUnit = g.Key.SecUnit,
                    SecPriQty = priQty,
                    SecQtyIn = qtyIn,
                    SecQtyOut = qtyOut,
                    SecQtyBal = qtyBal,
                    PriQtyDisplay = FormatWandaDefaultUnitQty(priQty, defaultUnitTitle),
                    QtyInDisplay = qtyIn > 0 ? FormatWandaDefaultUnitQty(qtyIn, defaultUnitTitle) : null,
                    QtyOutDisplay = qtyOut > 0 ? FormatWandaDefaultUnitQty(qtyOut, defaultUnitTitle) : null,
                    QtyBalDisplay = FormatWandaDefaultUnitQty(qtyBal, defaultUnitTitle),
                    LowStockAlert = g.Key.LowStockAlert,
                    LowStockQty = g.Key.LowStockQty,
                    Status = status
                };
            })
            .Where(x => x.QtyBal != 0 || x.SecQtyBal != 0)
            .ToList();

        var mode = (filter.Filter ?? "All").Trim();
        if (mode.Equals("Equal", StringComparison.OrdinalIgnoreCase) || mode == "= 0")
            data = data.Where(x => x.QtyBal == filter.Qty).ToList();
        else if (mode.Equals("Greater", StringComparison.OrdinalIgnoreCase) || mode == "> 0")
            data = data.Where(x => x.QtyBal > (mode == "> 0" ? 0 : filter.Qty)).ToList();
        else if (mode.Equals("Less", StringComparison.OrdinalIgnoreCase) || mode == "< 0")
            data = data.Where(x => x.QtyBal < (mode == "< 0" ? 0 : filter.Qty)).ToList();
        else if (mode.Equals("LowStock", StringComparison.OrdinalIgnoreCase) || mode.Equals("Low Stock", StringComparison.OrdinalIgnoreCase))
            data = data.Where(x => x.Status == "Low Stock").ToList();

        return data
            .OrderBy(x => x.Item)
            .ToList();
    }

    public async Task<byte[]> GetWandaStockBalancePdfAsync(StockBalanceFilter filter, CancellationToken cancellationToken)
    {
        var rawLines = await GetWandaStockBalanceAsync(filter, cancellationToken);

        var companyName = await _companyDetailRepository.GetAll()
            .AsNoTracking()
            .Select(x => x.CompanyName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Retail Suite Enterprise";

        string categoryName = "All Categories";
        if (!string.IsNullOrWhiteSpace(filter.Catagory))
        {
            categoryName = filter.Catagory;
        }

        var items = new List<StockBalanceReportItem>();
        int index = 1;
        foreach (var line in rawLines)
        {
            items.Add(new StockBalanceReportItem
            {
                Index = index++,
                ItemName = line.Item,
                Unit = line.Unit,
                OpeningQty = line.PriQty,
                QtyIn = line.QtyIn,
                QtyOut = line.QtyOut,
                ClosingQty = line.QtyBal,
                OpeningQtyDisplay = line.PriQtyDisplay,
                QtyInDisplay = line.QtyInDisplay,
                QtyOutDisplay = line.QtyOutDisplay,
                ClosingQtyDisplay = line.QtyBalDisplay,
                Rate = line.Rate,
                LowStockAlert = line.LowStockAlert,
                LowStockQty = line.LowStockQty,
                Status = line.Status
            });
        }

        var header = new StockBalanceHeader
        {
            CompanyName = companyName,
            CategoryName = categoryName,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            TotalItems = items.Count,
            TotalOpeningQty = items.Sum(x => x.OpeningQty),
            TotalQtyIn = items.Sum(x => x.QtyIn),
            TotalQtyOut = items.Sum(x => x.QtyOut),
            TotalClosingQty = items.Sum(x => x.ClosingQty),
            TotalStockValue = items.Sum(x => x.TotalValue),
            GeneratedAt = DateTime.Now
        };

        var document = new WandaStockBalanceDocument(header, items, filter.ShowStockValue);
        return document.GeneratePdf();
    }

    private static string FormatWandaDefaultUnitQty(decimal qty, string? unitTitle)
    {
        if (qty == 0m)
            return "0";
        var unit = unitTitle?.Trim() ?? string.Empty;
        var numText = qty % 1m == 0m ? qty.ToString("N0") : qty.ToString("#,##0.###");
        return string.IsNullOrWhiteSpace(unit) ? numText : $"{numText} {unit}";
    }
}
