using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddSecondaryQtyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "qty_in_pack",
                table: "StockAdjDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty_in",
                table: "StockAdjDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty_out",
                table: "StockAdjDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "StockAdjDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "StockAdjDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "qty_in_pack",
                table: "SaleSupplyDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty",
                table: "SaleSupplyDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "SaleSupplyDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "SaleSupplyDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "Sales",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty",
                table: "SaleRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "SaleRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "SaleRetDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty",
                table: "PurchaseRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "PurchaseRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "PurchaseRetDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty",
                table: "PurchaseDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "PurchaseDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "PurchaseDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty_in",
                table: "ItemTransaction",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_qty_out",
                table: "ItemTransaction",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sec_rate",
                table: "ItemTransaction",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sec_unit_id",
                table: "ItemTransaction",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adj_detail_sec_unit_id_tenant_id",
                table: "StockAdjDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_detail_sec_unit_id_tenant_id",
                table: "SaleSupplyDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sec_unit_id_tenant_id",
                table: "Sales",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_ret_detail_sec_unit_id_tenant_id",
                table: "SaleRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_ret_detail_sec_unit_id_tenant_id",
                table: "PurchaseRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_detail_sec_unit_id_tenant_id",
                table: "PurchaseDetail",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_sec_unit_id_tenant_id",
                table: "ItemTransaction",
                columns: new[] { "sec_unit_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_item_transaction_units_sec_unit_id_tenant_id",
                table: "ItemTransaction",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_detail_units_sec_unit_id_tenant_id",
                table: "PurchaseDetail",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_ret_detail_units_sec_unit_id_tenant_id",
                table: "PurchaseRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_ret_detail_units_sec_unit_id_tenant_id",
                table: "SaleRetDetail",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_units_sec_unit_id_tenant_id",
                table: "Sales",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_supply_detail_units_sec_unit_id_tenant_id",
                table: "SaleSupplyDetail",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adj_detail_units_sec_unit_id_tenant_id",
                table: "StockAdjDetail",
                columns: new[] { "sec_unit_id", "tenant_id" },
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_item_transaction_units_sec_unit_id_tenant_id",
                table: "ItemTransaction");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_detail_units_sec_unit_id_tenant_id",
                table: "PurchaseDetail");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_ret_detail_units_sec_unit_id_tenant_id",
                table: "PurchaseRetDetail");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_ret_detail_units_sec_unit_id_tenant_id",
                table: "SaleRetDetail");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_units_sec_unit_id_tenant_id",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_supply_detail_units_sec_unit_id_tenant_id",
                table: "SaleSupplyDetail");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adj_detail_units_sec_unit_id_tenant_id",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_stock_adj_detail_sec_unit_id_tenant_id",
                table: "StockAdjDetail");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_detail_sec_unit_id_tenant_id",
                table: "SaleSupplyDetail");

            migrationBuilder.DropIndex(
                name: "ix_sales_sec_unit_id_tenant_id",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_ret_detail_sec_unit_id_tenant_id",
                table: "SaleRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_ret_detail_sec_unit_id_tenant_id",
                table: "PurchaseRetDetail");

            migrationBuilder.DropIndex(
                name: "ix_purchase_detail_sec_unit_id_tenant_id",
                table: "PurchaseDetail");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_sec_unit_id_tenant_id",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "qty_in_pack",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty_in",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty_out",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "qty_in_pack",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "sec_qty",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "sec_qty_in",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "sec_qty_out",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "sec_rate",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "sec_unit_id",
                table: "ItemTransaction");
        }
    }
}
