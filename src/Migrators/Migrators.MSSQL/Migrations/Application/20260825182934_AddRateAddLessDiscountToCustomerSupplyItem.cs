using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddRateAddLessDiscountToCustomerSupplyItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "add_less",
                table: "CustomerSupplyItem",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                table: "CustomerSupplyItem",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate",
                table: "CustomerSupplyItem",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "add_less",
                table: "CustomerSupplyItem");

            migrationBuilder.DropColumn(
                name: "discount",
                table: "CustomerSupplyItem");

            migrationBuilder.DropColumn(
                name: "rate",
                table: "CustomerSupplyItem");
        }
    }
}
