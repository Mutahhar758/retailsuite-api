using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddManufacturingLinkedPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "create_raw_material_purchase",
                schema: "public",
                table: "ProductionMaster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "linked_purchase_voucher_no",
                schema: "public",
                table: "ProductionMaster",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_scope",
                schema: "public",
                table: "ProductionMaster",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_supplier_account_id",
                schema: "public",
                table: "ProductionMaster",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "create_raw_material_purchase",
                schema: "public",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "linked_purchase_voucher_no",
                schema: "public",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "purchase_scope",
                schema: "public",
                table: "ProductionMaster");

            migrationBuilder.DropColumn(
                name: "purchase_supplier_account_id",
                schema: "public",
                table: "ProductionMaster");
        }
    }
}
