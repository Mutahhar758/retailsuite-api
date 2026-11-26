using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddSortIndexToItemCustomerSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                table: "SupplierDetail",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                table: "ItemDetail",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                table: "CustomerDetail",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_detail_sort_index",
                table: "SupplierDetail",
                column: "sort_index");

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_sort_index",
                table: "ItemDetail",
                column: "sort_index");

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_sort_index",
                table: "CustomerDetail",
                column: "sort_index");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_supplier_detail_sort_index",
                table: "SupplierDetail");

            migrationBuilder.DropIndex(
                name: "ix_item_detail_sort_index",
                table: "ItemDetail");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_sort_index",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                table: "SupplierDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                table: "CustomerDetail");
        }
    }
}
