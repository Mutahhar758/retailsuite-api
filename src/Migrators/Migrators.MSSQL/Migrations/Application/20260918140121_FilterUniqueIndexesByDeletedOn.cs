using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class FilterUniqueIndexesByDeletedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                table: "StockAdjMaster");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_settings_key",
                table: "Settings");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                table: "SaleSupplyDetail");

            migrationBuilder.DropIndex(
                name: "ix_sales_v_type_v_no_seq",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                table: "SaleRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                table: "SaleRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_master_v_type_v_no",
                table: "SaleMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                table: "PurchaseRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                table: "PurchaseRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_master_v_type_v_no",
                table: "PurchaseMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                table: "PurchaseDetail");

            migrationBuilder.DropIndex(
                name: "ix_payroll_voucher_no_seq",
                table: "Payroll");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                table: "ItemTransaction");

            migrationBuilder.DropIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                table: "GL1");

            migrationBuilder.DropIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                table: "CustomerSupplyItem");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                table: "Settings",
                columns: new[] { "key", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                table: "PurchaseRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                table: "StockAdjMaster");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_settings_key",
                table: "Settings");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                table: "SaleSupplyDetail");

            migrationBuilder.DropIndex(
                name: "ix_sales_v_type_v_no_seq",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                table: "SaleRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                table: "SaleRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_master_v_type_v_no",
                table: "SaleMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                table: "PurchaseRetMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                table: "PurchaseRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_master_v_type_v_no",
                table: "PurchaseMaster");

            migrationBuilder.DropIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                table: "PurchaseDetail");

            migrationBuilder.DropIndex(
                name: "ix_payroll_voucher_no_seq",
                table: "Payroll");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                table: "ItemTransaction");

            migrationBuilder.DropIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                table: "GL1");

            migrationBuilder.DropIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                table: "CustomerSupplyItem");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_master_v_type_v_no",
                table: "StockAdjMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_v_type_v_no_seq",
                table: "StockAdjDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                table: "Settings",
                columns: new[] { "key", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_v_type_v_no",
                table: "SaleSupplyMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_v_type_v_no_seq",
                table: "SaleSupplyDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_v_type_v_no_seq",
                table: "Sales",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_master_v_type_v_no",
                table: "SaleRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_v_type_v_no_seq",
                table: "SaleRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_master_v_type_v_no",
                table: "SaleMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_master_v_type_v_no",
                table: "PurchaseRetMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_v_type_v_no_seq",
                table: "PurchaseRetDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_master_v_type_v_no",
                table: "PurchaseMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_v_type_v_no_seq",
                table: "PurchaseDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_voucher_no_seq",
                table: "Payroll",
                columns: new[] { "voucher_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_v_type_v_no_seq",
                table: "ItemTransaction",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gl1_v_type_voucher_no_v_seq",
                table: "GL1",
                columns: new[] { "v_type", "voucher_no", "v_seq", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true);
        }
    }
}
