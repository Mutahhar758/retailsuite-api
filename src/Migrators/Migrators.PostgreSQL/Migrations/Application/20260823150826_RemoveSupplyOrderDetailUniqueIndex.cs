using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemoveSupplyOrderDetailUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_supply_order_detail_supply_order_master_id_customer_account_i",
                schema: "public",
                table: "SupplyOrderDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_supply_order_detail_supply_order_master_id_customer_account_i",
                schema: "public",
                table: "SupplyOrderDetail",
                columns: new[] { "supply_order_master_id", "customer_account_id", "sort_order", "tenant_id" },
                unique: true);
        }
    }
}
