using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Persistence;
using Retailer.Domain.Payroll;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class PayrollComponentSeeder : ICustomSeeder
{
    private readonly IRepository<PayrollComponent> _componentRepository;
    private readonly ILogger<PayrollComponentSeeder> _logger;

    public PayrollComponentSeeder(
        IRepository<PayrollComponent> componentRepository,
        ILogger<PayrollComponentSeeder> logger)
    {
        _componentRepository = componentRepository;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _componentRepository.GetAll()
            .IgnoreQueryFilters()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var defaults = new List<PayrollComponent>
        {
            new()
            {
                Code = "BASIC",
                Name = "Basic Salary",
                ComponentType = PayrollComponentType.Earning,
                CalculationType = PayrollCalculationType.Fixed,
                DefaultValue = 0,
                IsSystem = true,
                IsRecurring = true,
                ShowOnPayslip = true,
                DisplayOrder = 1,
                IsActive = true,
                Description = "Base monthly/periodic salary from employee profile"
            },
            new()
            {
                Code = "HRA",
                Name = "House Rent Allowance",
                ComponentType = PayrollComponentType.Earning,
                CalculationType = PayrollCalculationType.PercentageOfBasic,
                DefaultValue = 0,
                IsSystem = false,
                IsRecurring = true,
                ShowOnPayslip = true,
                DisplayOrder = 2,
                IsActive = true,
                Description = "Housing allowance calculated as percentage of basic or fixed amount"
            },
            new()
            {
                Code = "MEDICAL",
                Name = "Medical Allowance",
                ComponentType = PayrollComponentType.Earning,
                CalculationType = PayrollCalculationType.PercentageOfBasic,
                DefaultValue = 0,
                IsSystem = false,
                IsRecurring = true,
                ShowOnPayslip = true,
                DisplayOrder = 3,
                IsActive = true,
                Description = "Medical allowance"
            },
            new()
            {
                Code = "OVERTIME",
                Name = "Overtime Pay",
                ComponentType = PayrollComponentType.Earning,
                CalculationType = PayrollCalculationType.PerUnit,
                DefaultValue = 0,
                IsSystem = true,
                IsRecurring = false,
                ShowOnPayslip = true,
                DisplayOrder = 10,
                IsActive = true,
                Description = "Overtime hours multiplied by employee overtime rate"
            },
            new()
            {
                Code = "BONUS",
                Name = "Bonus / Incentive",
                ComponentType = PayrollComponentType.Earning,
                CalculationType = PayrollCalculationType.Fixed,
                DefaultValue = 0,
                IsSystem = true,
                IsRecurring = false,
                ShowOnPayslip = true,
                DisplayOrder = 11,
                IsActive = true,
                Description = "Performance bonus or ad-hoc incentive"
            },
            new()
            {
                Code = "LEAVE_DED",
                Name = "Unpaid Leave Deduction",
                ComponentType = PayrollComponentType.Deduction,
                CalculationType = PayrollCalculationType.PerUnit,
                DefaultValue = 0,
                IsSystem = true,
                IsRecurring = false,
                ShowOnPayslip = true,
                DisplayOrder = 20,
                IsActive = true,
                Description = "Unpaid leave days multiplied by leave charge rate"
            },
            new()
            {
                Code = "LOAN_REC",
                Name = "Loan / Advance Recovery",
                ComponentType = PayrollComponentType.Deduction,
                CalculationType = PayrollCalculationType.Fixed,
                DefaultValue = 0,
                IsSystem = true,
                IsRecurring = false,
                ShowOnPayslip = true,
                DisplayOrder = 21,
                IsActive = true,
                Description = "Automatic recovery of scheduled employee loan or salary advance installments"
            }
        };

        var toAdd = defaults.Where(d => !existingSet.Contains(d.Code)).ToList();
        if (toAdd.Count > 0)
        {
            _logger.LogInformation("Seeding {Count} default payroll components.", toAdd.Count);
            await _componentRepository.AddRangeAsync(toAdd, doSaveChanges: true);
        }
    }
}
