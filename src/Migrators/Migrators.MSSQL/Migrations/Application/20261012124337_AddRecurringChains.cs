using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddRecurringChains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "recurring_chain_id",
                table: "SaleSupplyMaster",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recurring_chain_id",
                table: "GL1",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringChains",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    voucher_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: true),
                    day_of_month = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_run_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_recurring_chains_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_recurring_chain_id",
                table: "SaleSupplyMaster",
                column: "recurring_chain_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl1_recurring_chain_id",
                table: "GL1",
                column: "recurring_chain_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringChains");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_recurring_chain_id",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_gl1_recurring_chain_id",
                table: "GL1");

            migrationBuilder.DropColumn(
                name: "recurring_chain_id",
                table: "SaleSupplyMaster");

            migrationBuilder.DropColumn(
                name: "recurring_chain_id",
                table: "GL1");
        }
    }
}
