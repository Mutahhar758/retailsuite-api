using Mapster;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Models;
using Retailer.Application.Legacy.HumanResources;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.HumanResources;

public class HRInfoService : IHRInfoService
{
    private readonly IRepository<HrInfo> _hrInfoRepository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public HRInfoService(IRepository<HrInfo> hrInfoRepository, IMediaServiceClient mediaServiceClient)
    {
        _hrInfoRepository = hrInfoRepository;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<List<HRInfoResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var responses = employees.Adapt<List<HRInfoResponse>>();
        await PopulateMediaUrlsAsync(responses, cancellationToken);
        return responses;
    }

    public async Task<HRInfoResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);
        if (employee == null) return null;

        var response = employee.Adapt<HRInfoResponse>();
        if (!string.IsNullOrEmpty(response.MediaId))
        {
            await PopulateMediaUrlsAsync(new List<HRInfoResponse> { response }, cancellationToken);
        }
        return response;
    }

    public async Task CreateAsync(HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        var maxCode = await _hrInfoRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var newEmployee = request.Adapt<HrInfo>();
        newEmployee.Id = nextCode;

        await _hrInfoRepository.AddAsync(newEmployee);
    }

    public async Task UpdateAsync(string id, HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new BadRequestException("ID is required.");

        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);

        if (employee == null)
            throw new NotFoundException($"Employee with ID {id} not found.");

        request.Adapt(employee);
        await _hrInfoRepository.UpdateAsync(employee);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);

        if (employee == null)
            throw new Exception($"Employee with ID {id} not found.");

        await _hrInfoRepository.DeleteAsync(employee);
    }

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        var response = await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "employee", cancellationToken);
        return response ?? throw new InternalServerException("Failed to generate upload URL for employee image.");
    }

    private async Task PopulateMediaUrlsAsync(List<HRInfoResponse> items, CancellationToken cancellationToken)
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

    public async Task<BulkOperationResult> BulkUpsertAsync(HRInfoBulkUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult();
        if (request.Employees == null || request.Employees.Count == 0)
        {
            return result;
        }

        result.TotalProcessed = request.Employees.Count;

        var allIds = await _hrInfoRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        long maxCode = 0;
        foreach (var idStr in allIds)
        {
            if (long.TryParse(idStr, out var val) && val > maxCode)
            {
                maxCode = val;
            }
        }

        var updateCandidates = request.Employees
            .Select((e, idx) => new { Item = e, RowIndex = idx + 1 })
            .Where(x => !string.IsNullOrWhiteSpace(x.Item.Id) && x.Item.Id != "0")
            .ToList();

        var updateIds = updateCandidates.Select(x => x.Item.Id!.Trim()).Distinct().ToList();

        var existingEmployees = await _hrInfoRepository.GetAll()
            .Where(x => updateIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var newEmployeesToAdd = new List<HrInfo>();
        var employeesToUpdate = new List<HrInfo>();

        var defaultDate = DateOnly.FromDateTime(DateTime.Today);

        for (int i = 0; i < request.Employees.Count; i++)
        {
            var item = request.Employees[i];
            var rowIndex = i + 1;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkItemError
                {
                    RowIndex = rowIndex,
                    Identifier = item.Id ?? $"Row #{rowIndex}",
                    ErrorMessage = "Employee name is required."
                });
                continue;
            }

            var normalizedId = (item.Id ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedId) && normalizedId != "0" && existingEmployees.TryGetValue(normalizedId, out var existing))
            {
                existing.Name = item.Name;
                existing.FatherName = item.FatherName;
                if (!string.IsNullOrWhiteSpace(item.Gender)) existing.Gender = item.Gender;
                if (item.Dob.HasValue) existing.Dob = item.Dob.Value;
                existing.MaritialStatus = item.MaritialStatus;
                existing.Cnic = item.Cnic;
                if (item.AppointmentDate.HasValue) existing.AppointmentDate = item.AppointmentDate.Value;
                if (item.JoiningDate.HasValue) existing.JoiningDate = item.JoiningDate.Value;
                existing.Designation = item.Designation;
                if (!string.IsNullOrWhiteSpace(item.SalaryType)) existing.SalaryType = item.SalaryType;
                existing.Salary = item.Salary;
                existing.LeaveCharges = item.LeaveCharges;
                existing.Overtime = item.Overtime;
                existing.ExpenseAccount = item.ExpenseAccount;
                existing.PayableAccount = item.PayableAccount;

                employeesToUpdate.Add(existing);
                result.UpdatedCount++;
            }
            else
            {
                maxCode++;
                var nextCode = maxCode.ToString("D3");

                var newEmp = new HrInfo
                {
                    Id = nextCode,
                    Name = item.Name,
                    FatherName = item.FatherName,
                    Gender = string.IsNullOrWhiteSpace(item.Gender) ? "Male" : item.Gender,
                    Dob = item.Dob ?? new DateOnly(1990, 1, 1),
                    MaritialStatus = item.MaritialStatus,
                    Cnic = item.Cnic,
                    AppointmentDate = item.AppointmentDate ?? defaultDate,
                    JoiningDate = item.JoiningDate ?? defaultDate,
                    Designation = item.Designation,
                    SalaryType = string.IsNullOrWhiteSpace(item.SalaryType) ? "Monthly" : item.SalaryType,
                    Salary = item.Salary,
                    LeaveCharges = item.LeaveCharges,
                    Overtime = item.Overtime,
                    ExpenseAccount = item.ExpenseAccount,
                    PayableAccount = item.PayableAccount
                };

                newEmployeesToAdd.Add(newEmp);
                result.CreatedCount++;
            }
        }

        if (newEmployeesToAdd.Count > 0)
            await _hrInfoRepository.AddRangeAsync(newEmployeesToAdd, doSaveChanges: false);

        if (employeesToUpdate.Count > 0)
            await _hrInfoRepository.UpdateRangeAsync(employeesToUpdate, doSaveChanges: false);

        await _hrInfoRepository.SaveChangesAsync(cancellationToken);

        return result;
    }
}
