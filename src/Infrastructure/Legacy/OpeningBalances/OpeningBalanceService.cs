using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.OpeningBalances;
using Retailer.Domain.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Legacy.OpeningBalances;

internal class OpeningBalanceService : IOpeningBalanceService
{
    private static readonly DateOnly OpeningDate = new DateOnly(2018, 1, 1);
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
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.VType == VType &&
                     (x.DrAccountId == request.Account || x.CrAccountId == request.Account),
                cancellationToken);

        if (existing is null)
        {
            // All opening entries share one VoucherNo; get or default to "00001"
            var voucherNo = "00001";

            var maxSeq = await _glRepository.GetAll()
                .IgnoreQueryFilters()
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
            existing.DrAccountId = netAmount >= 0 ? request.Account : null;
            existing.Amount = dr != 0 ? dr : cr;
            existing.CrAccountId = netAmount < 0 ? request.Account : null;

            await _glRepository.UpdateAsync(existing);
        }
    }
}
