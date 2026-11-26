using Retailer.Domain.Payroll;

namespace Retailer.Application.Payroll.Structures;

public class SalaryStructureLineDto
{
    public int Id { get; set; }
    public int PayrollComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType EffectiveCalculationType { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public int DisplayOrder { get; set; }
}

public class SalaryStructureDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int AssignedEmployeeCount { get; set; }
    public List<SalaryStructureLineDto> Lines { get; set; } = [];
}

public class SalaryStructureLineRequest
{
    public int PayrollComponentId { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public int DisplayOrder { get; set; }
}

public class SalaryStructureUpsertRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalaryStructureLineRequest> Lines { get; set; } = [];
}

public class EmployeeSalaryComponentDto
{
    public int Id { get; set; }
    public string HrInfoId { get; set; } = string.Empty;
    public int PayrollComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeSalaryComponentUpsertItem
{
    public int PayrollComponentId { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public interface ISalaryStructureService : ITransientService
{
    Task<List<SalaryStructureDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<SalaryStructureDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(SalaryStructureUpsertRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, SalaryStructureUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<List<EmployeeSalaryComponentDto>> GetEmployeeOverridesAsync(string hrInfoId, CancellationToken cancellationToken);
    Task SaveEmployeeOverridesAsync(string hrInfoId, List<EmployeeSalaryComponentUpsertItem> items, CancellationToken cancellationToken);
}
