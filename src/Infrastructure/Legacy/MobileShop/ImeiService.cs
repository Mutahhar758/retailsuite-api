using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.MobileShop;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.MobileShop;

internal class ImeiService : IImeiService
{
    private readonly IRepository<PurchaseDetail> _purchaseDetailRepository;
    private readonly IRepository<PurchaseMaster> _purchaseMasterRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SaleMaster> _saleMasterRepository;
    private readonly IRepository<SaleRetDetail> _saleRetDetailRepository;
    private readonly IRepository<PurchaseRetDetail> _purchaseRetDetailRepository;
    private readonly IRepository<ImeiCostAddition> _costAdditionRepository;
    private readonly IRepository<RepairJob> _repairJobRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ChartOfAccount> _accountRepository;

    public ImeiService(
        IRepository<PurchaseDetail> purchaseDetailRepository,
        IRepository<PurchaseMaster> purchaseMasterRepository,
        IRepository<Sale> saleRepository,
        IRepository<SaleMaster> saleMasterRepository,
        IRepository<SaleRetDetail> saleRetDetailRepository,
        IRepository<PurchaseRetDetail> purchaseRetDetailRepository,
        IRepository<ImeiCostAddition> costAdditionRepository,
        IRepository<RepairJob> repairJobRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<ChartOfAccount> accountRepository)
    {
        _purchaseDetailRepository = purchaseDetailRepository;
        _purchaseMasterRepository = purchaseMasterRepository;
        _saleRepository = saleRepository;
        _saleMasterRepository = saleMasterRepository;
        _saleRetDetailRepository = saleRetDetailRepository;
        _purchaseRetDetailRepository = purchaseRetDetailRepository;
        _costAdditionRepository = costAdditionRepository;
        _repairJobRepository = repairJobRepository;
        _itemRepository = itemRepository;
        _accountRepository = accountRepository;
    }

