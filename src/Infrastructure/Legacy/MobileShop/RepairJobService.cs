using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.MobileShop;
using Retailer.Application.Legacy.Sales;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.MobileShop;

internal class RepairJobService : IRepairJobService
{
    private readonly IRepository<RepairJob> _jobRepository;
    private readonly IRepository<RepairJobPart> _partRepository;
    private readonly IRepository<RepairJobServiceItem> _serviceRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ChartOfAccount> _accountRepository;
    private readonly IRepository<HrInfo> _hrRepository;
    private readonly ISaleService _saleService;
    private readonly ICurrentUser _currentUser;

    public RepairJobService(
        IRepository<RepairJob> jobRepository,
        IRepository<RepairJobPart> partRepository,
        IRepository<RepairJobServiceItem> serviceRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<ChartOfAccount> accountRepository,
        IRepository<HrInfo> hrRepository,
        ISaleService saleService,
        ICurrentUser currentUser)
    {
        _jobRepository = jobRepository;
        _partRepository = partRepository;
        _serviceRepository = serviceRepository;
        _itemRepository = itemRepository;
        _accountRepository = accountRepository;
        _hrRepository = hrRepository;
        _saleService = saleService;
        _currentUser = currentUser;
    }

    public async Task<List<RepairJobResponse>> GetListAsync(RepairJobListFilter filter, CancellationToken cancellationToken)
    {
        var query = _jobRepository.GetAll().AsNoTracking();

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.JobDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.JobDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.CustomerAcc))
            query = query.Where(x => x.CustomerAcc == filter.CustomerAcc);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim().ToLower();
            query = query.Where(x =>
                x.Id.ToLower().Contains(search) ||
                (x.Imei != null && x.Imei.Contains(search)) ||
                (x.CustomerName != null && x.CustomerName.ToLower().Contains(search)) ||
                (x.CustomerPhone != null && x.CustomerPhone.Contains(search)) ||
                x.DeviceModel.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(x => x.JobDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new RepairJobResponse
            {
                JobNo = x.Id,
                JobDate = x.JobDate,
                CustomerAcc = x.CustomerAcc,
                CustomerAccountTitle = x.CustomerAccount != null ? x.CustomerAccount.Title : x.CustomerAcc,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                BrandId = x.BrandId,
                BrandTitle = x.Brand != null ? x.Brand.Title : null,
                DeviceModel = x.DeviceModel,
                Imei = x.Imei,
                PasscodeOrPattern = x.PasscodeOrPattern,
                FaultDescription = x.FaultDescription,
                PhysicalCondition = x.PhysicalCondition,
                EstimatedCost = x.EstimatedCost ?? 0,
                AdvancePaid = x.AdvancePaid ?? 0,
                Status = x.Status,
                AssignedTechnicianId = x.AssignedTechnicianId,
                AssignedTechnicianName = x.AssignedTechnician != null ? x.AssignedTechnician.Name : null,
                ExpectedDelivery = x.ExpectedDelivery,
                DeliveredOn = x.DeliveredOn,
                SaleVNo = x.SaleVNo,
                Remarks = x.Remarks,
                CreatedOn = x.CreatedOn,
                Parts = x.Parts.Select(p => new RepairJobPartResponse
                {
                    Id = p.Id,
                    ItemId = p.ItemId,
                    ItemTitle = p.Item != null ? p.Item.Title : p.ItemId,
                    Qty = p.Qty,
                    Rate = p.Rate
                }).ToList(),
                Services = x.Services.Select(s => new RepairJobServiceResponse
                {
                    Id = s.Id,
                    ServiceItemId = s.ServiceItemId,
                    Description = s.Description,
                    Amount = s.Amount,
                    TechnicianShare = s.TechnicianShare
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RepairJobResponse?> GetByIdAsync(string jobNo, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAll().AsNoTracking()
            .Include(x => x.CustomerAccount)
            .Include(x => x.Brand)
            .Include(x => x.AssignedTechnician)
            .Include(x => x.Parts)
                .ThenInclude(p => p.Item)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == jobNo, cancellationToken);

        if (job == null) return null;

        return new RepairJobResponse
        {
            JobNo = job.Id,
            JobDate = job.JobDate,
            CustomerAcc = job.CustomerAcc,
            CustomerAccountTitle = job.CustomerAccount?.Title ?? job.CustomerAcc,
            CustomerName = job.CustomerName,
            CustomerPhone = job.CustomerPhone,
            BrandId = job.BrandId,
            BrandTitle = job.Brand?.Title,
            DeviceModel = job.DeviceModel,
            Imei = job.Imei,
            PasscodeOrPattern = job.PasscodeOrPattern,
            FaultDescription = job.FaultDescription,
            PhysicalCondition = job.PhysicalCondition,
            EstimatedCost = job.EstimatedCost ?? 0,
            AdvancePaid = job.AdvancePaid ?? 0,
            Status = job.Status,
            AssignedTechnicianId = job.AssignedTechnicianId,
            AssignedTechnicianName = job.AssignedTechnician?.Name,
            ExpectedDelivery = job.ExpectedDelivery,
            DeliveredOn = job.DeliveredOn,
            SaleVNo = job.SaleVNo,
            Remarks = job.Remarks,
            CreatedOn = job.CreatedOn,
            Parts = job.Parts.Select(p => new RepairJobPartResponse
            {
                Id = p.Id,
                ItemId = p.ItemId,
                ItemTitle = p.Item?.Title ?? p.ItemId,
                Qty = p.Qty,
                Rate = p.Rate
            }).ToList(),
            Services = job.Services.Select(s => new RepairJobServiceResponse
            {
                Id = s.Id,
                ServiceItemId = s.ServiceItemId,
                Description = s.Description,
                Amount = s.Amount,
                TechnicianShare = s.TechnicianShare
            }).ToList()
        };
    }

    public async Task<string> CreateAsync(RepairJobCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerAcc))
            throw new BadRequestException("Customer account is required.");

        if (string.IsNullOrWhiteSpace(request.DeviceModel))
            throw new BadRequestException("Device model is required.");

        if (string.IsNullOrWhiteSpace(request.FaultDescription))
            throw new BadRequestException("Fault description is required.");

        // Generate JobNo e.g. "JOB-2609-0001"
        var prefix = $"JOB-{request.JobDate:yyMM}-";
        var maxJobNo = await _jobRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.Id.StartsWith(prefix))
            .MaxAsync(x => (string?)x.Id, cancellationToken);

        var nextNum = 1L;
        if (maxJobNo != null && maxJobNo.Length > prefix.Length)
        {
            if (long.TryParse(maxJobNo.Substring(prefix.Length), out var parsed))
                nextNum = parsed + 1;
        }

        var jobNo = $"{prefix}{nextNum:D4}";

        var job = new RepairJob
        {
            Id = jobNo,
            JobDate = request.JobDate,
            CustomerAcc = request.CustomerAcc,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            BrandId = request.BrandId,
            DeviceModel = request.DeviceModel,
            Imei = request.Imei,
            PasscodeOrPattern = request.PasscodeOrPattern,
            FaultDescription = request.FaultDescription,
            PhysicalCondition = request.PhysicalCondition,
            EstimatedCost = request.EstimatedCost,
            AdvancePaid = request.AdvancePaid,
            Status = "Received",
            AssignedTechnicianId = request.AssignedTechnicianId,
            ExpectedDelivery = request.ExpectedDelivery,
            Remarks = request.Remarks
        };

        foreach (var p in request.Parts)
        {
            job.Parts.Add(new RepairJobPart
            {
                JobNo = jobNo,
                ItemId = p.ItemId,
                Qty = p.Qty,
                Rate = p.Rate
            });
        }

        foreach (var s in request.Services)
        {
            job.Services.Add(new RepairJobServiceItem
            {
                JobNo = jobNo,
                ServiceItemId = s.ServiceItemId,
                Description = s.Description,
                Amount = s.Amount,
                TechnicianShare = s.TechnicianShare
            });
        }

        await _jobRepository.AddAsync(job, true);
        return job.Id;
    }

    public async Task UpdateAsync(string jobNo, RepairJobUpdateRequest request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAll()
            .Include(x => x.Parts)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == jobNo, cancellationToken)
            ?? throw new NotFoundException($"Repair job '{jobNo}' not found.");

        if (!string.IsNullOrWhiteSpace(job.SaleVNo))
            throw new BadRequestException($"Repair job '{jobNo}' has already been billed under Sale voucher '{job.SaleVNo}' and cannot be modified.");

        job.JobDate = request.JobDate;
        job.CustomerAcc = request.CustomerAcc;
        job.CustomerName = request.CustomerName;
        job.CustomerPhone = request.CustomerPhone;
        job.BrandId = request.BrandId;
        job.DeviceModel = request.DeviceModel;
        job.Imei = request.Imei;
        job.PasscodeOrPattern = request.PasscodeOrPattern;
        job.FaultDescription = request.FaultDescription;
        job.PhysicalCondition = request.PhysicalCondition;
        job.EstimatedCost = request.EstimatedCost;
        job.AdvancePaid = request.AdvancePaid;
        job.Status = request.Status;
        job.AssignedTechnicianId = request.AssignedTechnicianId;
        job.ExpectedDelivery = request.ExpectedDelivery;
        job.Remarks = request.Remarks;

        // Replace parts
        job.Parts.Clear();
        foreach (var p in request.Parts)
        {
            job.Parts.Add(new RepairJobPart
            {
                JobNo = jobNo,
                ItemId = p.ItemId,
                Qty = p.Qty,
                Rate = p.Rate
            });
        }

        // Replace services
        job.Services.Clear();
        foreach (var s in request.Services)
        {
            job.Services.Add(new RepairJobServiceItem
            {
                JobNo = jobNo,
                ServiceItemId = s.ServiceItemId,
                Description = s.Description,
                Amount = s.Amount,
                TechnicianShare = s.TechnicianShare
            });
        }

        await _jobRepository.UpdateAsync(job, true);
    }

    public async Task UpdateStatusAsync(string jobNo, RepairJobStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobNo, cancellationToken)
            ?? throw new NotFoundException($"Repair job '{jobNo}' not found.");

        job.Status = request.Status;
        if (request.Status == "Delivered")
            job.DeliveredOn = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Remarks))
            job.Remarks = string.IsNullOrWhiteSpace(job.Remarks) ? request.Remarks : $"{job.Remarks}\n{request.Remarks}";

        await _jobRepository.UpdateAsync(job, true);
    }

    public async Task<string> BillJobAsync(string jobNo, RepairJobBillRequest request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAll()
            .Include(x => x.Parts)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == jobNo, cancellationToken)
            ?? throw new NotFoundException($"Repair job '{jobNo}' not found.");

        if (!string.IsNullOrWhiteSpace(job.SaleVNo))
            throw new BadRequestException($"Repair job '{jobNo}' has already been billed under voucher '{job.SaleVNo}'.");

        var lines = new List<SaleLineRequest>();
        int seq = 1;

        // 1. Add parts lines (deducts stock)
        foreach (var part in job.Parts)
        {
            lines.Add(new SaleLineRequest
            {
                Seq = seq++,
                ItemId = part.ItemId,
                Qty = part.Qty,
                Rate = part.Rate,
                Discount = 0
            });
        }

        // 2. Add services lines
        foreach (var service in job.Services)
        {
            var serviceItemId = service.ServiceItemId;
            if (string.IsNullOrWhiteSpace(serviceItemId))
            {
                // Find or use any generic service item
                var anyService = await _itemRepository.GetAll()
                    .Where(x => x.ItemType == Retailer.Domain.Common.Enums.ItemType.Service)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                serviceItemId = anyService ?? "1";
            }

            lines.Add(new SaleLineRequest
            {
                Seq = seq++,
                ItemId = serviceItemId,
                Qty = 1,
                Rate = service.Amount,
                Discount = 0
            });
        }

        var saleRequest = new SaleCreateRequest
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Account = job.CustomerAcc,
            Description = request.Description ?? $"Repair Job #{jobNo} - {job.DeviceModel} ({job.FaultDescription})",
            Narration = request.NarrationId,
            CashReceipt = request.CashReceipt + (job.AdvancePaid ?? 0),
            Lines = lines
        };

        // Create the sale invoice via existing SaleService (GL and costing handled automatically)
        var saleVoucherNo = await _saleService.CreateAsync(saleRequest, cancellationToken);

        job.SaleVNo = saleVoucherNo;
        job.Status = "Delivered";
        job.DeliveredOn = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job, true);

        return saleVoucherNo;
    }

    public async Task DeleteAsync(string jobNo, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobNo, cancellationToken)
            ?? throw new NotFoundException($"Repair job '{jobNo}' not found.");

        if (!string.IsNullOrWhiteSpace(job.SaleVNo))
            throw new BadRequestException($"Repair job '{jobNo}' is already billed ({job.SaleVNo}) and cannot be deleted.");

        await _jobRepository.DeleteAsync(job, true);
    }
}
