using System.Globalization;
using Retailer.Domain.Payroll;

namespace Retailer.Application.Payroll.Engine;

public record PayrollComponentSpec(
    int? PayrollComponentId,
    string Code,
    string Name,
    PayrollComponentType ComponentType,
    PayrollCalculationType CalculationType,
    decimal Value,
    string? FormulaExpression,
    bool ShowOnPayslip,
    int DisplayOrder,
    string? GlAccountId,
    bool IsManualOverride = false);

public record PayrollCalculationInput(
    decimal BaseSalary,
    decimal WorkingDays,
    decimal PresentDays,
    decimal UnpaidLeaveDays,
    decimal LeaveChargeRate,
    decimal OvertimeHours,
    decimal OvertimeRate,
    decimal BonusAmount,
    decimal ScheduledLoanRecovery,
    bool ProrateBasicByPresentDays,
    IReadOnlyList<PayrollComponentSpec> Components);

public record PayrollCalculatedComponent(
    int? PayrollComponentId,
    string ComponentCode,
    string ComponentName,
    PayrollComponentType ComponentType,
    PayrollCalculationType CalculationType,
    decimal Quantity,
    decimal Rate,
    decimal Amount,
    bool IsManualOverride,
    bool ShowOnPayslip,
    int DisplayOrder,
    string? GlAccountId);

public record PayrollCalculationResult(
    decimal BasicSalary,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal LoanRecoveryAmount,
    decimal NetSalary,
    List<PayrollCalculatedComponent> Components);

