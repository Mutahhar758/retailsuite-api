using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class FilterUniqueIndexesByDeletedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                schema: "public",
                table: "StockAdjMaster");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                schema: "public",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                schema: "public",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleSupplyDetail");

            migrationBuilder.DropIndex(
                name: "ix_sales_v_type_v_no_seq",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                schema: "public",
                table: "SaleRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_master_v_type_v_no",
                schema: "public",
                table: "SaleMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                schema: "public",
                table: "PurchaseRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_master_v_type_v_no",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropIndex(
                name: "ix_payroll_voucher_no_seq",
                schema: "public",
                table: "Payroll");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                schema: "public",
                table: "GL1");

            migrationBuilder.DropIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                schema: "public",
                table: "CustomerSupplyItem");

            migrationBuilder.AddColumn<decimal>(
                name: "cash_back",
                schema: "public",
                table: "PurchaseMaster",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cash_paid",
                schema: "public",
                table: "PurchaseMaster",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_settings_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                schema: "public",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                schema: "public",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                schema: "public",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                schema: "public",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                schema: "public",
                table: "PurchaseRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                schema: "public",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                schema: "public",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                schema: "public",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                schema: "public",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                schema: "public",
                table: "Settings",
                columns: new[] { "key", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                schema: "public",
                table: "StockAdjMaster");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                schema: "public",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                schema: "public",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleSupplyDetail");

            migrationBuilder.DropIndex(
                name: "ix_sales_v_type_v_no_seq",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                schema: "public",
                table: "SaleRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_master_v_type_v_no",
                schema: "public",
                table: "SaleMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                schema: "public",
                table: "PurchaseRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_master_v_type_v_no",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropIndex(
                name: "ix_payroll_voucher_no_seq",
                schema: "public",
                table: "Payroll");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                schema: "public",
                table: "GL1");

            migrationBuilder.DropIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                schema: "public",
                table: "CustomerSupplyItem");

            migrationBuilder.DropColumn(
                name: "cash_back",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.DropColumn(
                name: "cash_paid",
                schema: "public",
                table: "PurchaseMaster");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                schema: "public",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                schema: "public",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                schema: "public",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                schema: "public",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                schema: "public",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                schema: "public",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                schema: "public",
                table: "PurchaseRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                schema: "public",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                schema: "public",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                schema: "public",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                schema: "public",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                schema: "public",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true);
        }
    }
}
