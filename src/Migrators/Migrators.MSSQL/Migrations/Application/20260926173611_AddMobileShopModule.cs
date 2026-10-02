using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddMobileShopModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "imei",
                table: "StockAdjDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                table: "StockAdjDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "battery_health",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_note",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pta_status",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_expiry_date",
                table: "Sales",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                table: "SaleRetDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                table: "SaleRetDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                table: "PurchaseRetDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                table: "PurchaseRetDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seller_cnic",
                table: "PurchaseMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seller_contact",
                table: "PurchaseMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "battery_health",
                table: "PurchaseDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_note",
                table: "PurchaseDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei",
                table: "PurchaseDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imei2",
                table: "PurchaseDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pta_status",
                table: "PurchaseDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_id",
                table: "ItemDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "ItemDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_name",
                table: "ItemDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ram",
                table: "ItemDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "require_imei",
                table: "ItemDetail",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage",
                table: "ItemDetail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_brand_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ImeiCostAddition",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    imei = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    expense_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    paid_from_account = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    consumed_item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    consumed_qty = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_imei_cost_addition_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_imei_cost_addition_chart_of_account_paid_from_account_tenant_id",
                        columns: x => new { x.paid_from_account, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_imei_cost_addition_item_detail_consumed_item_id_tenant_id",
                        columns: x => new { x.consumed_item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairJob",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    job_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_acc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    customer_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    brand_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    device_model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    imei = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    passcode_or_pattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fault_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    physical_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    advance_paid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    assigned_technician_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    expected_delivery = table.Column<DateTime>(type: "datetime2", nullable: true),
                    delivered_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sale_v_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_brand_brand_id_tenant_id",
                        columns: x => new { x.brand_id, x.tenant_id },
                        principalTable: "Brand",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_chart_of_account_customer_acc_tenant_id",
                        columns: x => new { x.customer_acc, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_hr_info_assigned_technician_id_tenant_id",
                        columns: x => new { x.assigned_technician_id, x.tenant_id },
                        principalTable: "HRInfo",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobPart",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    job_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    cost_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_part_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_part_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_part_repair_job_job_no_tenant_id",
                        columns: x => new { x.job_no, x.tenant_id },
                        principalTable: "RepairJob",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobService",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    job_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    service_item_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    technician_share = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_repair_job_service_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_repair_job_service_item_detail_service_item_id_tenant_id",
                        columns: x => new { x.service_item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_repair_job_service_repair_job_job_no_tenant_id",
                        columns: x => new { x.job_no, x.tenant_id },
                        principalTable: "RepairJob",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_brand_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "brand_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_imei_cost_addition_consumed_item_id_tenant_id",
                table: "ImeiCostAddition",
                columns: new[] { "consumed_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_imei_cost_addition_paid_from_account_tenant_id",
                table: "ImeiCostAddition",
                columns: new[] { "paid_from_account", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_assigned_technician_id_tenant_id",
                table: "RepairJob",
                columns: new[] { "assigned_technician_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_brand_id_tenant_id",
                table: "RepairJob",
                columns: new[] { "brand_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_customer_acc_tenant_id",
                table: "RepairJob",
                columns: new[] { "customer_acc", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_part_item_id_tenant_id",
                table: "RepairJobPart",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_part_job_no_tenant_id",
                table: "RepairJobPart",
                columns: new[] { "job_no", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_service_job_no_tenant_id",
                table: "RepairJobService",
                columns: new[] { "job_no", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_repair_job_service_service_item_id_tenant_id",
                table: "RepairJobService",
                columns: new[] { "service_item_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_item_detail_brand_brand_id_tenant_id",
                table: "ItemDetail",
                columns: new[] { "brand_id", "tenant_id" },
                principalTable: "Brand",
                principalColumns: new[] { "id", "tenant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_item_detail_brand_brand_id_tenant_id",
                table: "ItemDetail");

            migrationBuilder.DropTable(
                name: "ImeiCostAddition");

            migrationBuilder.DropTable(
                name: "RepairJobPart");

            migrationBuilder.DropTable(
                name: "RepairJobService");

            migrationBuilder.DropTable(
                name: "RepairJob");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropIndex(
                name: "ix_item_detail_brand_id_tenant_id",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "battery_health",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "condition_note",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei2",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "pta_status",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "warranty_expiry_date",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "imei",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "seller_cnic",
                table: "PurchaseMaster");

            migrationBuilder.DropColumn(
                name: "seller_contact",
                table: "PurchaseMaster");

            migrationBuilder.DropColumn(
                name: "battery_health",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "condition_note",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "imei",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "imei2",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "pta_status",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "brand_id",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "color",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "model_name",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "ram",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "require_imei",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "storage",
                table: "ItemDetail");
        }
    }
}
