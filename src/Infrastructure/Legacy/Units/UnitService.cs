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
                Title = x.Title,
                ShortTitle = x.ShortTitle,
                ParentUnitId = x.ParentUnitId,
                ParentUnitTitle = x.ParentUnit != null ? x.ParentUnit.Title : null,
                ConversionRatio = x.ConversionRatio
            })
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(UnitCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Application.Common.Exceptions.BadRequestException("Title is required.");

        var parentUnitId = string.IsNullOrWhiteSpace(request.ParentUnitId) ? null : request.ParentUnitId.Trim();
        if (parentUnitId != null && (!request.ConversionRatio.HasValue || request.ConversionRatio.Value <= 1))
            throw new Application.Common.Exceptions.BadRequestException("Conversion ratio must be greater than 1 when a parent unit is selected.");

        var maxCode = await _unitRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var unit = new Unit
        {
            Id = nextCode,
            Title = request.Title.Trim(),
            ShortTitle = string.IsNullOrWhiteSpace(request.ShortTitle) ? null : request.ShortTitle.Trim(),
            ParentUnitId = parentUnitId,
            ConversionRatio = parentUnitId != null ? request.ConversionRatio : null
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

        var parentUnitId = string.IsNullOrWhiteSpace(request.ParentUnitId) ? null : request.ParentUnitId.Trim();
        if (parentUnitId == code.Trim())
            throw new Application.Common.Exceptions.BadRequestException("A unit cannot be its own parent unit.");

        if (parentUnitId != null && (!request.ConversionRatio.HasValue || request.ConversionRatio.Value <= 1))
            throw new Application.Common.Exceptions.BadRequestException("Conversion ratio must be greater than 1 when a parent unit is selected.");

        var existing = await _unitRepository.GetAll()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == code, cancellationToken);

        if (existing == null)
            throw new Application.Common.Exceptions.NotFoundException($"Unit with code '{code}' not found.");

        existing.Title = request.Title.Trim();
        existing.ShortTitle = string.IsNullOrWhiteSpace(request.ShortTitle) ? null : request.ShortTitle.Trim();
        existing.ParentUnitId = parentUnitId;
        existing.ConversionRatio = parentUnitId != null ? request.ConversionRatio : null;

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
