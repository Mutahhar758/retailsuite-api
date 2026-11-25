using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddManufacturingLinkedPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "create_raw_material_purchase",
                table: "ProductionMaster",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "linked_purchase_voucher_no",
                table: "ProductionMaster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_scope",
                table: "ProductionMaster",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_supplier_account_id",
                table: "ProductionMaster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "create_raw_material_purchase",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "linked_purchase_voucher_no",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "purchase_scope",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "purchase_supplier_account_id",
                table: "ProductionMaster");
        }
    }
}
