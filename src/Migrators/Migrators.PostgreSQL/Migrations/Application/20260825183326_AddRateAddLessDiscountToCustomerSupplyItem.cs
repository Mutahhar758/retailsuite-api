using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddRateAddLessDiscountToCustomerSupplyItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "add_less",
                schema: "public",
                table: "CustomerSupplyItem",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "CustomerSupplyItem",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate",
                schema: "public",
                table: "CustomerSupplyItem",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "add_less",
                schema: "public",
                table: "CustomerSupplyItem");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "public",
                table: "CustomerSupplyItem");

            migrationBuilder.DropColumn(
                name: "rate",
                schema: "public",
                table: "CustomerSupplyItem");
        }
    }
}