/// <summary>
/// Pure, deterministic payroll calculation engine with safe arithmetic formula evaluation.
/// </summary>
public static class PayrollCalculationEngine
{
    public static PayrollCalculationResult Calculate(PayrollCalculationInput input)
    {
        var workingDays = input.WorkingDays <= 0 ? 30m : input.WorkingDays;
        var presentDays = Math.Clamp(input.PresentDays, 0m, workingDays);

        var basicSalary = input.ProrateBasicByPresentDays
            ? Round2(input.BaseSalary * (presentDays / workingDays))
            : Round2(input.BaseSalary);

        var calculated = new List<PayrollCalculatedComponent>();

        // 1. Always include Basic Salary
        var basicSpec = input.Components.FirstOrDefault(c => c.Code.Equals("BASIC", StringComparison.OrdinalIgnoreCase));
        calculated.Add(new PayrollCalculatedComponent(
            basicSpec?.PayrollComponentId,
            "BASIC",
            basicSpec?.Name ?? "Basic Salary",
            PayrollComponentType.Earning,
            PayrollCalculationType.Fixed,
            1m,
            basicSalary,
            basicSalary,
            false,
            true,
            basicSpec?.DisplayOrder ?? 1,
            basicSpec?.GlAccountId));

        // 2. Recurring & custom earnings (non-formula first, then formula)
        var nonSystemComponents = input.Components
            .Where(c => !IsReservedCode(c.Code))
            .OrderBy(c => c.CalculationType == PayrollCalculationType.Formula ? 1 : 0)
            .ThenBy(c => c.DisplayOrder)
            .ToList();

        decimal runningGross = basicSalary;

        foreach (var comp in nonSystemComponents.Where(c => c.ComponentType == PayrollComponentType.Earning))
        {
            var amount = EvaluateComponent(comp, basicSalary, runningGross, presentDays, input.OvertimeHours);
            if (amount == 0 && !comp.IsManualOverride)
                continue;

            runningGross += amount;
            calculated.Add(new PayrollCalculatedComponent(
                comp.PayrollComponentId,
                comp.Code,
                comp.Name,
                comp.ComponentType,
                comp.CalculationType,
                comp.CalculationType == PayrollCalculationType.PercentageOfBasic ? comp.Value : 1m,
                comp.CalculationType == PayrollCalculationType.PercentageOfBasic ? basicSalary : amount,
                amount,
                comp.IsManualOverride,
                comp.ShowOnPayslip,
                comp.DisplayOrder,
                comp.GlAccountId));
        }

        // 3. Overtime earning
        if (input.OvertimeHours > 0 && input.OvertimeRate > 0)
        {
            var otAmount = Round2(input.OvertimeHours * input.OvertimeRate);
            var otSpec = input.Components.FirstOrDefault(c => c.Code.Equals("OVERTIME", StringComparison.OrdinalIgnoreCase));
            runningGross += otAmount;
            calculated.Add(new PayrollCalculatedComponent(
                otSpec?.PayrollComponentId,
                "OVERTIME",
                otSpec?.Name ?? "Overtime Pay",
                PayrollComponentType.Earning,
                PayrollCalculationType.PerUnit,
                input.OvertimeHours,
                input.OvertimeRate,
                otAmount,
                false,
                true,
                otSpec?.DisplayOrder ?? 10,
                otSpec?.GlAccountId));
        }

        // 4. Bonus earning
        if (input.BonusAmount > 0)
        {
            var bonusAmount = Round2(input.BonusAmount);
            var bonusSpec = input.Components.FirstOrDefault(c => c.Code.Equals("BONUS", StringComparison.OrdinalIgnoreCase));
            runningGross += bonusAmount;
            calculated.Add(new PayrollCalculatedComponent(
                bonusSpec?.PayrollComponentId,
                "BONUS",
                bonusSpec?.Name ?? "Bonus / Incentive",
                PayrollComponentType.Earning,
                PayrollCalculationType.Fixed,
                1m,
                bonusAmount,
                bonusAmount,
                true,
                true,
                bonusSpec?.DisplayOrder ?? 11,
                bonusSpec?.GlAccountId));
        }

        var grossEarnings = Round2(runningGross);
        decimal totalDeductions = 0m;

        // 5. Unpaid leave deduction
        if (input.UnpaidLeaveDays > 0 && input.LeaveChargeRate > 0)
        {
            var leaveDed = Round2(input.UnpaidLeaveDays * input.LeaveChargeRate);
            var leaveSpec = input.Components.FirstOrDefault(c => c.Code.Equals("LEAVE_DED", StringComparison.OrdinalIgnoreCase));
            totalDeductions += leaveDed;
            calculated.Add(new PayrollCalculatedComponent(
                leaveSpec?.PayrollComponentId,
                "LEAVE_DED",
                leaveSpec?.Name ?? "Unpaid Leave Deduction",
                PayrollComponentType.Deduction,
                PayrollCalculationType.PerUnit,
                input.UnpaidLeaveDays,
                input.LeaveChargeRate,
                leaveDed,
                false,
                true,
                leaveSpec?.DisplayOrder ?? 20,
                leaveSpec?.GlAccountId));
        }

        // 6. Custom deductions
        foreach (var comp in nonSystemComponents.Where(c => c.ComponentType == PayrollComponentType.Deduction))
        {
            var amount = EvaluateComponent(comp, basicSalary, grossEarnings, presentDays, input.OvertimeHours);
            if (amount == 0 && !comp.IsManualOverride)
                continue;

            totalDeductions += amount;
            calculated.Add(new PayrollCalculatedComponent(
                comp.PayrollComponentId,
                comp.Code,
                comp.Name,
                comp.ComponentType,
                comp.CalculationType,
                comp.CalculationType == PayrollCalculationType.PercentageOfBasic ? comp.Value : 1m,
                comp.CalculationType == PayrollCalculationType.PercentageOfBasic ? basicSalary : amount,
                amount,
                comp.IsManualOverride,
                comp.ShowOnPayslip,
                comp.DisplayOrder,
                comp.GlAccountId));
        }

        totalDeductions = Round2(totalDeductions);

        // 7. Loan / Advance recovery (capped so net pay does not go below 0)
        var availableForLoan = Math.Max(0m, grossEarnings - totalDeductions);
        var loanRecovery = Round2(Math.Min(Math.Max(0m, input.ScheduledLoanRecovery), availableForLoan));

        if (loanRecovery > 0)
        {
            var loanSpec = input.Components.FirstOrDefault(c => c.Code.Equals("LOAN_REC", StringComparison.OrdinalIgnoreCase));
            calculated.Add(new PayrollCalculatedComponent(
                loanSpec?.PayrollComponentId,
                "LOAN_REC",
                loanSpec?.Name ?? "Loan / Advance Recovery",
                PayrollComponentType.Deduction,
                PayrollCalculationType.Fixed,
                1m,
                loanRecovery,
                loanRecovery,
                false,
                true,
                loanSpec?.DisplayOrder ?? 21,
                loanSpec?.GlAccountId));
        }

        var netSalary = Round2(Math.Max(0m, grossEarnings - totalDeductions - loanRecovery));

        return new PayrollCalculationResult(
            basicSalary,
            grossEarnings,
            totalDeductions,
            loanRecovery,
            netSalary,
            calculated.OrderBy(c => c.ComponentType).ThenBy(c => c.DisplayOrder).ToList());
    }

