using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddMobileShopModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "imei",
                schema: "public",
                table: "StockAdjDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                schema: "public",
                table: "StockAdjDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "battery_health",
                schema: "public",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_note",
                schema: "public",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                schema: "public",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                schema: "public",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pta_status",
                schema: "public",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_expiry_date",
                schema: "public",
                table: "Sales",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                schema: "public",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                schema: "public",
                table: "SaleRetDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                schema: "public",
                table: "SaleRetDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                schema: "public",
                table: "PurchaseRetDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                schema: "public",
                table: "PurchaseRetDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seller_cnic",
                schema: "public",
                table: "PurchaseMaster",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seller_contact",
                schema: "public",
                table: "PurchaseMaster",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "battery_health",
                schema: "public",
                table: "PurchaseDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_note",
                schema: "public",
                table: "PurchaseDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                schema: "public",
                table: "PurchaseDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                schema: "public",
                table: "PurchaseDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pta_status",
                schema: "public",
                table: "PurchaseDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_id",
                schema: "public",
                table: "ItemDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "public",
                table: "ItemDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_name",
                schema: "public",
                table: "ItemDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ram",
                schema: "public",
                table: "ItemDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "require_imei",
                schema: "public",
                table: "ItemDetail",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage",
                schema: "public",
                table: "ItemDetail",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Brand",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("ak_brand_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ImeiCostAddition",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    imei = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    expense_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    paid_from_account = table.Column<string>(type: "text", nullable: true),
                    consumed_item_id = table.Column<string>(type: "text", nullable: true),
                    consumed_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_imei_cost_addition_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_imei_cost_addition_chart_of_account_paid_from_account_tenant_id",
                        columns: x => new { x.paid_from_account, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_imei_cost_addition_item_detail_consumed_item_id_tenant_id",
                        columns: x => new { x.consumed_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairJob",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    job_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_acc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: true),
                    customer_phone = table.Column<string>(type: "text", nullable: true),
                    brand_id = table.Column<string>(type: "text", nullable: true),
                    device_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    imei = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    passcode_or_pattern = table.Column<string>(type: "text", nullable: true),
                    fault_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    physical_condition = table.Column<string>(type: "text", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric", nullable: true),
                    advance_paid = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_technician_id = table.Column<string>(type: "text", nullable: true),
                    expected_delivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sale_v_no = table.Column<string>(type: "text", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_brand_brand_id_tenant_id",
                        columns: x => new { x.brand_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Brand",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_chart_of_account_customer_acc_tenant_id",
                        columns: x => new { x.customer_acc, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_hr_info_assigned_technician_id_tenant_id",
                        columns: x => new { x.assigned_technician_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobPart",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    job_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    cost_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_part_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_part_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_part_repair_job_job_no_tenant_id",
                        columns: x => new { x.job_no, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "RepairJob",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobService",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    job_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    service_item_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    technician_share = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_service_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_service_item_detail_service_item_id_tenant_id",
                        columns: x => new { x.service_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_service_repair_job_job_no_tenant_id",
                        columns: x => new { x.job_no, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "RepairJob",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_brand_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "brand_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_imei_cost_addition_consumed_item_id_tenant_id",
                schema: "public",
                table: "ImeiCostAddition",
                columns: new[] { "consumed_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_imei_cost_addition_paid_from_account_tenant_id",
                schema: "public",
                table: "ImeiCostAddition",
                columns: new[] { "paid_from_account", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_assigned_technician_id_tenant_id",
                schema: "public",
                table: "RepairJob",
                columns: new[] { "assigned_technician_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_brand_id_tenant_id",
                schema: "public",
                table: "RepairJob",
                columns: new[] { "brand_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_customer_acc_tenant_id",
                schema: "public",
                table: "RepairJob",
                columns: new[] { "customer_acc", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_part_item_id_tenant_id",
                schema: "public",
                table: "RepairJobPart",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_part_job_no_tenant_id",
                schema: "public",
                table: "RepairJobPart",
                columns: new[] { "job_no", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_service_job_no_tenant_id",
                schema: "public",
                table: "RepairJobService",
                columns: new[] { "job_no", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_service_service_item_id_tenant_id",
                schema: "public",
                table: "RepairJobService",
                columns: new[] { "service_item_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_item_detail_brand_brand_id_tenant_id",
                schema: "public",
                table: "ItemDetail",
                columns: new[] { "brand_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "Brand",
                principalColumns: new[] { "id", "tenant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_item_detail_brand_brand_id_tenant_id",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropTable(
                name: "ImeiCostAddition",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RepairJobPart",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RepairJobService",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RepairJob",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Brand",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_item_detail_brand_id_tenant_id",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                schema: "public",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                schema: "public",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "battery_health",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "condition_note",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei2",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "pta_status",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "warranty_expiry_date",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei",
                schema: "public",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                schema: "public",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                schema: "public",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                schema: "public",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "seller_cnic",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.DropColumn(
                name: "seller_contact",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.DropColumn(
                name: "battery_health",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "condition_note",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "pta_status",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "brand_id",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "model_name",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "ram",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "require_imei",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "storage",
                schema: "public",
                table: "ItemDetail");
        }
    }
}
