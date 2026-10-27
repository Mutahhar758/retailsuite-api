using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.JournalVouchers;
using Retailer.Application.Legacy.RecurringChains;
using Retailer.Application.Legacy.SaleSupplies;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Transactions;

namespace Retailer.Infrastructure.Legacy.RecurringChains;

internal class RecurringChainService : IRecurringChainService
{
    private readonly IRepository<RecurringChain> _chainRepo;
    private readonly IRepository<SaleSupplyMaster> _saleSupplyMasterRepo;
    private readonly IRepository<SaleSupplyDetail> _saleSupplyDetailRepo;
    private readonly ISaleSupplyService _saleSupplyService;
    private readonly IRepository<GlEntry> _glRepo;
    private readonly IJournalVoucherService _journalVoucherService;
    private readonly EfTransactionManager _transactionManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RecurringChainService> _logger;

    public RecurringChainService(
        IRepository<RecurringChain> chainRepo,
        IRepository<SaleSupplyMaster> saleSupplyMasterRepo,
        IRepository<SaleSupplyDetail> saleSupplyDetailRepo,
        ISaleSupplyService saleSupplyService,
        IRepository<GlEntry> glRepo,
        IJournalVoucherService journalVoucherService,
        EfTransactionManager transactionManager,
        ApplicationDbContext dbContext,
        ILogger<RecurringChainService> logger)
    {
        _chainRepo = chainRepo;
        _saleSupplyMasterRepo = saleSupplyMasterRepo;
        _saleSupplyDetailRepo = saleSupplyDetailRepo;
        _saleSupplyService = saleSupplyService;
        _glRepo = glRepo;
        _journalVoucherService = journalVoucherService;
        _transactionManager = transactionManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<RecurringChainResponse>> GetListAsync(CancellationToken cancellationToken)
    {
        return await _chainRepo.GetAll()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new RecurringChainResponse
            {
                Id = x.Id,
                Name = x.Name,
                VoucherType = x.VoucherType,
                Frequency = x.Frequency,
                DayOfWeek = x.DayOfWeek,
                DayOfMonth = x.DayOfMonth,
                IsActive = x.IsActive,
                LastRunDate = x.LastRunDate,
                LastVoucherNo = x.LastVoucherNo,
                Notes = x.Notes,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RecurringChainResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await _chainRepo.GetByIdAsync(id, cancellationToken);
        if (chain is null)
        {
            throw new NotFoundException($"Recurring chain with ID '{id}' not found.");
        }

        return new RecurringChainResponse
        {
            Id = chain.Id,
            Name = chain.Name,
            VoucherType = chain.VoucherType,
            Frequency = chain.Frequency,
            DayOfWeek = chain.DayOfWeek,
            DayOfMonth = chain.DayOfMonth,
            IsActive = chain.IsActive,
            LastRunDate = chain.LastRunDate,
            LastVoucherNo = chain.LastVoucherNo,
            Notes = chain.Notes,
            CreatedOn = chain.CreatedOn
        };
    }

    public async Task<int> CreateFromVoucherAsync(RecurringChainCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Schedule name is required.");

        if (string.IsNullOrWhiteSpace(request.SourceVoucherNo))
            throw new BadRequestException("Source voucher number is required.");

        var chain = new RecurringChain
        {
            Name = request.Name.Trim(),
            VoucherType = request.VoucherType,
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? "Daily" : request.Frequency,
            DayOfWeek = request.DayOfWeek,
            DayOfMonth = request.DayOfMonth,
            IsActive = true,
            LastVoucherNo = request.SourceVoucherNo,
            Notes = request.Notes
        };

        await _chainRepo.AddAsync(chain);

        // Link the source voucher directly into this newly created chain
        if (request.VoucherType is "SP" or "SaleSupply")
        {
            var sourceMaster = await _saleSupplyMasterRepo.GetAll()
                .FirstOrDefaultAsync(x => x.VNo == request.SourceVoucherNo && x.VType == "SP", cancellationToken);

            if (sourceMaster is not null)
            {
                sourceMaster.RecurringChainId = chain.Id;
                await _saleSupplyMasterRepo.UpdateAsync(sourceMaster);
                chain.LastRunDate = sourceMaster.VDate;
                await _chainRepo.UpdateAsync(chain);
            }
        }
        else if (request.VoucherType is "JV" or "JournalVoucher")
        {
            var glEntries = await _glRepo.GetAll()
                .Where(x => x.VoucherNo == request.SourceVoucherNo && x.VType == "JV")
                .ToListAsync(cancellationToken);

            if (glEntries.Count > 0)
            {
                foreach (var gl in glEntries)
                {
                    gl.RecurringChainId = chain.Id;
                    await _glRepo.UpdateAsync(gl);
                }

                chain.LastRunDate = glEntries[0].VDate;
                await _chainRepo.UpdateAsync(chain);
            }
        }

        return chain.Id;
    }

    public async Task UpdateAsync(int id, RecurringChainUpdateRequest request, CancellationToken cancellationToken)
    {
        var chain = await _chainRepo.GetByIdAsync(id, cancellationToken);
        if (chain is null)
            throw new NotFoundException($"Recurring chain with ID '{id}' not found.");

        chain.Name = request.Name.Trim();
        chain.Frequency = request.Frequency;
        chain.DayOfWeek = request.DayOfWeek;
        chain.DayOfMonth = request.DayOfMonth;
        chain.IsActive = request.IsActive;
        chain.Notes = request.Notes;

        await _chainRepo.UpdateAsync(chain);
    }

    public async Task ToggleActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var chain = await _chainRepo.GetByIdAsync(id, cancellationToken);
        if (chain is null)
            throw new NotFoundException($"Recurring chain with ID '{id}' not found.");

        chain.IsActive = isActive;
        await _chainRepo.UpdateAsync(chain);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await _chainRepo.GetByIdAsync(id, cancellationToken);
        if (chain is null)
            throw new NotFoundException($"Recurring chain with ID '{id}' not found.");

        await _chainRepo.DeleteAsync(chain);
    }

    public async Task<string?> RunChainForTodayAsync(int id, DateOnly? targetDate, CancellationToken cancellationToken)
    {
        var runDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var chain = await _chainRepo.GetByIdAsync(id, cancellationToken);
        if (chain is null)
            throw new NotFoundException($"Recurring chain with ID '{id}' not found.");

        if (chain.VoucherType is "SP" or "SaleSupply")
        {
            return await CloneLatestSaleSupplyAsync(chain, runDate, cancellationToken);
        }

        if (chain.VoucherType is "JV" or "JournalVoucher")
        {
            return await CloneLatestJournalVoucherAsync(chain, runDate, cancellationToken);
        }

        _logger.LogWarning("Unsupported voucher type '{VoucherType}' for chain ID {ChainId}.", chain.VoucherType, id);
        return null;
    }

    public async Task ProcessTenantDueChainsAsync(DateOnly runDate, CancellationToken cancellationToken)
    {
        var activeChains = await _chainRepo.GetAll()
            .Where(x => x.IsActive && (x.LastRunDate == null || x.LastRunDate < runDate))
            .ToListAsync(cancellationToken);

        foreach (var chain in activeChains)
        {
            if (!IsDueOn(chain, runDate))
                continue;

            try
            {
                _logger.LogInformation("Processing recurring chain '{Name}' (ID: {ChainId}) for date {Date}.", chain.Name, chain.Id, runDate);
                var voucherNo = await RunChainForTodayAsync(chain.Id, runDate, cancellationToken);

                // Commit the transaction started by EfTransactionManager for this chain.
                // In HTTP requests, EfTransactionMiddleware handles this, but background jobs run outside the HTTP pipeline.
                if (_transactionManager.Transaction != null)
                {
                    await _transactionManager.Transaction.CommitAsync(cancellationToken);
                    await _transactionManager.Transaction.DisposeAsync();
                    _transactionManager.Transaction = null;
                }

                _logger.LogInformation("Successfully processed recurring chain '{Name}' (ID: {ChainId}), Voucher '{VoucherNo}'.", chain.Name, chain.Id, voucherNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute recurring chain '{Name}' (ID: {ChainId}) on {Date}.", chain.Name, chain.Id, runDate);

                if (_transactionManager.Transaction != null)
                {
                    await _transactionManager.Transaction.RollbackAsync(cancellationToken);
                    await _transactionManager.Transaction.DisposeAsync();
                    _transactionManager.Transaction = null;
                }

                _dbContext.ChangeTracker.Clear();
            }
        }
    }

    private static bool IsDueOn(RecurringChain chain, DateOnly date)
    {
        if (string.Equals(chain.Frequency, "Daily", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(chain.Frequency, "Weekly", StringComparison.OrdinalIgnoreCase))
        {
            var targetDay = chain.DayOfWeek ?? DayOfWeek.Monday;
            return date.DayOfWeek == targetDay;
        }

        if (string.Equals(chain.Frequency, "Monthly", StringComparison.OrdinalIgnoreCase))
        {
            var targetDay = chain.DayOfMonth ?? 1;
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            var effectiveDay = Math.Min(targetDay, daysInMonth);
            return date.Day == effectiveDay;
        }

        return false;
    }

    private async Task<string?> CloneLatestSaleSupplyAsync(RecurringChain chain, DateOnly runDate, CancellationToken cancellationToken)
    {
        // 1. Check if a voucher for this chain already exists on runDate (Duplicate Guard)
        var existingForToday = await _saleSupplyMasterRepo.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RecurringChainId == chain.Id && x.VDate == runDate && x.VType == "SP", cancellationToken);

        if (existingForToday is not null)
        {
            _logger.LogInformation("Voucher '{VNo}' already exists for chain {ChainId} on {Date}.", existingForToday.VNo, chain.Id, runDate);
            chain.LastRunDate = runDate;
            chain.LastVoucherNo = existingForToday.VNo;
            await _chainRepo.UpdateAsync(chain);
            return existingForToday.VNo;
        }

        // 2. Fetch the latest active voucher belonging to this chain (No date limit!)
        var latestMaster = await _saleSupplyMasterRepo.GetAll()
            .AsNoTracking()
            .Where(x => x.RecurringChainId == chain.Id && x.VType == "SP")
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestMaster is null)
        {
            _logger.LogWarning("No existing voucher found in chain {ChainId} to clone.", chain.Id);
            return null;
        }

        // 3. Fetch its lines directly from database (with latest modifications)
        var details = await _saleSupplyDetailRepo.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == latestMaster.VType && x.VNo == latestMaster.VNo)
            .OrderBy(x => x.Seq)
            .ToListAsync(cancellationToken);

        var request = new SaleSupplyCreateRequest
        {
            Date = runDate,
            ItemId = latestMaster.ItemId ?? string.Empty,
            Description = latestMaster.Descr,
            Narration = latestMaster.NarrationId,
            SupplyOrderMasterId = latestMaster.SupplyOrderMasterId,
            Lines = details.Select(d => new SaleSupplyLineRequest
            {
                Seq = d.Seq,
                CustomerId = d.CustomerAccountId ?? string.Empty,
                Qty = d.Qty,
                Rate = d.GrossRate,
                Discount = d.Discount,
                AddLess = d.AddLess,
                Carriage = d.Carriage,
                SecUnit = d.SecUnitId,
                SecQty = d.SecQty,
                SecRate = d.SecRate,
                QtyInPack = d.QtyInPack
            }).ToList()
        };

        // 4. Create the new voucher
        var createdVNo = await _saleSupplyService.CreateAsync(request, cancellationToken);

        // 5. Link new voucher to the chain
        var createdMaster = await _saleSupplyMasterRepo.GetAll()
            .FirstOrDefaultAsync(x => x.VType == "SP" && x.VNo == createdVNo, cancellationToken);

        if (createdMaster is not null)
        {
            createdMaster.RecurringChainId = chain.Id;
            await _saleSupplyMasterRepo.UpdateAsync(createdMaster);
        }

        // 6. Update chain state
        chain.LastRunDate = runDate;
        chain.LastVoucherNo = createdVNo;
        await _chainRepo.UpdateAsync(chain);

        return createdVNo;
    }

