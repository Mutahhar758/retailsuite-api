using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Payroll.Structures;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;

namespace Retailer.Infrastructure.EnterprisePayroll;

internal class SalaryStructureService : ISalaryStructureService
{
    private readonly IRepository<SalaryStructure> _structureRepository;
    private readonly IRepository<SalaryStructureLine> _lineRepository;
    private readonly IRepository<EmployeeSalaryComponent> _employeeComponentRepository;
    private readonly IRepository<PayrollComponent> _componentRepository;
    private readonly IRepository<HrInfo> _hrInfoRepository;

    public SalaryStructureService(
        IRepository<SalaryStructure> structureRepository,
        IRepository<SalaryStructureLine> lineRepository,
        IRepository<EmployeeSalaryComponent> employeeComponentRepository,
        IRepository<PayrollComponent> componentRepository,
        IRepository<HrInfo> hrInfoRepository)
    {
        _structureRepository = structureRepository;
        _lineRepository = lineRepository;
        _employeeComponentRepository = employeeComponentRepository;
        _componentRepository = componentRepository;
        _hrInfoRepository = hrInfoRepository;
    }

    public async Task<List<SalaryStructureDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var structures = await _structureRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.PayrollComponent)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var empCounts = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.SalaryStructureId != null)
            .GroupBy(x => x.SalaryStructureId!.Value)
            .Select(g => new { StructureId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StructureId, x => x.Count, cancellationToken);

        return structures.Select(s => MapToDto(s, empCounts.GetValueOrDefault(s.Id, 0))).ToList();
    }

    public async Task<SalaryStructureDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var structure = await _structureRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.PayrollComponent)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (structure is null)
            return null;

        var empCount = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.SalaryStructureId == id, cancellationToken);

        return MapToDto(structure, empCount);
    }

    public async Task<int> CreateAsync(SalaryStructureUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _structureRepository.GetAll().AnyAsync(x => x.Code == code, cancellationToken))
            throw new BadRequestException($"Salary structure with code '{code}' already exists.");

        if (request.IsDefault)
            await ClearDefaultFlagAsync(request.SalaryType, null, cancellationToken);

        var entity = new SalaryStructure
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            SalaryType = string.IsNullOrWhiteSpace(request.SalaryType) ? "Monthly" : request.SalaryType.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive
        };

        foreach (var line in request.Lines.DistinctBy(l => l.PayrollComponentId))
        {
            entity.Lines.Add(new SalaryStructureLine
            {
                PayrollComponentId = line.PayrollComponentId,
                CalculationTypeOverride = line.CalculationTypeOverride,
                Value = line.Value,
                FormulaOverride = string.IsNullOrWhiteSpace(line.FormulaOverride) ? null : line.FormulaOverride.Trim(),
                DisplayOrder = line.DisplayOrder
            });
        }

        await _structureRepository.AddAsync(entity, true);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, SalaryStructureUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var entity = await _structureRepository.GetAll()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Salary structure #{id} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _structureRepository.GetAll().AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BadRequestException($"Salary structure with code '{code}' already exists.");

        if (request.IsDefault)
            await ClearDefaultFlagAsync(request.SalaryType, id, cancellationToken);

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.SalaryType = string.IsNullOrWhiteSpace(request.SalaryType) ? "Monthly" : request.SalaryType.Trim();
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;

        // Soft-delete removed lines and upsert active ones
        var incomingByComp = request.Lines
            .DistinctBy(l => l.PayrollComponentId)
            .ToDictionary(l => l.PayrollComponentId);

        foreach (var existingLine in entity.Lines.ToList())
        {
            if (!incomingByComp.TryGetValue(existingLine.PayrollComponentId, out var updated))
            {
                await _lineRepository.DeleteAsync(existingLine, false);
            }
            else
            {
                existingLine.CalculationTypeOverride = updated.CalculationTypeOverride;
                existingLine.Value = updated.Value;
                existingLine.FormulaOverride = string.IsNullOrWhiteSpace(updated.FormulaOverride) ? null : updated.FormulaOverride.Trim();
                existingLine.DisplayOrder = updated.DisplayOrder;
                await _lineRepository.UpdateAsync(existingLine, false);
                incomingByComp.Remove(existingLine.PayrollComponentId);
            }
        }

        foreach (var newLine in incomingByComp.Values)
        {
            await _lineRepository.AddAsync(new SalaryStructureLine
            {
                SalaryStructureId = id,
                PayrollComponentId = newLine.PayrollComponentId,
                CalculationTypeOverride = newLine.CalculationTypeOverride,
                Value = newLine.Value,
                FormulaOverride = string.IsNullOrWhiteSpace(newLine.FormulaOverride) ? null : newLine.FormulaOverride.Trim(),
                DisplayOrder = newLine.DisplayOrder
            }, false);
        }

        await _structureRepository.UpdateAsync(entity, false);
        await _structureRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _structureRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Salary structure #{id} not found.");

        var assigned = await _hrInfoRepository.GetAll()
            .AnyAsync(x => x.SalaryStructureId == id, cancellationToken);
        if (assigned)
            throw new BadRequestException("Cannot delete salary structure while employees are assigned to it.");

        await _structureRepository.DeleteAsync(entity, true);
    }

    public async Task<List<EmployeeSalaryComponentDto>> GetEmployeeOverridesAsync(string hrInfoId, CancellationToken cancellationToken)
    {
        return await _employeeComponentRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.PayrollComponent)
            .Where(x => x.HrInfoId == hrInfoId)
            .OrderBy(x => x.PayrollComponent.ComponentType)
            .ThenBy(x => x.PayrollComponent.DisplayOrder)
            .Select(x => new EmployeeSalaryComponentDto
            {
                Id = x.Id,
                HrInfoId = x.HrInfoId,
                PayrollComponentId = x.PayrollComponentId,
                ComponentCode = x.PayrollComponent.Code,
                ComponentName = x.PayrollComponent.Name,
                ComponentType = x.PayrollComponent.ComponentType,
                CalculationTypeOverride = x.CalculationTypeOverride,
                Value = x.Value,
                FormulaOverride = x.FormulaOverride,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveEmployeeOverridesAsync(string hrInfoId, List<EmployeeSalaryComponentUpsertItem> items, CancellationToken cancellationToken)
    {
        var employeeExists = await _hrInfoRepository.GetAll().AnyAsync(x => x.Id == hrInfoId, cancellationToken);
        if (!employeeExists)
            throw new NotFoundException($"Employee '{hrInfoId}' not found.");

        var existing = await _employeeComponentRepository.GetAll()
            .Where(x => x.HrInfoId == hrInfoId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
            await _employeeComponentRepository.DeleteRangeAsync(existing, false);

        if (items.Count > 0)
        {
            var validComponentIds = await _componentRepository.GetAll()
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var validSet = validComponentIds.ToHashSet();

            var toAdd = items
                .Where(x => validSet.Contains(x.PayrollComponentId))
                .DistinctBy(x => x.PayrollComponentId)
                .Select(x => new EmployeeSalaryComponent
                {
                    HrInfoId = hrInfoId,
                    PayrollComponentId = x.PayrollComponentId,
                    CalculationTypeOverride = x.CalculationTypeOverride,
                    Value = x.Value,
                    FormulaOverride = string.IsNullOrWhiteSpace(x.FormulaOverride) ? null : x.FormulaOverride.Trim(),
                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,
                    IsActive = x.IsActive
                })
                .ToList();

            if (toAdd.Count > 0)
                await _employeeComponentRepository.AddRangeAsync(toAdd, false);
        }

        await _employeeComponentRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ClearDefaultFlagAsync(string salaryType, int? excludeId, CancellationToken cancellationToken)
    {
        var normalizedType = string.IsNullOrWhiteSpace(salaryType) ? "Monthly" : salaryType.Trim();
        var defaults = await _structureRepository.GetAll()
            .Where(x => x.IsDefault && x.SalaryType == normalizedType && (!excludeId.HasValue || x.Id != excludeId.Value))
            .ToListAsync(cancellationToken);

        foreach (var d in defaults)
        {
            d.IsDefault = false;
            await _structureRepository.UpdateAsync(d, false);
        }
    }

    private static void ValidateRequest(SalaryStructureUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Structure code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Structure name is required.");
    }

    private static SalaryStructureDto MapToDto(SalaryStructure s, int empCount) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Description = s.Description,
        SalaryType = s.SalaryType,
        IsDefault = s.IsDefault,
        IsActive = s.IsActive,
        AssignedEmployeeCount = empCount,
        Lines = s.Lines
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.PayrollComponent?.DisplayOrder ?? 0)
            .Select(l => new SalaryStructureLineDto
            {
                Id = l.Id,
                PayrollComponentId = l.PayrollComponentId,
                ComponentCode = l.PayrollComponent?.Code ?? string.Empty,
                ComponentName = l.PayrollComponent?.Name ?? string.Empty,
                ComponentType = l.PayrollComponent?.ComponentType ?? PayrollComponentType.Earning,
                EffectiveCalculationType = l.CalculationTypeOverride ?? l.PayrollComponent?.CalculationType ?? PayrollCalculationType.Fixed,
                CalculationTypeOverride = l.CalculationTypeOverride,
                Value = l.Value,
                FormulaOverride = l.FormulaOverride,
                DisplayOrder = l.DisplayOrder
            }).ToList()
    };
}
