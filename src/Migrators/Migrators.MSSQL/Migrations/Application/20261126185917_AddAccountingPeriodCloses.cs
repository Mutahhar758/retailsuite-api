using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddAccountingPeriodCloses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingPeriodCloses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    period_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    closing_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    closing_voucher_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    target_equity_account_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    closed_drawings = table.Column<bool>(type: "bit", nullable: false),
                    opening_stock_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_stock_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_income = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_cogs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_expenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_profit_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_assets = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_liabilities = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_equity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_balanced = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    reopened_by = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    reopened_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reopen_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_accounting_period_closes_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_period_closes_status_closing_date",
                table: "AccountingPeriodCloses",
                columns: new[] { "status", "closing_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriodCloses");
        }
    }
}
