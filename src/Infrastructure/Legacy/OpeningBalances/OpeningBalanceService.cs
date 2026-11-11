using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.OpeningBalances;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Legacy.OpeningBalances;

internal class OpeningBalanceService : IOpeningBalanceService
{
    private static readonly DateOnly OpeningDate = new DateOnly(2026, 1, 1);
    private const string VType = "Op";

    private readonly IRepository<ChartOfAccount> _chartRepository;
    private readonly IRepository<GlEntry> _glRepository;

    public OpeningBalanceService(IRepository<ChartOfAccount> chartRepository, IRepository<GlEntry> glRepository)
    {
        _chartRepository = chartRepository;
        _glRepository = glRepository;
    }

    public async Task<List<OpeningBalanceResponse>> GetAsync(string? parentAccountId, CancellationToken cancellationToken)
    {
        // Get all detail (level 5) accounts optionally filtered by parent (level 4)
        var accountsQuery = _chartRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5);

        if (!string.IsNullOrWhiteSpace(parentAccountId) && parentAccountId != "%")
            accountsQuery = accountsQuery.Where(x => x.ParentId == parentAccountId);

        var accounts = await accountsQuery
            .Select(x => new { x.Id, x.Title, x.ParentId })
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(x => x.Id).ToList();

        // Fetch opening GL entries for these accounts (Dr or Cr side)
        var glEntries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType &&
                        (accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!)))
            .Select(x => new { x.DrAccountId, x.CrAccountId, x.Amount })
            .ToListAsync(cancellationToken);

        // Build balance map: positive = Dr, negative = Cr
        var balMap = glEntries.ToDictionary(
            x => x.DrAccountId != null && x.DrAccountId != null ? x.DrAccountId : x.CrAccountId!,
            x => x.DrAccountId != null && x.DrAccountId != null ? x.Amount : -x.Amount);

        return accounts.Select(a => new OpeningBalanceResponse
        {
            ParentCode = a.ParentId ?? string.Empty,
            Code = a.Id,
            Title = a.Title,
            Bal = balMap.TryGetValue(a.Id, out var bal) ? bal : 0m
        }).ToList();
    }

    public async Task UpsertAsync(OpeningBalanceUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Account))
            throw new BadRequestException("Account is required.");

        var dr = request.DrAmount ?? 0m;
        var cr = request.CrAmount ?? 0m;
        var netAmount = dr - cr;

        var existing = await _glRepository.GetAll()
            .FirstOrDefaultAsync(
                x => x.VType == VType &&
                     (x.DrAccountId == request.Account || x.CrAccountId == request.Account),
                cancellationToken);

        if (existing is null)
        {
            if (dr == 0 && cr == 0)
                return;

            // All opening entries share one VoucherNo; get or default to "00001"
            var voucherNo = "00001";

            var maxSeq = await _glRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .AsNoTracking()
                .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
                .MaxAsync(x => (int?)x.VSeq, cancellationToken) ?? 0;

            var entry = new GlEntry
            {
                VDate = OpeningDate,
                VTime = TimeOnly.MinValue,
                VoucherNo = voucherNo,
                VType = VType,
                VSeq = maxSeq + 1,
                DrAccountId = netAmount >= 0 ? request.Account : null,
                Amount = dr != 0 ? dr : cr,
                CrAccountId = netAmount < 0 ? request.Account : null,
                Clear = 0
            };

            await _glRepository.AddAsync(entry);
        }
        else
        {
            if (dr == 0 && cr == 0)
            {
                await _glRepository.DeleteAsync(existing);
                return;
            }

            existing.DrAccountId = netAmount >= 0 ? request.Account : null;
            existing.Amount = dr != 0 ? dr : cr;
            existing.CrAccountId = netAmount < 0 ? request.Account : null;

            await _glRepository.UpdateAsync(existing);
        }
    }

    public async Task BatchUpsertAsync(List<OpeningBalanceUpsertRequest> requests, CancellationToken cancellationToken)
    {
        if (requests == null || requests.Count == 0)
            return;

        var validRequests = requests.Where(r => !string.IsNullOrWhiteSpace(r.Account)).ToList();
        if (validRequests.Count == 0)
            return;

        var accountIds = validRequests.Select(r => r.Account.Trim()).Distinct().ToList();

        var existingEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType &&
                        ((x.DrAccountId != null && accountIds.Contains(x.DrAccountId)) ||
                         (x.CrAccountId != null && accountIds.Contains(x.CrAccountId))))
            .ToListAsync(cancellationToken);

        var existingByAccount = new Dictionary<string, GlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in existingEntries)
        {
            var acc = entry.DrAccountId ?? entry.CrAccountId;
            if (!string.IsNullOrWhiteSpace(acc) && !existingByAccount.ContainsKey(acc))
            {
                existingByAccount[acc] = entry;
            }
        }

        var voucherNo = "00001";
        var maxSeq = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .MaxAsync(x => (int?)x.VSeq, cancellationToken) ?? 0;

        var entriesToAdd = new List<GlEntry>();
        var entriesToUpdate = new List<GlEntry>();
        var entriesToDelete = new List<GlEntry>();

        foreach (var req in validRequests)
        {
            var account = req.Account.Trim();
            var dr = req.DrAmount ?? 0m;
            var cr = req.CrAmount ?? 0m;
            var netAmount = dr - cr;

            if (existingByAccount.TryGetValue(account, out var existing))
            {
                if (dr == 0 && cr == 0)
                {
                    entriesToDelete.Add(existing);
                }
                else
                {
                    existing.DrAccountId = netAmount >= 0 ? account : null;
                    existing.Amount = dr != 0 ? dr : cr;
                    existing.CrAccountId = netAmount < 0 ? account : null;
                    entriesToUpdate.Add(existing);
                }
            }
            else
            {
                if (dr == 0 && cr == 0)
                    continue;

                maxSeq++;
                var newEntry = new GlEntry
                {
                    VDate = OpeningDate,
                    VTime = TimeOnly.MinValue,
                    VoucherNo = voucherNo,
                    VType = VType,
                    VSeq = maxSeq,
                    DrAccountId = netAmount >= 0 ? account : null,
                    Amount = dr != 0 ? dr : cr,
                    CrAccountId = netAmount < 0 ? account : null,
                    Clear = 0
                };
                entriesToAdd.Add(newEntry);
                existingByAccount[account] = newEntry;
            }
        }

        if (entriesToDelete.Count > 0)
            await _glRepository.DeleteRangeAsync(entriesToDelete, doSaveChanges: false);

        if (entriesToAdd.Count > 0)
            await _glRepository.AddRangeAsync(entriesToAdd, doSaveChanges: false);

        if (entriesToUpdate.Count > 0)
            await _glRepository.UpdateRangeAsync(entriesToUpdate, doSaveChanges: false);

        await _glRepository.SaveChangesAsync(cancellationToken);
    }
}
