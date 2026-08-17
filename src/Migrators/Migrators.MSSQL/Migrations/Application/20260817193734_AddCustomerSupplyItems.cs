using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddCustomerSupplyItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSupplyItem",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    customer_account_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    sec_qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_supply_item_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_customer_supply_item_chart_of_account_customer_account_id_tenant_id",
                        columns: x => new { x.customer_account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_supply_item_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_item_id",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "item_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_customer_account_id_tenant_id",
                table: "CustomerSupplyItem",
                columns: new[] { "customer_account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_supply_item_item_id_tenant_id",
                table: "CustomerSupplyItem",
                columns: new[] { "item_id", "tenant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSupplyItem");
        }
    }
}