    private async Task<string?> CloneLatestJournalVoucherAsync(RecurringChain chain, DateOnly runDate, CancellationToken cancellationToken)
    {
        // 1. Duplicate guard
        var existingForToday = await _glRepo.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RecurringChainId == chain.Id && x.VDate == runDate && x.VType == "JV", cancellationToken);

        if (existingForToday is not null)
        {
            chain.LastRunDate = runDate;
            chain.LastVoucherNo = existingForToday.VoucherNo;
            await _chainRepo.UpdateAsync(chain);
            return existingForToday.VoucherNo;
        }

        // 2. Fetch latest voucher in chain
        var latestGl = await _glRepo.GetAll()
            .AsNoTracking()
            .Where(x => x.RecurringChainId == chain.Id && x.VType == "JV")
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestGl is null)
        {
            _logger.LogWarning("No existing JV found in chain {ChainId} to clone.", chain.Id);
            return null;
        }

        var lines = await _glRepo.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "JV" && x.VoucherNo == latestGl.VoucherNo)
            .OrderBy(x => x.VSeq)
            .ToListAsync(cancellationToken);

        var request = new JournalVoucherCreateRequest
        {
            Date = runDate,
            Narration = latestGl.NarrationId,
            Lines = lines.Select(l => new JournalVoucherLineRequest
            {
                Seq = l.VSeq,
                DrAccount = l.DrAccountId ?? string.Empty,
                CrAccount = l.CrAccountId ?? string.Empty,
                Amount = l.Amount,
                Remarks = l.Remarks
            }).ToList()
        };

        var createdVNo = await _journalVoucherService.CreateAsync(request, cancellationToken);

        var createdEntries = await _glRepo.GetAll()
            .Where(x => x.VType == "JV" && x.VoucherNo == createdVNo)
            .ToListAsync(cancellationToken);

        foreach (var entry in createdEntries)
        {
            entry.RecurringChainId = chain.Id;
            await _glRepo.UpdateAsync(entry);
        }

        chain.LastRunDate = runDate;
        chain.LastVoucherNo = createdVNo;
        await _chainRepo.UpdateAsync(chain);

        return createdVNo;
    }
}
