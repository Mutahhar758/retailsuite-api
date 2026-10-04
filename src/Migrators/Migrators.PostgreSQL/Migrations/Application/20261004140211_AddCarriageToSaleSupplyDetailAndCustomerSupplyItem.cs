using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddCarriageToSaleSupplyDetailAndCustomerSupplyItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "CustomerSupplyItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "carriage",
                schema: "public",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "carriage",
                schema: "public",
                table: "CustomerSupplyItem");
        }
    }
}
