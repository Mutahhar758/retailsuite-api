using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddKotDiningTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prep_station_id",
                table: "ItemCatagory",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiningTables",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_dining_tables_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "PrepStations",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_prep_stations_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "KotOrders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    token_no = table.Column<int>(type: "int", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    order_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    table_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sale_voucher_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_kot_orders_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_kot_orders_dining_tables_table_id_tenant_id",
                        columns: x => new { x.table_id, x.tenant_id },
                        principalTable: "DiningTables",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KotOrderItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    kot_order_id = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_kot_order_items_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_kot_order_items_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kot_order_items_kot_orders_kot_order_id_tenant_id",
                        columns: x => new { x.kot_order_id, x.tenant_id },
                        principalTable: "KotOrders",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_catagory_prep_station_id_tenant_id",
                table: "ItemCatagory",
                columns: new[] { "prep_station_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kot_order_items_item_id_tenant_id",
                table: "KotOrderItems",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kot_order_items_kot_order_id_tenant_id",
                table: "KotOrderItems",
                columns: new[] { "kot_order_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_kot_orders_table_id_tenant_id",
                table: "KotOrders",
                columns: new[] { "table_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_item_catagory_prep_stations_prep_station_id_tenant_id",
                table: "ItemCatagory",
                columns: new[] { "prep_station_id", "tenant_id" },
                principalTable: "PrepStations",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_item_catagory_prep_stations_prep_station_id_tenant_id",
                table: "ItemCatagory");

            migrationBuilder.DropTable(
                name: "KotOrderItems");

            migrationBuilder.DropTable(
                name: "PrepStations");

            migrationBuilder.DropTable(
                name: "KotOrders");

            migrationBuilder.DropTable(
                name: "DiningTables");

            migrationBuilder.DropIndex(
                name: "ix_item_catagory_prep_station_id_tenant_id",
                table: "ItemCatagory");

            migrationBuilder.DropColumn(
                name: "prep_station_id",
                table: "ItemCatagory");
        }
    }
}