    public async Task<List<ImeiStockResponse>> GetImeiStockAsync(CancellationToken cancellationToken)
    {
        // 1. Get all purchased IMEIs
        var purchases = await (
            from d in _purchaseDetailRepository.GetAll().AsNoTracking()
            join m in _purchaseMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where !string.IsNullOrWhiteSpace(d.Imei)
            select new
            {
                d.Imei,
                d.Imei2,
                d.ItemId,
                ItemTitle = i != null ? i.Title : string.Empty,
                BrandTitle = i != null && i.Brand != null ? i.Brand.Title : null,
                ModelName = i != null ? i.ModelName : null,
                Storage = i != null ? i.Storage : null,
                Ram = i != null ? i.Ram : null,
                Color = i != null ? i.Color : null,
                d.PtaStatus,
                d.ConditionNote,
                d.BatteryHealth,
                PurchaseVNo = d.VNo,
                m.VDate,
                d.Rate
            }
        ).ToListAsync(cancellationToken);

        // 2. Get all sold IMEIs
        var sales = await (
            from s in _saleRepository.GetAll().AsNoTracking()
            join m in _saleMasterRepository.GetAll().AsNoTracking()
                on new { s.VType, s.VNo } equals new { m.VType, VNo = m.VNo }
            where !string.IsNullOrWhiteSpace(s.Imei) && s.VType == "SL"
            select new
            {
                s.Imei,
                SaleVNo = s.VNo,
                SaleDate = m.VDate,
                SaleRate = s.GrossRate ?? 0
            }
        ).ToDictionaryAsync(x => x.Imei!, cancellationToken);

        // 3. Get all cost additions grouped by IMEI
        var costAdditions = await _costAdditionRepository.GetAll().AsNoTracking()
            .GroupBy(x => x.Imei)
            .Select(g => new { Imei = g.Key, TotalAdded = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Imei, x => x.TotalAdded, cancellationToken);

        // 4. Build response: phones purchased but not currently sold
        var list = new List<ImeiStockResponse>();
        foreach (var p in purchases)
        {
            var isSold = sales.TryGetValue(p.Imei!, out var saleInfo);
            var added = costAdditions.GetValueOrDefault(p.Imei!, 0);

            list.Add(new ImeiStockResponse
            {
                Imei = p.Imei!,
                Imei2 = p.Imei2,
                ItemId = p.ItemId!,
                ItemTitle = p.ItemTitle,
                BrandTitle = p.BrandTitle,
                ModelName = p.ModelName,
                Storage = p.Storage,
                Ram = p.Ram,
                Color = p.Color,
                PtaStatus = p.PtaStatus,
                ConditionNote = p.ConditionNote,
                BatteryHealth = p.BatteryHealth,
                PurchaseVNo = p.PurchaseVNo,
                PurchaseDate = p.VDate,
                PurchaseRate = p.Rate,
                AddedCost = added,
                IsInStock = !isSold,
                SaleVNo = isSold ? saleInfo?.SaleVNo : null,
                SaleDate = isSold ? saleInfo?.SaleDate : null,
                SaleRate = isSold ? saleInfo?.SaleRate : null
            });
        }

        return list;
    }

    public async Task<ImeiHistoryResponse?> GetImeiHistoryAsync(string imei, CancellationToken cancellationToken)
    {
        var normalizedImei = imei.Trim();

        var purchase = await (
            from d in _purchaseDetailRepository.GetAll().AsNoTracking()
            join m in _purchaseMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            join a in _accountRepository.GetAll().AsNoTracking()
                on m.AccountId equals a.Id into accJoin
            from a in accJoin.DefaultIfEmpty()
            where d.Imei == normalizedImei || d.Imei2 == normalizedImei
            select new
            {
                d.Imei,
                d.Imei2,
                ItemTitle = i != null ? i.Title : string.Empty,
                BrandTitle = i != null && i.Brand != null ? i.Brand.Title : null,
                ModelName = i != null ? i.ModelName : null,
                d.PtaStatus,
                d.ConditionNote,
                Info = new ImeiTransactionInfo
                {
                    VoucherType = d.VType,
                    VoucherNo = d.VNo,
                    Date = m.VDate,
                    AccountId = m.AccountId!,
                    AccountTitle = a != null ? a.Title : m.AccountId,
                    Rate = d.Rate,
                    PtaStatus = d.PtaStatus,
                    ConditionNote = d.ConditionNote
                }
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (purchase == null) return null;

        var sale = await (
            from s in _saleRepository.GetAll().AsNoTracking()
            join m in _saleMasterRepository.GetAll().AsNoTracking()
                on new { s.VType, s.VNo } equals new { m.VType, VNo = m.VNo }
            join a in _accountRepository.GetAll().AsNoTracking()
                on m.AccountId equals a.Id into accJoin
            from a in accJoin.DefaultIfEmpty()
            where s.Imei == normalizedImei && s.VType == "SL"
            select new ImeiTransactionInfo
            {
                VoucherType = s.VType,
                VoucherNo = s.VNo,
                Date = m.VDate,
                AccountId = m.AccountId!,
                AccountTitle = a != null ? a.Title : m.AccountId,
                Rate = s.GrossRate ?? 0,
                PtaStatus = s.PtaStatus,
                ConditionNote = s.ConditionNote,
                WarrantyMonths = s.WarrantyMonths,
                WarrantyExpiryDate = s.WarrantyExpiryDate
            }
        ).FirstOrDefaultAsync(cancellationToken);

        var additions = await (
            from c in _costAdditionRepository.GetAll().AsNoTracking()
            join a in _accountRepository.GetAll().AsNoTracking()
                on c.PaidFromAccount equals a.Id into accJoin
            from a in accJoin.DefaultIfEmpty()
            join i in _itemRepository.GetAll().AsNoTracking()
                on c.ConsumedItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where c.Imei == normalizedImei
            orderby c.Date
            select new ImeiCostAdditionResponse
            {
                Id = c.Id,
                Imei = c.Imei,
                Date = c.Date,
                ExpenseType = c.ExpenseType,
                Description = c.Description,
                Amount = c.Amount,
                PaidFromAccount = c.PaidFromAccount,
                PaidFromTitle = a != null ? a.Title : c.PaidFromAccount,
                ConsumedItemId = c.ConsumedItemId,
                ConsumedItemTitle = i != null ? i.Title : null,
                ConsumedQty = c.ConsumedQty,
                CreatedOn = c.CreatedOn
            }
        ).ToListAsync(cancellationToken);

        var repairs = await _repairJobRepository.GetAll().AsNoTracking()
            .Where(r => r.Imei == normalizedImei)
            .OrderByDescending(r => r.JobDate)
            .Select(r => new RepairJobSummaryResponse
            {
                JobNo = r.Id,
                JobDate = r.JobDate,
                DeviceModel = r.DeviceModel,
                FaultDescription = r.FaultDescription,
                Status = r.Status,
                TotalAmount = (r.Parts.Sum(p => p.Qty * p.Rate)) + (r.Services.Sum(s => s.Amount)),
                SaleVNo = r.SaleVNo
            })
            .ToListAsync(cancellationToken);

        return new ImeiHistoryResponse
        {
            Imei = purchase.Imei ?? normalizedImei,
            Imei2 = purchase.Imei2,
            ItemTitle = purchase.ItemTitle,
            BrandTitle = purchase.BrandTitle,
            ModelName = purchase.ModelName,
            CurrentPtaStatus = purchase.PtaStatus,
            CurrentCondition = purchase.ConditionNote,
            IsInStock = sale == null,
            PurchasedIn = purchase.Info,
            SoldIn = sale,
            CostAdditions = additions,
            RepairJobs = repairs
        };
    }

    public async Task<long> AddImeiCostAsync(ImeiCostAdditionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Imei))
            throw new BadRequestException("IMEI is required.");

        if (request.Amount <= 0)
            throw new BadRequestException("Amount must be greater than zero.");

        var addition = new ImeiCostAddition
        {
            Imei = request.Imei.Trim(),
            Date = request.Date,
            ExpenseType = request.ExpenseType,
            Description = request.Description,
            Amount = request.Amount,
            PaidFromAccount = request.PaidFromAccount,
            ConsumedItemId = request.ConsumedItemId,
            ConsumedQty = request.ConsumedQty ?? 1
        };

        await _costAdditionRepository.AddAsync(addition, true);
        return addition.Id;
    }
}
