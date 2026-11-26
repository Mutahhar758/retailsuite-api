using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEnterprisePayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account_no",
                schema: "public",
                table: "HRInfo",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_title",
                schema: "public",
                table: "HRInfo",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                schema: "public",
                table: "HRInfo",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "public",
                table: "HRInfo",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_status",
                schema: "public",
                table: "HRInfo",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "iban",
                schema: "public",
                table: "HRInfo",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "loan_receivable_account_id",
                schema: "public",
                table: "HRInfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                schema: "public",
                table: "HRInfo",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddColumn<int>(
                name: "salary_structure_id",
                schema: "public",
                table: "HRInfo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "termination_date",
                schema: "public",
                table: "HRInfo",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    loan_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    hr_info_id = table.Column<string>(type: "text", nullable: false),
                    loan_type = table.Column<int>(type: "integer", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    installment_count = table.Column<int>(type: "integer", nullable: false),
                    installment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: false),
                    start_month = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    disbursement_date = table.Column<DateOnly>(type: "date", nullable: true),
                    disbursement_account_id = table.Column<string>(type: "text", nullable: true),
                    receivable_account_id = table.Column<string>(type: "text", nullable: true),
                    gl_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approved_by = table.Column<string>(type: "text", nullable: true),
                    approved_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_loans_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_loans_chart_of_account_disbursement_account_id_tenant",
                        columns: x => new { x.disbursement_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_loans_chart_of_account_receivable_account_id_tenant_id",
                        columns: x => new { x.receivable_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_loans_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    component_type = table.Column<int>(type: "integer", nullable: false),
                    calculation_type = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    formula_expression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_payslip = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expense_account_id = table.Column<string>(type: "text", nullable: true),
                    liability_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_components_chart_of_account_expense_account_id_tenant_",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_components_chart_of_account_liability_account_id_tenan",
                        columns: x => new { x.liability_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    run_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    pay_year = table.Column<int>(type: "integer", nullable: false),
                    pay_month = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    pay_date = table.Column<DateOnly>(type: "date", nullable: false),
                    salary_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    employee_count = table.Column<int>(type: "integer", nullable: false),
                    total_gross_earnings = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_loan_recovery = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_net_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gl_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reversal_gl_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    submitted_by = table.Column<string>(type: "text", nullable: true),
                    submitted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    approved_by = table.Column<string>(type: "text", nullable: true),
                    approved_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "text", nullable: true),
                    posted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_runs_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructures",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    salary_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_salary_structures_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryComponents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    hr_info_id = table.Column<string>(type: "text", nullable: false),
                    payroll_component_id = table.Column<int>(type: "integer", nullable: false),
                    calculation_type_override = table.Column<int>(type: "integer", nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    formula_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_salary_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_salary_components_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_salary_components_payroll_components_payroll_componen",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDisbursements",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    disbursement_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payroll_run_id = table.Column<int>(type: "integer", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_account_id = table.Column<string>(type: "text", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gl_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_disbursements_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_disbursements_chart_of_account_source_account_id_tenan",
                        columns: x => new { x.source_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_disbursements_payroll_runs_payroll_run_id_tenant_id",
                        columns: x => new { x.payroll_run_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunItems",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    payroll_run_id = table.Column<int>(type: "integer", nullable: false),
                    hr_info_id = table.Column<string>(type: "text", nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    designation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cnic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    bank_account_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_no = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    iban = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    expense_account_id = table.Column<string>(type: "text", nullable: true),
                    payable_account_id = table.Column<string>(type: "text", nullable: true),
                    working_days = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    present_days = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unpaid_leave_days = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    leave_charge_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    overtime_hours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    overtime_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bonus_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    basic_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_earnings = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    loan_recovery_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_on_hold = table.Column<bool>(type: "boolean", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_run_items_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_run_items_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_run_items_payroll_runs_payroll_run_id_tenant_id",
                        columns: x => new { x.payroll_run_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructureLines",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    salary_structure_id = table.Column<int>(type: "integer", nullable: false),
                    payroll_component_id = table.Column<int>(type: "integer", nullable: false),
                    calculation_type_override = table.Column<int>(type: "integer", nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    formula_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_salary_structure_lines_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_salary_structure_lines_payroll_components_payroll_component_id",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_salary_structure_lines_salary_structures_salary_structure_id_t",
                        columns: x => new { x.salary_structure_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SalaryStructures",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoanSchedules",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    employee_loan_id = table.Column<int>(type: "integer", nullable: false),
                    installment_no = table.Column<int>(type: "integer", nullable: false),
                    due_year = table.Column<int>(type: "integer", nullable: false),
                    due_month = table.Column<int>(type: "integer", nullable: false),
                    scheduled_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deducted_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "integer", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_employee_loan_schedules_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_employee_loan_schedules_employee_loans_employee_loan_id_tenant",
                        columns: x => new { x.employee_loan_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "EmployeeLoans",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_loan_schedules_payroll_run_items_payroll_run_item_id_t",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDisbursementLines",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    payroll_disbursement_id = table.Column<int>(type: "integer", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "integer", nullable: false),
                    hr_info_id = table.Column<string>(type: "text", nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bank_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    bank_account_snapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    iban_snapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_disbursement_lines_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_payroll_disbursements_payroll_disbu",
                        columns: x => new { x.payroll_disbursement_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollDisbursements",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payroll_disbursement_lines_payroll_run_items_payroll_run_item_i",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunItemComponents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    payroll_run_item_id = table.Column<int>(type: "integer", nullable: false),
                    payroll_component_id = table.Column<int>(type: "integer", nullable: true),
                    component_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    component_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    component_type = table.Column<int>(type: "integer", nullable: false),
                    calculation_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_payslip = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    gl_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_run_item_components_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_run_item_components_payroll_components_payroll_componen",
                        columns: x => new { x.payroll_component_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollComponents",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payroll_run_item_components_payroll_run_items_payroll_run_item_i",
                        columns: x => new { x.payroll_run_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PayrollRunItems",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_loan_receivable_account_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "loan_receivable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_salary_structure_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "salary_structure_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_disbursement_account_id_tenant_id",
                schema: "public",
                table: "EmployeeLoans",
                columns: new[] { "disbursement_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_hr_info_id_tenant_id",
                schema: "public",
                table: "EmployeeLoans",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_loan_no",
                schema: "public",
                table: "EmployeeLoans",
                columns: new[] { "loan_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_receivable_account_id_tenant_id",
                schema: "public",
                table: "EmployeeLoans",
                columns: new[] { "receivable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_employee_loan_id_installment_no",
                schema: "public",
                table: "EmployeeLoanSchedules",
                columns: new[] { "employee_loan_id", "installment_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_employee_loan_id_tenant_id",
                schema: "public",
                table: "EmployeeLoanSchedules",
                columns: new[] { "employee_loan_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loan_schedules_payroll_run_item_id_tenant_id",
                schema: "public",
                table: "EmployeeLoanSchedules",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_hr_info_id_tenant_id",
                schema: "public",
                table: "EmployeeSalaryComponents",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_payroll_component_id_tenant_id",
                schema: "public",
                table: "EmployeeSalaryComponents",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_code",
                schema: "public",
                table: "PayrollComponents",
                columns: new[] { "code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_expense_account_id_tenant_id",
                schema: "public",
                table: "PayrollComponents",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_components_liability_account_id_tenant_id",
                schema: "public",
                table: "PayrollComponents",
                columns: new[] { "liability_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_hr_info_id_tenant_id",
                schema: "public",
                table: "PayrollDisbursementLines",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_payroll_disbursement_id_tenant_id",
                schema: "public",
                table: "PayrollDisbursementLines",
                columns: new[] { "payroll_disbursement_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursement_lines_payroll_run_item_id_tenant_id",
                schema: "public",
                table: "PayrollDisbursementLines",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_disbursement_no",
                schema: "public",
                table: "PayrollDisbursements",
                columns: new[] { "disbursement_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_payroll_run_id_tenant_id",
                schema: "public",
                table: "PayrollDisbursements",
                columns: new[] { "payroll_run_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_disbursements_source_account_id_tenant_id",
                schema: "public",
                table: "PayrollDisbursements",
                columns: new[] { "source_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_item_components_payroll_component_id_tenant_id",
                schema: "public",
                table: "PayrollRunItemComponents",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_item_components_payroll_run_item_id_tenant_id",
                schema: "public",
                table: "PayrollRunItemComponents",
                columns: new[] { "payroll_run_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_hr_info_id_tenant_id",
                schema: "public",
                table: "PayrollRunItems",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_payroll_run_id_hr_info_id",
                schema: "public",
                table: "PayrollRunItems",
                columns: new[] { "payroll_run_id", "hr_info_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_items_payroll_run_id_tenant_id",
                schema: "public",
                table: "PayrollRunItems",
                columns: new[] { "payroll_run_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_run_no",
                schema: "public",
                table: "PayrollRuns",
                columns: new[] { "run_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_payroll_component_id_tenant_id",
                schema: "public",
                table: "SalaryStructureLines",
                columns: new[] { "payroll_component_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_salary_structure_id_payroll_component_",
                schema: "public",
                table: "SalaryStructureLines",
                columns: new[] { "salary_structure_id", "payroll_component_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_lines_salary_structure_id_tenant_id",
                schema: "public",
                table: "SalaryStructureLines",
                columns: new[] { "salary_structure_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structures_code",
                schema: "public",
                table: "SalaryStructures",
                columns: new[] { "code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_hr_info_chart_of_account_loan_receivable_account_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "loan_receivable_account_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "ChartOfAccount",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hr_info_salary_structures_salary_structure_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "salary_structure_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "SalaryStructures",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hr_info_chart_of_account_loan_receivable_account_id_tenant_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropForeignKey(
                name: "fk_hr_info_salary_structures_salary_structure_id_tenant_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropTable(
                name: "EmployeeLoanSchedules",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EmployeeSalaryComponents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollDisbursementLines",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollRunItemComponents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SalaryStructureLines",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EmployeeLoans",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollDisbursements",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollRunItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollComponents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SalaryStructures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PayrollRuns",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_hr_info_loan_receivable_account_id_tenant_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropIndex(
                name: "ix_hr_info_salary_structure_id_tenant_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_account_no",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_account_title",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "bank_name",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "employment_status",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "iban",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "loan_receivable_account_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "payment_method",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "salary_structure_id",
                schema: "public",
                table: "HRInfo");

            migrationBuilder.DropColumn(
                name: "termination_date",
                schema: "public",
                table: "HRInfo");
        }
    }
}
