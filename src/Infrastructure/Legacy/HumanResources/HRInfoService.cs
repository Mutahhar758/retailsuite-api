using Mapster;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.HumanResources;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.HumanResources;

public class HRInfoService : IHRInfoService
{
    private readonly IRepository<HrInfo> _hrInfoRepository;

    public HRInfoService(IRepository<HrInfo> hrInfoRepository)
    {
        _hrInfoRepository = hrInfoRepository;
    }

    public async Task<List<HRInfoResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return employees.Adapt<List<HRInfoResponse>>();
    }

    public async Task<HRInfoResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);
        return employee?.Adapt<HRInfoResponse>();
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
            throw new NotFoundException($"HR Info with ID {id} not found.");

        request.Adapt(employee);
        await _hrInfoRepository.UpdateAsync(employee);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);

        if (employee == null)
            throw new Exception($"HR Info with ID {id} not found.");

        await _hrInfoRepository.DeleteAsync(employee);
    }
}
