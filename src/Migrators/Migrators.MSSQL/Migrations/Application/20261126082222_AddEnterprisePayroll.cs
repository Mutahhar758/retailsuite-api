using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEnterprisePayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account_no",
                table: "HRInfo",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_title",
                table: "HRInfo",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "HRInfo",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "HRInfo",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_status",
                table: "HRInfo",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "iban",
                table: "HRInfo",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "loan_receivable_account_id",
                table: "HRInfo",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "HRInfo",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddColumn<int>(
                name: "salary_structure_id",
                table: "HRInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "termination_date",
                table: "HRInfo",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    loan_no = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    hr_info_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    loan_type = table.Column<int>(type: "int", nullable: false),
                    principal_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    installment_count = table.Column<int>(type: "int", nullable: false),
                    installment_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    start_year = table.Column<int>(type: "int", nullable: false),
                    start_month = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    disbursement_date = table.Column<DateOnly>(type: "date", nullable: true),
                    disbursement_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    receivable_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    gl_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    approved_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_loans_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_loans_chart_of_account_disbursement_account_id_tenant_id",
                        columns: x => new { x.disbursement_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_loans_chart_of_account_receivable_account_id_tenant_id",
                        columns: x => new { x.receivable_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_loans_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    component_type = table.Column<int>(type: "int", nullable: false),
                    calculation_type = table.Column<int>(type: "int", nullable: false),
                    default_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    formula_expression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "bit", nullable: false),
                    is_recurring = table.Column<bool>(type: "bit", nullable: false),
                    show_on_payslip = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    expense_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    liability_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_components_chart_of_account_expense_account_id_tenant_id",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_components_chart_of_account_liability_account_id_tenant_id",
                        columns: x => new { x.liability_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    run_no = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    pay_year = table.Column<int>(type: "int", nullable: false),
                    pay_month = table.Column<int>(type: "int", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    pay_date = table.Column<DateOnly>(type: "date", nullable: false),
                    salary_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    employee_count = table.Column<int>(type: "int", nullable: false),
                    total_gross_earnings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_deductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_loan_recovery = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_net_salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_paid_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    gl_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reversal_gl_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    submitted_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    submitted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    approved_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    posted_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    posted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_runs_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructures",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    salary_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_salary_structures_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryComponents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    hr_info_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    payroll_component_id = table.Column<int>(type: "int", nullable: false),
                    calculation_type_override = table.Column<int>(type: "int", nullable: true),
                    value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    formula_override = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_salary_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_salary_components_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_salary_components_payroll_components_payroll_component_id_tenant_id",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDisbursements",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    disbursement_no = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    payroll_run_id = table.Column<int>(type: "int", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source_account_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    gl_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_disbursements_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_disbursements_chart_of_account_source_account_id_tenant_id",
                        columns: x => new { x.source_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_disbursements_payroll_runs_payroll_run_id_tenant_id",
                        columns: x => new { x.payroll_run_id, x.tenant_id },
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    payroll_run_id = table.Column<int>(type: "int", nullable: false),
                    hr_info_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    employee_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    designation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    cnic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    bank_account_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    bank_account_no = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    iban = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    expense_account_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payable_account_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    working_days = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    present_days = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    unpaid_leave_days = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    leave_charge_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    overtime_hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    overtime_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bonus_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    basic_salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_earnings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_deductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    loan_recovery_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_on_hold = table.Column<bool>(type: "bit", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_run_items_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_run_items_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_run_items_payroll_runs_payroll_run_id_tenant_id",
                        columns: x => new { x.payroll_run_id, x.tenant_id },
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructureLines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    salary_structure_id = table.Column<int>(type: "int", nullable: false),
                    payroll_component_id = table.Column<int>(type: "int", nullable: false),
                    calculation_type_override = table.Column<int>(type: "int", nullable: true),
                    value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    formula_override = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_salary_structure_lines_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_salary_structure_lines_payroll_components_payroll_component_id_tenant_id",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_salary_structure_lines_salary_structures_salary_structure_id_tenant_id",
                        columns: x => new { x.salary_structure_id, x.tenant_id },
                        principalTable: "SalaryStructures",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoanSchedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    employee_loan_id = table.Column<int>(type: "int", nullable: false),
                    installment_no = table.Column<int>(type: "int", nullable: false),
                    due_year = table.Column<int>(type: "int", nullable: false),
                    due_month = table.Column<int>(type: "int", nullable: false),
                    scheduled_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    deducted_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "int", nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_loan_schedules_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_loan_schedules_employee_loans_employee_loan_id_tenant_id",
                        columns: x => new { x.employee_loan_id, x.tenant_id },
                        principalTable: "EmployeeLoans",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_loan_schedules_payroll_run_items_payroll_run_item_id_tenant_id",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDisbursementLines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    payroll_disbursement_id = table.Column<int>(type: "int", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "int", nullable: false),
                    hr_info_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    employee_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bank_name_snapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    bank_account_snapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    iban_snapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_disbursement_lines_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_payroll_disbursements_payroll_disbursement_id_tenant_id",
                        columns: x => new { x.payroll_disbursement_id, x.tenant_id },
                        principalTable: "PayrollDisbursements",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_payroll_run_items_payroll_run_item_id_tenant_id",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunItemComponents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "int", nullable: false),
                    payroll_component_id = table.Column<int>(type: "int", nullable: true),
                    component_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    component_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    component_type = table.Column<int>(type: "int", nullable: false),
                    calculation_type = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_manual_override = table.Column<bool>(type: "bit", nullable: false),
                    show_on_payslip = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    gl_account_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_run_item_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_run_item_components_payroll_components_payroll_component_id_tenant_id",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_run_item_components_payroll_run_items_payroll_run_item_id_tenant_id",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_loan_receivable_account_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "loan_receivable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_salary_structure_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "salary_structure_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_disbursement_account_id_tenant_id",
                table: "EmployeeLoans",
                columns: new[] { "disbursement_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_hr_info_id_tenant_id",
                table: "EmployeeLoans",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_loan_no",
                table: "EmployeeLoans",
                columns: new[] { "loan_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_receivable_account_id_tenant_id",
                table: "EmployeeLoans",
                columns: new[] { "receivable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_employee_loan_id_installment_no",
                table: "EmployeeLoanSchedules",
                columns: new[] { "employee_loan_id", "installment_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_employee_loan_id_tenant_id",
                table: "EmployeeLoanSchedules",
                columns: new[] { "employee_loan_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_payroll_run_item_id_tenant_id",
                table: "EmployeeLoanSchedules",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_hr_info_id_tenant_id",
                table: "EmployeeSalaryComponents",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_payroll_component_id_tenant_id",
                table: "EmployeeSalaryComponents",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_code",
                table: "PayrollComponents",
                columns: new[] { "code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_expense_account_id_tenant_id",
                table: "PayrollComponents",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_liability_account_id_tenant_id",
                table: "PayrollComponents",
                columns: new[] { "liability_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_hr_info_id_tenant_id",
                table: "PayrollDisbursementLines",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_payroll_disbursement_id_tenant_id",
                table: "PayrollDisbursementLines",
                columns: new[] { "payroll_disbursement_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_payroll_run_item_id_tenant_id",
                table: "PayrollDisbursementLines",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_disbursement_no",
                table: "PayrollDisbursements",
                columns: new[] { "disbursement_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_payroll_run_id_tenant_id",
                table: "PayrollDisbursements",
                columns: new[] { "payroll_run_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_source_account_id_tenant_id",
                table: "PayrollDisbursements",
                columns: new[] { "source_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_item_components_payroll_component_id_tenant_id",
                table: "PayrollRunItemComponents",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_item_components_payroll_run_item_id_tenant_id",
                table: "PayrollRunItemComponents",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_hr_info_id_tenant_id",
                table: "PayrollRunItems",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_payroll_run_id_hr_info_id",
                table: "PayrollRunItems",
                columns: new[] { "payroll_run_id", "hr_info_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_payroll_run_id_tenant_id",
                table: "PayrollRunItems",
                columns: new[] { "payroll_run_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_run_no",
                table: "PayrollRuns",
                columns: new[] { "run_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_payroll_component_id_tenant_id",
                table: "SalaryStructureLines",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_salary_structure_id_payroll_component_id",
                table: "SalaryStructureLines",
                columns: new[] { "salary_structure_id", "payroll_component_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_salary_structure_id_tenant_id",
                table: "SalaryStructureLines",
                columns: new[] { "salary_structure_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structures_code",
                table: "SalaryStructures",
                columns: new[] { "code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_hr_info_chart_of_account_loan_receivable_account_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "loan_receivable_account_id", "tenant_id" },
                principalTable: "ChartOfAccount",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hr_info_salary_structures_salary_structure_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "salary_structure_id", "tenant_id" },
                principalTable: "SalaryStructures",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hr_info_chart_of_account_loan_receivable_account_id_tenant_id",
                table: "HRInfo");

            migrationBuilder.DropForeignKey(
                name: "fk_hr_info_salary_structures_salary_structure_id_tenant_id",
                table: "HRInfo");

            migrationBuilder.DropTable(
                name: "EmployeeLoanSchedules");

            migrationBuilder.DropTable(
                name: "EmployeeSalaryComponents");

            migrationBuilder.DropTable(
                name: "PayrollDisbursementLines");

            migrationBuilder.DropTable(
                name: "PayrollRunItemComponents");

            migrationBuilder.DropTable(
                name: "SalaryStructureLines");

            migrationBuilder.DropTable(
                name: "EmployeeLoans");

            migrationBuilder.DropTable(
                name: "PayrollDisbursements");

            migrationBuilder.DropTable(
                name: "PayrollRunItems");

            migrationBuilder.DropTable(
                name: "PayrollComponents");

            migrationBuilder.DropTable(
                name: "SalaryStructures");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropIndex(
                name: "ix_hr_info_loan_receivable_account_id_tenant_id",
                table: "HRInfo");

            migrationBuilder.DropIndex(
                name: "ix_hr_info_salary_structure_id_tenant_id",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_account_no",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_account_title",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "department",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "employment_status",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "iban",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "loan_receivable_account_id",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "salary_structure_id",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "termination_date",
                table: "HRInfo");
        }
    }
}
