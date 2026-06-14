using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auditing");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "audit_trails",
                schema: "auditing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    table_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    old_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    affected_columns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    primary_key = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_audit_trails_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccount",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    parent_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    acc_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    acc_level = table.Column<int>(type: "int", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_chart_of_account_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_chart_of_account_chart_of_account_parent_id_tenant_id",
                        columns: x => new { x.parent_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    company_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ur_company_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cell2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contact_header = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_company_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "CustomerDetail",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cnic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    qualification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sms_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sms_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    email_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    file_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    converted_file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    access_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_documents_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ItemCatagory",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    item_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_catagory_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "Narration",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_narration_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_roles_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SupplierDetail",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cnic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    qualification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sms_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sms_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    email_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    show_in_sales = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supplier_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SupplyOrderMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supply_order_master_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_units_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refresh_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    object_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    biometric_public_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    security_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                    access_failed_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_users_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "DefaultAccount",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    a = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    map_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_default_account_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_default_account_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_default_account_chart_of_account_map_account_id_tenant_id",
                        columns: x => new { x.map_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HRInfo",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    father_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: false),
                    maritial_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cnic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: false),
                    designation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    salary_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    leave_charges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    overtime = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    expense_account = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payable_account = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    expense_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    payable_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_hr_info_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_hr_info_chart_of_account_expense_account_id_tenant_id",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hr_info_chart_of_account_payable_account_id_tenant_id",
                        columns: x => new { x.payable_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GL1",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    voucher_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_seq = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    check_num = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    clear = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    dr_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    cr_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_gl1_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_gl1_chart_of_account_cr_account_id_tenant_id",
                        columns: x => new { x.cr_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gl1_chart_of_account_dr_account_id_tenant_id",
                        columns: x => new { x.dr_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gl1_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRetMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_ret_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_ret_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    net_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cash_receipt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    cash_back = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleRetMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    net_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cash_receipt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    cash_back = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_ret_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_ret_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    terminal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_stock_adj_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_stock_adj_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_role_claims_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id_tenant_id",
                        columns: x => new { x.role_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplyOrderDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    supply_order_master_id = table.Column<int>(type: "int", nullable: true),
                    customer_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supply_order_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_supply_order_detail_chart_of_account_customer_account_id_tenant_id",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supply_order_detail_supply_order_master_supply_order_master_id_tenant_id",
                        columns: x => new { x.supply_order_master_id, x.tenant_id },
                        principalTable: "SupplyOrderMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemDetail",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    item_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    item_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pri_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sec_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    primary_unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    secondary_unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    default_unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    low_stock_alert = table.Column<bool>(type: "bit", nullable: true),
                    opn_stock = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    opn_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    item_category_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_detail_item_catagory_item_category_id_tenant_id",
                        columns: x => new { x.item_category_id, x.tenant_id },
                        principalTable: "ItemCatagory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_default_unit_id_tenant_id",
                        columns: x => new { x.default_unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_primary_unit_id_tenant_id",
                        columns: x => new { x.primary_unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_secondary_unit_id_tenant_id",
                        columns: x => new { x.secondary_unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_user_claims_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_user_logins_login_provider_provider_key_tenant_id", x => new { x.login_provider, x.provider_key, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_user_roles_user_id_role_id_tenant_id", x => new { x.user_id, x.role_id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id_tenant_id",
                        columns: x => new { x.role_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    application_user_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    device_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fcm_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    device_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    type = table.Column<int>(type: "int", nullable: false),
                    is_remember = table.Column<bool>(type: "bit", nullable: false),
                    explicit_logout = table.Column<bool>(type: "bit", nullable: false),
                    version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_user_sessions_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_sessions_users_application_user_id_tenant_id",
                        columns: x => new { x.application_user_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumns: new[] { "id", "tenant_id" });
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_user_tokens_user_id_login_provider_name_tenant_id", x => new { x.user_id, x.login_provider, x.name, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payroll",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    voucher_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    salary_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    seq = table.Column<long>(type: "bigint", nullable: false),
                    salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    no_of_leaves = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    leave_charges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    overtime = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    overtime_charges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    bonus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    net_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hr_info_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    payable_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    expense_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_chart_of_account_expense_account_id_tenant_id",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_chart_of_account_payable_account_id_tenant_id",
                        columns: x => new { x.payable_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemTransaction",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    tran_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    qty_out = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_transaction_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_transaction_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_transaction_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_transaction_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    add_less = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    purchase_master_id = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_detail_purchase_master_purchase_master_id_tenant_id",
                        columns: x => new { x.purchase_master_id, x.tenant_id },
                        principalTable: "PurchaseMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRetDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    purchase_ret_master_id = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_ret_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_purchase_ret_master_purchase_ret_master_id_tenant_id",
                        columns: x => new { x.purchase_ret_master_id, x.tenant_id },
                        principalTable: "PurchaseRetMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleRetDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gross_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    sale_ret_master_id = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_ret_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_sale_ret_master_sale_ret_master_id_tenant_id",
                        columns: x => new { x.sale_ret_master_id, x.tenant_id },
                        principalTable: "SaleRetMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gross_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    sale_master_id = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sales_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sales_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_master_sale_master_id_tenant_id",
                        columns: x => new { x.sale_master_id, x.tenant_id },
                        principalTable: "SaleMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleSupplyMaster",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    descr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    narration_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    net_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    counter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_supply_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_supply_master_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    qty_in = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    qty_out = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    stock_adj_master_id = table.Column<int>(type: "int", nullable: true),
                    category_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_stock_adj_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_item_catagory_category_id_tenant_id",
                        columns: x => new { x.category_id, x.tenant_id },
                        principalTable: "ItemCatagory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_stock_adj_master_stock_adj_master_id_tenant_id",
                        columns: x => new { x.stock_adj_master_id, x.tenant_id },
                        principalTable: "StockAdjMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleSupplyDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    unit_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gross_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    add_less = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    sale_supply_master_id = table.Column<int>(type: "int", nullable: true),
                    customer_account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_supply_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_chart_of_account_customer_account_id_tenant_id",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_sale_supply_master_sale_supply_master_id_tenant_id",
                        columns: x => new { x.sale_supply_master_id, x.tenant_id },
                        principalTable: "SaleSupplyMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_account_parent_id_tenant_id",
                table: "ChartOfAccount",
                columns: new[] { "parent_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_default_account_account_id_tenant_id",
                table: "DefaultAccount",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_default_account_map_account_id_tenant_id",
                table: "DefaultAccount",
                columns: new[] { "map_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_cr_account_id_tenant_id",
                table: "GL1",
                columns: new[] { "cr_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_dr_account_id_tenant_id",
                table: "GL1",
                columns: new[] { "dr_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_narration_id_tenant_id",
                table: "GL1",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_expense_account_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_payable_account_id_tenant_id",
                table: "HRInfo",
                columns: new[] { "payable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_default_unit_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "default_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_item_category_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "item_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_primary_unit_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "primary_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_secondary_unit_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "secondary_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_account_id_tenant_id",
                table: "ItemTransaction",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_item_id_tenant_id",
                table: "ItemTransaction",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_unit_id_tenant_id",
                table: "ItemTransaction",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_expense_account_id_tenant_id",
                table: "Payroll",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_hr_info_id_tenant_id",
                table: "Payroll",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_payable_account_id_tenant_id",
                table: "Payroll",
                columns: new[] { "payable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_item_id_tenant_id",
                table: "PurchaseDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_purchase_master_id_tenant_id",
                table: "PurchaseDetail",
                columns: new[] { "purchase_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_unit_id_tenant_id",
                table: "PurchaseDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_account_id_tenant_id",
                table: "PurchaseMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_narration_id_tenant_id",
                table: "PurchaseMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_item_id_tenant_id",
                table: "PurchaseRetDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_purchase_ret_master_id_tenant_id",
                table: "PurchaseRetDetail",
                columns: new[] { "purchase_ret_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_unit_id_tenant_id",
                table: "PurchaseRetDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_account_id_tenant_id",
                table: "PurchaseRetMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_narration_id_tenant_id",
                table: "PurchaseRetMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                table: "PurchaseRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id_tenant_id",
                schema: "identity",
                table: "role_claims",
                columns: new[] { "role_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "roles",
                columns: new[] { "normalized_name", "tenant_id" },
                unique: true,
                filter: "[normalized_name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_account_id_tenant_id",
                table: "SaleMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_narration_id_tenant_id",
                table: "SaleMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_item_id_tenant_id",
                table: "SaleRetDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_sale_ret_master_id_tenant_id",
                table: "SaleRetDetail",
                columns: new[] { "sale_ret_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_unit_id_tenant_id",
                table: "SaleRetDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_account_id_tenant_id",
                table: "SaleRetMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_narration_id_tenant_id",
                table: "SaleRetMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_item_id_tenant_id",
                table: "Sales",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_master_id_tenant_id",
                table: "Sales",
                columns: new[] { "sale_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_unit_id_tenant_id",
                table: "Sales",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_customer_account_id_tenant_id",
                table: "SaleSupplyDetail",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_sale_supply_master_id_tenant_id",
                table: "SaleSupplyDetail",
                columns: new[] { "sale_supply_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_unit_id_tenant_id",
                table: "SaleSupplyDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_item_id_tenant_id",
                table: "SaleSupplyMaster",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_narration_id_tenant_id",
                table: "SaleSupplyMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_category_id_tenant_id",
                table: "StockAdjDetail",
                columns: new[] { "category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_item_id_tenant_id",
                table: "StockAdjDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_stock_adj_master_id_tenant_id",
                table: "StockAdjDetail",
                columns: new[] { "stock_adj_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_narration_id_tenant_id",
                table: "StockAdjMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_customer_account_id_tenant_id",
                table: "SupplyOrderDetail",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_supply_order_master_id_customer_account_id_sort_order",
                table: "SupplyOrderDetail",
                columns: new[] { "supply_order_master_id", "customer_account_id", "sort_order", "tenant_id" },
                unique: true,
                filter: "[supply_order_master_id] IS NOT NULL AND [customer_account_id] IS NOT NULL AND [sort_order] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_supply_order_master_id_tenant_id",
                table: "SupplyOrderDetail",
                columns: new[] { "supply_order_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id_tenant_id",
                schema: "identity",
                table: "user_claims",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id_tenant_id",
                schema: "identity",
                table: "user_logins",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id_tenant_id",
                schema: "identity",
                table: "user_roles",
                columns: new[] { "role_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_tenant_id",
                schema: "identity",
                table: "user_roles",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_application_user_id_tenant_id",
                table: "user_sessions",
                columns: new[] { "application_user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_id_tenant_id",
                schema: "identity",
                table: "user_tokens",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "users",
                columns: new[] { "normalized_user_name", "tenant_id" },
                unique: true,
                filter: "[normalized_user_name] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_trails",
                schema: "auditing");

            migrationBuilder.DropTable(
                name: "CompanyDetail");

            migrationBuilder.DropTable(
                name: "CustomerDetail");

            migrationBuilder.DropTable(
                name: "DefaultAccount");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "GL1");

            migrationBuilder.DropTable(
                name: "ItemTransaction");

            migrationBuilder.DropTable(
                name: "Payroll");

            migrationBuilder.DropTable(
                name: "PurchaseDetail");

            migrationBuilder.DropTable(
                name: "PurchaseRetDetail");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SaleRetDetail");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "SaleSupplyDetail");

            migrationBuilder.DropTable(
                name: "StockAdjDetail");

            migrationBuilder.DropTable(
                name: "SupplierDetail");

            migrationBuilder.DropTable(
                name: "SupplyOrderDetail");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "HRInfo");

            migrationBuilder.DropTable(
                name: "PurchaseMaster");

            migrationBuilder.DropTable(
                name: "PurchaseRetMaster");

            migrationBuilder.DropTable(
                name: "SaleRetMaster");

            migrationBuilder.DropTable(
                name: "SaleMaster");

            migrationBuilder.DropTable(
                name: "SaleSupplyMaster");

            migrationBuilder.DropTable(
                name: "StockAdjMaster");

            migrationBuilder.DropTable(
                name: "SupplyOrderMaster");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ChartOfAccount");

            migrationBuilder.DropTable(
                name: "ItemDetail");

            migrationBuilder.DropTable(
                name: "Narration");

            migrationBuilder.DropTable(
                name: "ItemCatagory");

            migrationBuilder.DropTable(
                name: "Units");
        }
    }
}
