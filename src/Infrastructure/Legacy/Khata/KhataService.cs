using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Khata;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Khata;

internal class KhataService : IKhataService
{
    private static readonly string[] PartyDefaultTitles = new[] { "Customers", "Suppliers" };

    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<CustomerDetail> _customerDetailRepository;
    private readonly IRepository<SupplierDetail> _supplierDetailRepository;
    private readonly IRepository<GlEntry> _glRepository;

    public KhataService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IRepository<SupplierDetail> supplierDetailRepository,
        IRepository<GlEntry> glRepository)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _customerDetailRepository = customerDetailRepository;
        _supplierDetailRepository = supplierDetailRepository;
        _glRepository = glRepository;
    }

    public async Task<KhataSummaryResponse> GetPartiesAsync(string? partyType, string? search, CancellationToken cancellationToken)
    {
        var defaultAccounts = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => PartyDefaultTitles.Contains(x.Title!))
            .Select(x => new { x.Title, Prefix = x.MapAccountId ?? x.AccountId })
            .Where(x => !string.IsNullOrWhiteSpace(x.Prefix))
            .ToListAsync(cancellationToken);

        string? customerPrefix = defaultAccounts.FirstOrDefault(x => x.Title == "Customers")?.Prefix;
        string? supplierPrefix = defaultAccounts.FirstOrDefault(x => x.Title == "Suppliers")?.Prefix;

        var distinctPrefixes = defaultAccounts
            .Select(x => x.Prefix!)
            .Distinct()
            .ToList();

        if (distinctPrefixes.Count == 0)
        {
            return new KhataSummaryResponse();
        }

        var allLevel5 = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5)
            .OrderBy(x => x.Title)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        var matchedAccounts = allLevel5
            .Where(x => distinctPrefixes.Any(p => x.Id.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        if (matchedAccounts.Count == 0)
        {
            return new KhataSummaryResponse();
        }

        var accountIds = matchedAccounts.Select(x => x.Id).ToList();

        // Load phone + active status from CustomerDetail & SupplierDetail
        var customerDetails = await _customerDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Phone1, x.Phone2, x.SmsNumber, x.Active })
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var supplierDetails = await _supplierDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Phone1, x.Phone2, x.SmsNumber, x.Active })
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        // Single GL fetch for all matched party accounts to compute balances & last transaction
        var glEntries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId != null && accountIds.Contains(x.DrAccountId))
                || (x.CrAccountId != null && accountIds.Contains(x.CrAccountId)))
            .Select(x => new
            {
                x.DrAccountId,
                x.CrAccountId,
                x.Amount,
                x.VDate,
                x.VType,
                x.VoucherNo,
                x.VSeq,
                x.Remarks
            })
            .ToListAsync(cancellationToken);

        var balanceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var lastTxMap = new Dictionary<string, (DateOnly Date, string VoucherNo, int Seq, decimal Amount, string Type, string? Note)>(StringComparer.OrdinalIgnoreCase);

        var accountIdSet = new HashSet<string>(accountIds, StringComparer.OrdinalIgnoreCase);

        void RecordMovement(string accId, decimal delta, DateOnly vDate, string vType, string voucherNo, int vSeq, decimal amount, string? remarks)
        {
            balanceMap[accId] = balanceMap.TryGetValue(accId, out var cur) ? cur + delta : delta;

            if (string.Equals(vType, "Op", StringComparison.OrdinalIgnoreCase))
                return;

            string txType = vType == "RV" ? "IN" : vType == "PV" ? "OUT" : "OTHER";
            if (!lastTxMap.TryGetValue(accId, out var existing)
                || vDate > existing.Date
                || (vDate == existing.Date && string.CompareOrdinal(voucherNo, existing.VoucherNo) > 0)
                || (vDate == existing.Date && voucherNo == existing.VoucherNo && vSeq > existing.Seq))
            {
                lastTxMap[accId] = (vDate, voucherNo, vSeq, amount, txType, remarks);
            }
        }

        foreach (var e in glEntries)
        {
            if (!string.IsNullOrWhiteSpace(e.DrAccountId) && accountIdSet.Contains(e.DrAccountId))
            {
                RecordMovement(e.DrAccountId, e.Amount, e.VDate, e.VType, e.VoucherNo, e.VSeq, e.Amount, e.Remarks);
            }

            if (!string.IsNullOrWhiteSpace(e.CrAccountId) && accountIdSet.Contains(e.CrAccountId))
            {
                RecordMovement(e.CrAccountId, -e.Amount, e.VDate, e.VType, e.VoucherNo, e.VSeq, e.Amount, e.Remarks);
            }
        }

        var parties = new List<KhataPartyDto>(matchedAccounts.Count);

        foreach (var acc in matchedAccounts)
        {
            bool isVendor = supplierPrefix != null && acc.Id.StartsWith(supplierPrefix, StringComparison.Ordinal);
            string resolvedType = isVendor ? "Vendor" : "Customer";

            // Skip explicitly deactivated parties if they have zero balance
            decimal bal = balanceMap.TryGetValue(acc.Id, out var b) ? b : 0m;
            if (isVendor)
            {
                if (supplierDetails.TryGetValue(acc.Id, out var sd) && sd.Active == false && bal == 0m)
                    continue;
            }
            else
            {
                if (customerDetails.TryGetValue(acc.Id, out var cd) && cd.Active == false && bal == 0m)
                    continue;
            }

            string? phone = null;
            if (isVendor && supplierDetails.TryGetValue(acc.Id, out var sup))
            {
                phone = FirstNonEmpty(sup.Phone1, sup.SmsNumber, sup.Phone2);
            }
            else if (customerDetails.TryGetValue(acc.Id, out var cust))
            {
                phone = FirstNonEmpty(cust.Phone1, cust.SmsNumber, cust.Phone2);
            }

            lastTxMap.TryGetValue(acc.Id, out var lastTx);

            parties.Add(new KhataPartyDto
            {
                AccountId = acc.Id,
                Title = acc.Title,
                PartyType = resolvedType,
                Phone = phone,
                Balance = bal,
                LastTransactionDate = lastTx.Date == default ? null : lastTx.Date,
                LastTransactionAmount = lastTx.Date == default ? null : lastTx.Amount,
                LastTransactionType = lastTx.Date == default ? null : lastTx.Type,
                LastTransactionNote = lastTx.Date == default ? null : lastTx.Note
            });
        }

        // Totals computed across all active parties (before search filter so header reflects overall Khata)
        decimal totalReceivable = parties.Where(x => x.Balance > 0).Sum(x => x.Balance);
        decimal totalPayable = parties.Where(x => x.Balance < 0).Sum(x => Math.Abs(x.Balance));

        IEnumerable<KhataPartyDto> filtered = parties;

        if (!string.IsNullOrWhiteSpace(partyType) && !string.Equals(partyType, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => string.Equals(x.PartyType, partyType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(x =>
                x.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(x.Phone) && x.Phone.Contains(term, StringComparison.OrdinalIgnoreCase))
                || x.AccountId.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var orderedParties = filtered
            .OrderByDescending(x => x.LastTransactionDate.HasValue)
            .ThenByDescending(x => x.LastTransactionDate)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new KhataSummaryResponse
        {
            TotalReceivable = totalReceivable,
            TotalPayable = totalPayable,
            NetBalance = totalReceivable - totalPayable,
            Parties = orderedParties
        };
    }

    public async Task<KhataLedgerResponse> GetPartyLedgerAsync(string accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new BadRequestException("AccountId is required.");

        var account = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => new { x.Id, x.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            throw new NotFoundException($"Account '{accountId}' not found.");

        // Determine party type & phone
        var supplierPrefix = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == "Suppliers")
            .Select(x => x.MapAccountId ?? x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        bool isVendor = !string.IsNullOrWhiteSpace(supplierPrefix)
            && account.Id.StartsWith(supplierPrefix, StringComparison.Ordinal);

        string? phone = null;
        if (isVendor)
        {
            var sup = await _supplierDetailRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Id == account.Id)
                .Select(x => new { x.Phone1, x.Phone2, x.SmsNumber })
                .FirstOrDefaultAsync(cancellationToken);
            if (sup != null)
                phone = FirstNonEmpty(sup.Phone1, sup.SmsNumber, sup.Phone2);
        }
        else
        {
            var cust = await _customerDetailRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Id == account.Id)
                .Select(x => new { x.Phone1, x.Phone2, x.SmsNumber })
                .FirstOrDefaultAsync(cancellationToken);
            if (cust != null)
                phone = FirstNonEmpty(cust.Phone1, cust.SmsNumber, cust.Phone2);
        }

        // Opening balance (all Op entries + entries before fromDate if fromDate is provided)
        decimal openingBalance = 0m;
        if (fromDate.HasValue)
        {
            openingBalance = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => (x.DrAccountId == accountId || x.CrAccountId == accountId)
                    && (x.VType == "Op" || x.VDate < fromDate.Value))
                .Select(x => x.DrAccountId == accountId ? x.Amount : -x.Amount)
                .SumAsync(cancellationToken);
        }
        else
        {
            openingBalance = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => (x.DrAccountId == accountId || x.CrAccountId == accountId)
                    && x.VType == "Op")
                .Select(x => x.DrAccountId == accountId ? x.Amount : -x.Amount)
                .SumAsync(cancellationToken);
        }

        var movementQuery = _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == accountId || x.CrAccountId == accountId) && x.VType != "Op");

        if (fromDate.HasValue)
            movementQuery = movementQuery.Where(x => x.VDate >= fromDate.Value);

        if (toDate.HasValue)
            movementQuery = movementQuery.Where(x => x.VDate <= toDate.Value);

        var rawEntries = await movementQuery
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VType)
            .ThenBy(x => x.VoucherNo)
            .ThenBy(x => x.VSeq)
            .Select(x => new
            {
                x.VDate,
                x.VType,
                x.VoucherNo,
                x.VSeq,
                x.Amount,
                x.DrAccountId,
                x.CrAccountId,
                x.Remarks,
                Narration = x.Narration != null ? x.Narration.Title : x.NarrationId
            })
            .ToListAsync(cancellationToken);

        // Resolve counterpart titles
        var counterpartIds = rawEntries
            .Select(x => x.DrAccountId == accountId ? x.CrAccountId : x.DrAccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var titleMap = counterpartIds.Count == 0
            ? new Dictionary<string, string>()
            : await _chartOfAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => counterpartIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        // Detect multi-line RV / PV vouchers so Khata edit/delete preserves other lines safely
        var rvVoucherNos = rawEntries.Where(x => x.VType == "RV").Select(x => x.VoucherNo).Distinct().ToList();
        var pvVoucherNos = rawEntries.Where(x => x.VType == "PV").Select(x => x.VoucherNo).Distinct().ToList();

        var multiLineVouchers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (rvVoucherNos.Count > 0)
        {
            var rvMulti = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "RV" && rvVoucherNos.Contains(x.VoucherNo))
                .GroupBy(x => x.VoucherNo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);
            foreach (var v in rvMulti)
                multiLineVouchers.Add($"RV-{v}");
        }

        if (pvVoucherNos.Count > 0)
        {
            var pvMulti = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "PV" && pvVoucherNos.Contains(x.VoucherNo))
                .GroupBy(x => x.VoucherNo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);
            foreach (var v in pvMulti)
                multiLineVouchers.Add($"PV-{v}");
        }

        decimal running = openingBalance;
        decimal totalIn = 0m;
        decimal totalOut = 0m;
        var entries = new List<KhataLedgerEntryDto>(rawEntries.Count);

        foreach (var e in rawEntries)
        {
            decimal dr = e.DrAccountId == accountId ? e.Amount : 0m;
            decimal cr = e.CrAccountId == accountId ? e.Amount : 0m;
            running += dr - cr;
            totalOut += dr;
            totalIn += cr;

            var counterpartId = e.DrAccountId == accountId ? e.CrAccountId : e.DrAccountId;
            var counterpartTitle = !string.IsNullOrWhiteSpace(counterpartId) && titleMap.TryGetValue(counterpartId, out var t)
                ? t
                : string.Empty;

            bool isEditable = e.VType == "RV" || e.VType == "PV";

            entries.Add(new KhataLedgerEntryDto
            {
                VDate = e.VDate,
                VType = e.VType,
                VoucherNo = e.VoucherNo,
                VSeq = e.VSeq,
                CounterpartAccountId = counterpartId,
                CounterpartTitle = counterpartTitle,
                Remarks = e.Remarks,
                Narration = e.Narration,
                Dr = dr,
                Cr = cr,
                RunningBalance = running,
                IsEditable = isEditable,
                IsMultiLineVoucher = isEditable && multiLineVouchers.Contains($"{e.VType}-{e.VoucherNo}")
            });
        }

        return new KhataLedgerResponse
        {
            AccountId = account.Id,
            Title = account.Title,
            PartyType = isVendor ? "Vendor" : "Customer",
            Phone = phone,
            OpeningBalance = openingBalance,
            CurrentBalance = running,
            TotalIn = totalIn,
            TotalOut = totalOut,
            Entries = entries
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