    public static decimal EvaluateSafeFormula(string? expression, decimal basic, decimal gross, decimal days, decimal otHours)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return 0m;

        var tokens = Tokenize(expression, basic, gross, days, otHours);
        int pos = 0;
        var value = ParseAddSub(tokens, ref pos);
        return Round2(Math.Max(0m, value));
    }

    private static decimal EvaluateComponent(PayrollComponentSpec comp, decimal basic, decimal gross, decimal days, decimal otHours)
    {
        return comp.CalculationType switch
        {
            PayrollCalculationType.Fixed => Round2(comp.Value),
            PayrollCalculationType.PercentageOfBasic => Round2(basic * comp.Value / 100m),
            PayrollCalculationType.PerUnit => Round2(comp.Value),
            PayrollCalculationType.Formula => EvaluateSafeFormula(comp.FormulaExpression, basic, gross, days, otHours),
            _ => 0m
        };
    }

    private static bool IsReservedCode(string code) =>
        code.Equals("BASIC", StringComparison.OrdinalIgnoreCase) ||
        code.Equals("OVERTIME", StringComparison.OrdinalIgnoreCase) ||
        code.Equals("BONUS", StringComparison.OrdinalIgnoreCase) ||
        code.Equals("LEAVE_DED", StringComparison.OrdinalIgnoreCase) ||
        code.Equals("LOAN_REC", StringComparison.OrdinalIgnoreCase);

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static List<string> Tokenize(string expr, decimal basic, decimal gross, decimal days, decimal otHours)
    {
        var normalized = expr.ToUpperInvariant()
            .Replace("OT_HOURS", otHours.ToString(CultureInfo.InvariantCulture))
            .Replace("BASIC", basic.ToString(CultureInfo.InvariantCulture))
            .Replace("GROSS", gross.ToString(CultureInfo.InvariantCulture))
            .Replace("DAYS", days.ToString(CultureInfo.InvariantCulture));

        var tokens = new List<string>();
        int i = 0;
        while (i < normalized.Length)
        {
            char c = normalized[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if ("+-*/()".Contains(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i < normalized.Length && (char.IsDigit(normalized[i]) || normalized[i] == '.'))
                    i++;
                tokens.Add(normalized[start..i]);
                continue;
            }

            throw new FormatException($"Unsupported token '{c}' in payroll formula '{expr}'.");
        }

        return tokens;
    }

    private static decimal ParseAddSub(List<string> tokens, ref int pos)
    {
        var left = ParseMulDiv(tokens, ref pos);
        while (pos < tokens.Count && (tokens[pos] == "+" || tokens[pos] == "-"))
        {
            var op = tokens[pos++];
            var right = ParseMulDiv(tokens, ref pos);
            left = op == "+" ? left + right : left - right;
        }
        return left;
    }

    private static decimal ParseMulDiv(List<string> tokens, ref int pos)
    {
        var left = ParsePrimary(tokens, ref pos);
        while (pos < tokens.Count && (tokens[pos] == "*" || tokens[pos] == "/"))
        {
            var op = tokens[pos++];
            var right = ParsePrimary(tokens, ref pos);
            if (op == "/")
                left = right == 0m ? 0m : left / right;
            else
                left *= right;
        }
        return left;
    }

    private static decimal ParsePrimary(List<string> tokens, ref int pos)
    {
        if (pos >= tokens.Count)
            return 0m;

        if (tokens[pos] == "(")
        {
            pos++;
            var val = ParseAddSub(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos] == ")")
                pos++;
            return val;
        }

        if (tokens[pos] == "-")
        {
            pos++;
            return -ParsePrimary(tokens, ref pos);
        }

        if (decimal.TryParse(tokens[pos], NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
        {
            pos++;
            return num;
        }

        throw new FormatException($"Invalid numeric token '{tokens[pos]}' in payroll formula.");
    }
}
