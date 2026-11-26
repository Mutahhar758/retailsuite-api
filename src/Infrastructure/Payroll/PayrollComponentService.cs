using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Payroll.Components;
using Retailer.Application.Payroll.Engine;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.EnterprisePayroll;

internal class PayrollComponentService : IPayrollComponentService
{
    private readonly IRepository<PayrollComponent> _componentRepository;
    private readonly IRepository<ChartOfAccount> _coaRepository;

    public PayrollComponentService(
        IRepository<PayrollComponent> componentRepository,
        IRepository<ChartOfAccount> coaRepository)
    {
        _componentRepository = componentRepository;
        _coaRepository = coaRepository;
    }

    public async Task<List<PayrollComponentDto>> GetAllAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = _componentRepository.GetAll().AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query
            .OrderBy(x => x.ComponentType)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var accountIds = items
            .SelectMany(x => new[] { x.ExpenseAccountId, x.LiabilityAccountId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var accounts = accountIds.Count > 0
            ? await _coaRepository.GetAll().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken)
            : new Dictionary<string, string>();

        return items.Select(x => MapToDto(x, accounts)).ToList();
    }

    public async Task<PayrollComponentDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _componentRepository.GetAll().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var accountIds = new[] { entity.ExpenseAccountId, entity.LiabilityAccountId }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var accounts = accountIds.Count > 0
            ? await _coaRepository.GetAll().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken)
            : new Dictionary<string, string>();

        return MapToDto(entity, accounts);
    }

    public async Task<int> CreateAsync(PayrollComponentUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _componentRepository.GetAll()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
            throw new BadRequestException($"Payroll component with code '{normalizedCode}' already exists.");

        var entity = new PayrollComponent
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            ComponentType = request.ComponentType,
            CalculationType = request.CalculationType,
            DefaultValue = request.DefaultValue,
            FormulaExpression = string.IsNullOrWhiteSpace(request.FormulaExpression) ? null : request.FormulaExpression.Trim(),
            IsSystem = false,
            IsRecurring = request.IsRecurring,
            ShowOnPayslip = request.ShowOnPayslip,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            Description = request.Description?.Trim(),
            ExpenseAccountId = string.IsNullOrWhiteSpace(request.ExpenseAccountId) ? null : request.ExpenseAccountId.Trim(),
            LiabilityAccountId = string.IsNullOrWhiteSpace(request.LiabilityAccountId) ? null : request.LiabilityAccountId.Trim()
        };

        await _componentRepository.AddAsync(entity, true);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, PayrollComponentUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var entity = await _componentRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll component #{id} not found.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (entity.IsSystem && !entity.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("System payroll component code cannot be changed.");

        var duplicate = await _componentRepository.GetAll()
            .AnyAsync(x => x.Id != id && x.Code == normalizedCode, cancellationToken);
        if (duplicate)
            throw new BadRequestException($"Payroll component with code '{normalizedCode}' already exists.");

        entity.Code = normalizedCode;
        entity.Name = request.Name.Trim();
        if (!entity.IsSystem)
        {
            entity.ComponentType = request.ComponentType;
            entity.CalculationType = request.CalculationType;
        }
        entity.DefaultValue = request.DefaultValue;
        entity.FormulaExpression = string.IsNullOrWhiteSpace(request.FormulaExpression) ? null : request.FormulaExpression.Trim();
        entity.IsRecurring = request.IsRecurring;
        entity.ShowOnPayslip = request.ShowOnPayslip;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description?.Trim();
        entity.ExpenseAccountId = string.IsNullOrWhiteSpace(request.ExpenseAccountId) ? null : request.ExpenseAccountId.Trim();
        entity.LiabilityAccountId = string.IsNullOrWhiteSpace(request.LiabilityAccountId) ? null : request.LiabilityAccountId.Trim();

        await _componentRepository.UpdateAsync(entity, true);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _componentRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll component #{id} not found.");

        if (entity.IsSystem)
            throw new BadRequestException($"System payroll component '{entity.Code}' cannot be deleted.");

        await _componentRepository.DeleteAsync(entity, true);
    }

    private static void ValidateRequest(PayrollComponentUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Component code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Component name is required.");

        if (request.CalculationType == PayrollCalculationType.Formula)
        {
            if (string.IsNullOrWhiteSpace(request.FormulaExpression))
                throw new BadRequestException("Formula expression is required when Calculation Type is Formula.");

            try
            {
                _ = PayrollCalculationEngine.EvaluateSafeFormula(request.FormulaExpression, 50000m, 60000m, 30m, 5m);
            }
            catch (FormatException ex)
            {
                throw new BadRequestException($"Invalid formula expression: {ex.Message}");
            }
        }
    }

    private static PayrollComponentDto MapToDto(PayrollComponent x, Dictionary<string, string> accounts) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        ComponentType = x.ComponentType,
        CalculationType = x.CalculationType,
        DefaultValue = x.DefaultValue,
        FormulaExpression = x.FormulaExpression,
        IsSystem = x.IsSystem,
        IsRecurring = x.IsRecurring,
        ShowOnPayslip = x.ShowOnPayslip,
        DisplayOrder = x.DisplayOrder,
        IsActive = x.IsActive,
        Description = x.Description,
        ExpenseAccountId = x.ExpenseAccountId,
        ExpenseAccountTitle = x.ExpenseAccountId != null && accounts.TryGetValue(x.ExpenseAccountId, out var exp) ? exp : null,
        LiabilityAccountId = x.LiabilityAccountId,
        LiabilityAccountTitle = x.LiabilityAccountId != null && accounts.TryGetValue(x.LiabilityAccountId, out var liab) ? liab : null
    };
}
