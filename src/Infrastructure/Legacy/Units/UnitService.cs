using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Units;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Units;

internal class UnitService : IUnitService
{
    private readonly IRepository<Unit> _unitRepository;

    public UnitService(IRepository<Unit> unitRepository)
    {
        _unitRepository = unitRepository;
    }

    public async Task<List<UnitLookupResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _unitRepository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new UnitLookupResponse
            {
                Code = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(UnitCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Application.Common.Exceptions.BadRequestException("Title is required.");

        var maxCode = await _unitRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var unit = new Unit
        {
            Id = nextCode,
            Title = request.Title
        };

        if (!request.Active)
        {
            unit.DeletedBy = "system";
            unit.DeletedOn = DateTime.UtcNow;
        }

        await _unitRepository.AddAsync(unit);
    }

    public async Task UpdateAsync(string code, UnitUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new Application.Common.Exceptions.BadRequestException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Application.Common.Exceptions.BadRequestException("Title is required.");

        var existing = await _unitRepository.GetAll()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == code, cancellationToken);

        if (existing == null)
            throw new Application.Common.Exceptions.NotFoundException($"Unit with code '{code}' not found.");

        existing.Title = request.Title;

        if (request.Active)
        {
            existing.DeletedBy = null;
            existing.DeletedOn = null;
        }
        else
        {
            existing.DeletedBy = existing.DeletedBy ?? "system";
            existing.DeletedOn = existing.DeletedOn ?? DateTime.UtcNow;
        }

        await _unitRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new Application.Common.Exceptions.BadRequestException("Code is required.");

        var unit = await _unitRepository.GetByIdAsync(code, cancellationToken);

        if (unit is null)
            return;

        await _unitRepository.DeleteAsync(unit);
    }
}
