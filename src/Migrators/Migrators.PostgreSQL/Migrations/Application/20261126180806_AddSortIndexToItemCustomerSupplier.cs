using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddSortIndexToItemCustomerSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                schema: "public",
                table: "SupplierDetail",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                schema: "public",
                table: "ItemDetail",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sort_index",
                schema: "public",
                table: "CustomerDetail",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_detail_sort_index",
                schema: "public",
                table: "SupplierDetail",
                column: "sort_index");

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_sort_index",
                schema: "public",
                table: "ItemDetail",
                column: "sort_index");

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_sort_index",
                schema: "public",
                table: "CustomerDetail",
                column: "sort_index");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_supplier_detail_sort_index",
                schema: "public",
                table: "SupplierDetail");

            migrationBuilder.DropIndex(
                name: "ix_item_detail_sort_index",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_sort_index",
                schema: "public",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                schema: "public",
                table: "SupplierDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "sort_index",
                schema: "public",
                table: "CustomerDetail");
        }
    }
}
