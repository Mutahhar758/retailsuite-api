using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPackingToTransactionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "StockAdjDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "SaleSupplyDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "SaleRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "PurchaseRetDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packing",
                table: "PurchaseDetail",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "packing",
                table: "StockAdjDetail");

            migrationBuilder.DropColumn(
                name: "packing",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "packing",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "packing",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "packing",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "packing",
                table: "PurchaseDetail");
        }
    }
}
