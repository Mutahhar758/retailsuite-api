using Retailer.Domain.Payroll;

namespace Retailer.Application.Payroll.Components;

public class PayrollComponentDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType CalculationType { get; set; }
    public decimal DefaultValue { get; set; }
    public string? FormulaExpression { get; set; }
    public bool IsSystem { get; set; }
    public bool IsRecurring { get; set; }
    public bool ShowOnPayslip { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string? ExpenseAccountTitle { get; set; }
    public string? LiabilityAccountId { get; set; }
    public string? LiabilityAccountTitle { get; set; }
}

public class PayrollComponentUpsertRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType CalculationType { get; set; } = PayrollCalculationType.Fixed;
    public decimal DefaultValue { get; set; }
    public string? FormulaExpression { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool ShowOnPayslip { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string? LiabilityAccountId { get; set; }
}

public interface IPayrollComponentService : ITransientService
{
    Task<List<PayrollComponentDto>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);
    Task<PayrollComponentDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(PayrollComponentUpsertRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, PayrollComponentUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
