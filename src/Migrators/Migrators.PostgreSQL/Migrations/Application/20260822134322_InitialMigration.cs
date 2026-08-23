using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
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
                name: "public");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "audit_trails",
                schema: "auditing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    table_name = table.Column<string>(type: "text", nullable: true),
                    date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    affected_columns = table.Column<string>(type: "text", nullable: true),
                    primary_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_audit_trails_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccount",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<string>(type: "text", nullable: true),
                    acc_type = table.Column<string>(type: "text", nullable: false),
                    acc_level = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_chart_of_account_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_chart_of_account_chart_of_account_parent_id_tenant_id",
                        columns: x => new { x.parent_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: false),
                    ur_company_name = table.Column<string>(type: "text", nullable: true),
                    descr = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    cell = table.Column<string>(type: "text", nullable: true),
                    cell2 = table.Column<string>(type: "text", nullable: true),
                    contact_header = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_company_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "CustomerDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    fax = table.Column<string>(type: "text", nullable: true),
                    cnic = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    qualification = table.Column<string>(type: "text", nullable: true),
                    phone1 = table.Column<string>(type: "text", nullable: true),
                    phone2 = table.Column<string>(type: "text", nullable: true),
                    sms_number = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    sms_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    email_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    media_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "DiningTables",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_dining_tables_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: false),
                    converted_file_name = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: false),
                    path = table.Column<string>(type: "text", nullable: true),
                    access_url = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_documents_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "Narration",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_narration_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "PrepStations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_prep_stations_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_roles_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SupplierDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    fax = table.Column<string>(type: "text", nullable: true),
                    cnic = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    qualification = table.Column<string>(type: "text", nullable: true),
                    phone1 = table.Column<string>(type: "text", nullable: true),
                    phone2 = table.Column<string>(type: "text", nullable: true),
                    sms_number = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    sms_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    email_alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    show_in_sales = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    media_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supplier_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "SupplyOrderMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supply_order_master_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
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
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    biometric_public_key = table.Column<string>(type: "text", nullable: true),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_users_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "DefaultAccount",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    a = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    map_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_default_account_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_default_account_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_default_account_chart_of_account_map_account_id_tenant_id",
                        columns: x => new { x.map_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HRInfo",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    father_name = table.Column<string>(type: "text", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: false),
                    maritial_status = table.Column<string>(type: "text", nullable: true),
                    cnic = table.Column<string>(type: "text", nullable: true),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: false),
                    designation = table.Column<string>(type: "text", nullable: true),
                    salary_type = table.Column<string>(type: "text", nullable: false),
                    salary = table.Column<decimal>(type: "numeric", nullable: false),
                    leave_charges = table.Column<decimal>(type: "numeric", nullable: false),
                    overtime = table.Column<decimal>(type: "numeric", nullable: false),
                    expense_account = table.Column<string>(type: "text", nullable: true),
                    payable_account = table.Column<string>(type: "text", nullable: true),
                    expense_account_id = table.Column<string>(type: "text", nullable: true),
                    payable_account_id = table.Column<string>(type: "text", nullable: true),
                    media_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_hr_info_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_hr_info_chart_of_account_expense_account_id_tenant_id",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hr_info_chart_of_account_payable_account_id_tenant_id",
                        columns: x => new { x.payable_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KotOrders",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    token_no = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    order_type = table.Column<string>(type: "text", nullable: false),
                    table_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    sale_voucher_no = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<string>(type: "text", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_kot_orders_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_kot_orders_dining_tables_table_id_tenant_id",
                        columns: x => new { x.table_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "DiningTables",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GL1",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    voucher_no = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_seq = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    check_num = table.Column<string>(type: "text", nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    clearing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_status = table.Column<string>(type: "text", nullable: true),
                    clear = table.Column<decimal>(type: "numeric", nullable: false),
                    dr_account_id = table.Column<string>(type: "text", nullable: true),
                    cr_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_gl1_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_gl1_chart_of_account_cr_account_id_tenant_id",
                        columns: x => new { x.cr_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gl1_chart_of_account_dr_account_id_tenant_id",
                        columns: x => new { x.dr_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gl1_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRetMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_ret_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_ret_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    cash_receipt = table.Column<decimal>(type: "numeric", nullable: false),
                    cash_back = table.Column<decimal>(type: "numeric", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleRetMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    cash_receipt = table.Column<decimal>(type: "numeric", nullable: false),
                    cash_back = table.Column<decimal>(type: "numeric", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_ret_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_ret_master_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    terminal = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_stock_adj_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_stock_adj_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemCatagory",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: false),
                    media_id = table.Column<string>(type: "text", nullable: true),
                    prep_station_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_catagory_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_catagory_prep_stations_prep_station_id_tenant_id",
                        columns: x => new { x.prep_station_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PrepStations",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
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
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    supply_order_master_id = table.Column<int>(type: "integer", nullable: true),
                    customer_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_supply_order_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_supply_order_detail_chart_of_account_customer_account_id_tenant",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supply_order_detail_supply_order_master_supply_order_master_id_",
                        columns: x => new { x.supply_order_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SupplyOrderMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
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
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
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
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
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
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    application_user_id = table.Column<string>(type: "text", nullable: true),
                    token = table.Column<string>(type: "text", nullable: true),
                    device_id = table.Column<string>(type: "text", nullable: true),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_remember = table.Column<bool>(type: "boolean", nullable: false),
                    explicit_logout = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
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
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
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
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    voucher_no = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    salary_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    seq = table.Column<long>(type: "bigint", nullable: false),
                    salary = table.Column<decimal>(type: "numeric", nullable: false),
                    no_of_leaves = table.Column<decimal>(type: "numeric", nullable: false),
                    leave_charges = table.Column<decimal>(type: "numeric", nullable: false),
                    overtime = table.Column<decimal>(type: "numeric", nullable: false),
                    overtime_charges = table.Column<decimal>(type: "numeric", nullable: false),
                    bonus = table.Column<decimal>(type: "numeric", nullable: false),
                    net_salary = table.Column<decimal>(type: "numeric", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    hr_info_id = table.Column<string>(type: "text", nullable: true),
                    payable_account_id = table.Column<string>(type: "text", nullable: true),
                    expense_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_payroll_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_payroll_chart_of_account_expense_account_id_tenant_id",
                        columns: x => new { x.expense_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_chart_of_account_payable_account_id_tenant_id",
                        columns: x => new { x.payable_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_hr_info_hr_info_id_tenant_id",
                        columns: x => new { x.hr_info_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    barcode = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    item_key = table.Column<string>(type: "text", nullable: true),
                    pri_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    primary_unit_id = table.Column<string>(type: "text", nullable: true),
                    secondary_unit_id = table.Column<string>(type: "text", nullable: true),
                    default_unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    alert = table.Column<decimal>(type: "numeric(1,0)", nullable: true),
                    low_stock_alert = table.Column<bool>(type: "boolean", nullable: true),
                    opn_stock = table.Column<decimal>(type: "numeric", nullable: true),
                    opn_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    media_id = table.Column<string>(type: "text", nullable: true),
                    quick_qty_presets = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    item_category_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_detail_item_catagory_item_category_id_tenant_id",
                        columns: x => new { x.item_category_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemCatagory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_default_unit_id_tenant_id",
                        columns: x => new { x.default_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_primary_unit_id_tenant_id",
                        columns: x => new { x.primary_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_detail_units_secondary_unit_id_tenant_id",
                        columns: x => new { x.secondary_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSupplyItem",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    customer_account_id = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<string>(type: "text", nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sec_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_supply_item_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_customer_supply_item_chart_of_account_customer_account_id_tenan",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_supply_item_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemTransaction",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    tran_type = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in = table.Column<decimal>(type: "numeric", nullable: false),
                    qty_out = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    counter = table.Column<string>(type: "text", nullable: true),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty_in = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_qty_out = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_transaction_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_transaction_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_transaction_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_transaction_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_transaction_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KotOrderItems",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    kot_order_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<string>(type: "text", nullable: false),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_kot_order_items_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_kot_order_items_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kot_order_items_kot_orders_kot_order_id_tenant_id",
                        columns: x => new { x.kot_order_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "KotOrders",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    add_less = table.Column<decimal>(type: "numeric", nullable: false),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    purchase_master_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_detail_purchase_master_purchase_master_id_tenant_id",
                        columns: x => new { x.purchase_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PurchaseMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_detail_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRetDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    purchase_ret_master_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_purchase_ret_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_purchase_ret_master_purchase_ret_master_id_",
                        columns: x => new { x.purchase_ret_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "PurchaseRetMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_ret_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleRetDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    sale_ret_master_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_ret_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_sale_ret_master_sale_ret_master_id_tenant_id",
                        columns: x => new { x.sale_ret_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SaleRetMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_ret_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    sale_master_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sales_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sales_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_master_sale_master_id_tenant_id",
                        columns: x => new { x.sale_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SaleMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleSupplyMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    descr = table.Column<string>(type: "text", nullable: true),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    supply_order_master_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_supply_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_supply_master_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_master_supply_order_master_supply_order_master_id_t",
                        columns: x => new { x.supply_order_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SupplyOrderMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    qty_in = table.Column<decimal>(type: "numeric", nullable: false),
                    qty_out = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty_in = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_qty_out = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    stock_adj_master_id = table.Column<int>(type: "integer", nullable: true),
                    category_id = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_stock_adj_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_item_catagory_category_id_tenant_id",
                        columns: x => new { x.category_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemCatagory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_stock_adj_master_stock_adj_master_id_tenant_id",
                        columns: x => new { x.stock_adj_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "StockAdjMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adj_detail_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleSupplyDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_type = table.Column<string>(type: "text", nullable: false),
                    v_no = table.Column<string>(type: "text", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    discount = table.Column<decimal>(type: "numeric", nullable: true),
                    add_less = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_unit_id = table.Column<string>(type: "text", nullable: true),
                    sec_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    sec_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    qty_in_pack = table.Column<decimal>(type: "numeric", nullable: true),
                    packing = table.Column<decimal>(type: "numeric", nullable: true),
                    sale_supply_master_id = table.Column<int>(type: "integer", nullable: true),
                    customer_account_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_sale_supply_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_chart_of_account_customer_account_id_tenant_",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_sale_supply_master_sale_supply_master_id_ten",
                        columns: x => new { x.sale_supply_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "SaleSupplyMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_units_sec_unit_id_tenant_id",
                        columns: x => new { x.sec_unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_supply_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_account_parent_id_tenant_id",
                schema: "public",
                table: "ChartOfAccount",
                columns: new[] { "parent_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                schema: "public",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_tenant_id",
                schema: "public",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_item_id_tenant_id",
                schema: "public",
                table: "CustomerSupplyItem",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_default_account_account_id_tenant_id",
                schema: "public",
                table: "DefaultAccount",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_default_account_map_account_id_tenant_id",
                schema: "public",
                table: "DefaultAccount",
                columns: new[] { "map_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_cr_account_id_tenant_id",
                schema: "public",
                table: "GL1",
                columns: new[] { "cr_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_dr_account_id_tenant_id",
                schema: "public",
                table: "GL1",
                columns: new[] { "dr_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_narration_id_tenant_id",
                schema: "public",
                table: "GL1",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                schema: "public",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_expense_account_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hr_info_payable_account_id_tenant_id",
                schema: "public",
                table: "HRInfo",
                columns: new[] { "payable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_catagory_prep_station_id_tenant_id",
                schema: "public",
                table: "ItemCatagory",
                columns: new[] { "prep_station_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_default_unit_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "default_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_item_category_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "item_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_primary_unit_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "primary_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_secondary_unit_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "secondary_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_account_id_tenant_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_item_id_tenant_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_sec_unit_id_tenant_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_unit_id_tenant_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kot_order_items_item_id_tenant_id",
                schema: "public",
                table: "KotOrderItems",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kot_order_items_kot_order_id_tenant_id",
                schema: "public",
                table: "KotOrderItems",
                columns: new[] { "kot_order_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kot_orders_table_id_tenant_id",
                schema: "public",
                table: "KotOrders",
                columns: new[] { "table_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_expense_account_id_tenant_id",
                schema: "public",
                table: "Payroll",
                columns: new[] { "expense_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_hr_info_id_tenant_id",
                schema: "public",
                table: "Payroll",
                columns: new[] { "hr_info_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_payable_account_id_tenant_id",
                schema: "public",
                table: "Payroll",
                columns: new[] { "payable_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                schema: "public",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_item_id_tenant_id",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_purchase_master_id_tenant_id",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "purchase_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_sec_unit_id_tenant_id",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_unit_id_tenant_id",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_account_id_tenant_id",
                schema: "public",
                table: "PurchaseMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_narration_id_tenant_id",
                schema: "public",
                table: "PurchaseMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                schema: "public",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_item_id_tenant_id",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_purchase_ret_master_id_tenant_id",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "purchase_ret_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_sec_unit_id_tenant_id",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_unit_id_tenant_id",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_account_id_tenant_id",
                schema: "public",
                table: "PurchaseRetMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_narration_id_tenant_id",
                schema: "public",
                table: "PurchaseRetMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                schema: "public",
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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_account_id_tenant_id",
                schema: "public",
                table: "SaleMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_narration_id_tenant_id",
                schema: "public",
                table: "SaleMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                schema: "public",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_item_id_tenant_id",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_sale_ret_master_id_tenant_id",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "sale_ret_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_sec_unit_id_tenant_id",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_unit_id_tenant_id",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_account_id_tenant_id",
                schema: "public",
                table: "SaleRetMaster",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_narration_id_tenant_id",
                schema: "public",
                table: "SaleRetMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                schema: "public",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_item_id_tenant_id",
                schema: "public",
                table: "Sales",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_master_id_tenant_id",
                schema: "public",
                table: "Sales",
                columns: new[] { "sale_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sec_unit_id_tenant_id",
                schema: "public",
                table: "Sales",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_unit_id_tenant_id",
                schema: "public",
                table: "Sales",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                schema: "public",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_customer_account_id_tenant_id",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_sale_supply_master_id_tenant_id",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "sale_supply_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_sec_unit_id_tenant_id",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_unit_id_tenant_id",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_item_id_tenant_id",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_narration_id_tenant_id",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_supply_order_master_id_tenant_id",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "supply_order_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_category_id_tenant_id",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_item_id_tenant_id",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_sec_unit_id_tenant_id",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_stock_adj_master_id_tenant_id",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "stock_adj_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_narration_id_tenant_id",
                schema: "public",
                table: "StockAdjMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                schema: "public",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_customer_account_id_tenant_id",
                schema: "public",
                table: "SupplyOrderDetail",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_supply_order_master_id_customer_account_i",
                schema: "public",
                table: "SupplyOrderDetail",
                columns: new[] { "supply_order_master_id", "customer_account_id", "sort_order", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_supply_order_master_id_tenant_id",
                schema: "public",
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
                schema: "public",
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
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_trails",
                schema: "auditing");

            migrationBuilder.DropTable(
                name: "CompanyDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CustomerDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CustomerSupplyItem",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DefaultAccount",
                schema: "public");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "GL1",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ItemTransaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "KotOrderItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payroll",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PurchaseDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PurchaseRetDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SaleRetDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Sales",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SaleSupplyDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StockAdjDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SupplierDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SupplyOrderDetail",
                schema: "public");

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
                name: "user_sessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "KotOrders",
                schema: "public");

            migrationBuilder.DropTable(
                name: "HRInfo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PurchaseMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PurchaseRetMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SaleRetMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SaleMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SaleSupplyMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StockAdjMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "DiningTables",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ChartOfAccount",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ItemDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SupplyOrderMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Narration",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ItemCatagory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Units",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PrepStations",
                schema: "public");
        }
    }
}
