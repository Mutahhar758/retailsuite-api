using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddProfileToSaleSupplyMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supply_order_master_id",
                table: "SaleSupplyMaster",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_supply_order_master_id_tenant_id",
                table: "SaleSupplyMaster",
                columns: new[] { "supply_order_master_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_sale_supply_master_supply_order_master_supply_order_master_id_tenant_id",
                table: "SaleSupplyMaster",
                columns: new[] { "supply_order_master_id", "tenant_id" },
                principalTable: "SupplyOrderMaster",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_supply_master_supply_order_master_supply_order_master_id_tenant_id",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_supply_order_master_id_tenant_id",
                table: "SaleSupplyMaster");

            migrationBuilder.DropColumn(
                name: "supply_order_master_id",
                table: "SaleSupplyMaster");
        }
    }
}
