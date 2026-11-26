using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Retailer.Domain.Payroll;

namespace Retailer.Infrastructure.Persistence.Configuration;

public class PayrollComponentConfig : IEntityTypeConfiguration<PayrollComponent>
{
    public void Configure(EntityTypeBuilder<PayrollComponent> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DefaultValue).HasPrecision(18, 2);
        builder.Property(x => x.FormulaExpression).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.ExpenseAccount)
            .WithMany()
            .HasForeignKey(x => x.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LiabilityAccount)
            .WithMany()
            .HasForeignKey(x => x.LiabilityAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryStructureConfig : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SalaryType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();
    }
}

public class SalaryStructureLineConfig : IEntityTypeConfiguration<SalaryStructureLine>
{
    public void Configure(EntityTypeBuilder<SalaryStructureLine> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Value).HasPrecision(18, 4);
        builder.Property(x => x.FormulaOverride).HasMaxLength(500);

        builder.HasIndex(x => new { x.SalaryStructureId, x.PayrollComponentId }).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.SalaryStructure)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SalaryStructureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PayrollComponent)
            .WithMany()
            .HasForeignKey(x => x.PayrollComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeSalaryComponentConfig : IEntityTypeConfiguration<EmployeeSalaryComponent>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponent> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Value).HasPrecision(18, 4);
        builder.Property(x => x.FormulaOverride).HasMaxLength(500);

        builder.HasOne(x => x.HrInfo)
            .WithMany()
            .HasForeignKey(x => x.HrInfoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PayrollComponent)
            .WithMany()
            .HasForeignKey(x => x.PayrollComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeLoanConfig : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.LoanNo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
        builder.Property(x => x.InstallmentAmount).HasPrecision(18, 2);
        builder.Property(x => x.RemainingBalance).HasPrecision(18, 2);
        builder.Property(x => x.GlVoucherNo).HasMaxLength(50);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.LoanNo).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.HrInfo)
            .WithMany()
            .HasForeignKey(x => x.HrInfoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DisbursementAccount)
            .WithMany()
            .HasForeignKey(x => x.DisbursementAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReceivableAccount)
            .WithMany()
            .HasForeignKey(x => x.ReceivableAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeLoanScheduleConfig : IEntityTypeConfiguration<EmployeeLoanSchedule>
{
    public void Configure(EntityTypeBuilder<EmployeeLoanSchedule> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ScheduledAmount).HasPrecision(18, 2);
        builder.Property(x => x.DeductedAmount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeLoanId, x.InstallmentNo }).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.EmployeeLoan)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.EmployeeLoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PayrollRunItem)
            .WithMany()
            .HasForeignKey(x => x.PayrollRunItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PayrollRunConfig : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RunNo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SalaryType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.TotalGrossEarnings).HasPrecision(18, 2);
        builder.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        builder.Property(x => x.TotalLoanRecovery).HasPrecision(18, 2);
        builder.Property(x => x.TotalNetSalary).HasPrecision(18, 2);
        builder.Property(x => x.TotalPaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.GlVoucherNo).HasMaxLength(50);
        builder.Property(x => x.ReversalGlVoucherNo).HasMaxLength(50);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => x.RunNo).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();
    }
}

public class PayrollRunItemConfig : IEntityTypeConfiguration<PayrollRunItem>
{
    public void Configure(EntityTypeBuilder<PayrollRunItem> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(150);
        builder.Property(x => x.Department).HasMaxLength(150);
        builder.Property(x => x.Cnic).HasMaxLength(50);
        builder.Property(x => x.PaymentMethod).HasMaxLength(40).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(150);
        builder.Property(x => x.BankAccountTitle).HasMaxLength(200);
        builder.Property(x => x.BankAccountNo).HasMaxLength(80);
        builder.Property(x => x.Iban).HasMaxLength(80);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.Property(x => x.WorkingDays).HasPrecision(18, 2);
        builder.Property(x => x.PresentDays).HasPrecision(18, 2);
        builder.Property(x => x.UnpaidLeaveDays).HasPrecision(18, 2);
        builder.Property(x => x.LeaveChargeRate).HasPrecision(18, 2);
        builder.Property(x => x.OvertimeHours).HasPrecision(18, 2);
        builder.Property(x => x.OvertimeRate).HasPrecision(18, 2);
        builder.Property(x => x.BonusAmount).HasPrecision(18, 2);
        builder.Property(x => x.BasicSalary).HasPrecision(18, 2);
        builder.Property(x => x.GrossEarnings).HasPrecision(18, 2);
        builder.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        builder.Property(x => x.LoanRecoveryAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetSalary).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.PayrollRunId, x.HrInfoId }).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.PayrollRun)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HrInfo)
            .WithMany()
            .HasForeignKey(x => x.HrInfoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollRunItemComponentConfig : IEntityTypeConfiguration<PayrollRunItemComponent>
{
    public void Configure(EntityTypeBuilder<PayrollRunItemComponent> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ComponentCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ComponentName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Rate).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasOne(x => x.PayrollRunItem)
            .WithMany(x => x.Components)
            .HasForeignKey(x => x.PayrollRunItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PayrollComponent)
            .WithMany()
            .HasForeignKey(x => x.PayrollComponentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PayrollDisbursementConfig : IEntityTypeConfiguration<PayrollDisbursement>
{
    public void Configure(EntityTypeBuilder<PayrollDisbursement> builder)
    {
        var mtBuilder = builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.DisbursementNo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(40).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.GlVoucherNo).HasMaxLength(50);
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.DisbursementNo).IsUnique().HasSoftDeleteFilter();
        mtBuilder.AdjustUniqueIndexes();

        builder.HasOne(x => x.PayrollRun)
            .WithMany(x => x.Disbursements)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceAccount)
            .WithMany()
            .HasForeignKey(x => x.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollDisbursementLineConfig : IEntityTypeConfiguration<PayrollDisbursementLine>
{
    public void Configure(EntityTypeBuilder<PayrollDisbursementLine> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BankNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.BankAccountSnapshot).HasMaxLength(80);
        builder.Property(x => x.IbanSnapshot).HasMaxLength(80);

        builder.HasOne(x => x.PayrollDisbursement)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PayrollDisbursementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PayrollRunItem)
            .WithMany()
            .HasForeignKey(x => x.PayrollRunItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.HrInfo)
            .WithMany()
            .HasForeignKey(x => x.HrInfoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
