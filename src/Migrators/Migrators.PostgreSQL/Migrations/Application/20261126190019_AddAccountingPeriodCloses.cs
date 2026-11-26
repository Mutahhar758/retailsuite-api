using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddAccountingPeriodCloses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingPeriodCloses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    period_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    closing_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    closing_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_equity_account_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    closed_drawings = table.Column<bool>(type: "boolean", nullable: false),
                    opening_stock_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_stock_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_income = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_cogs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_expenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_profit_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_assets = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_liabilities = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_equity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_balanced = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reopened_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reopened_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    reopen_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_accounting_period_closes_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_period_closes_status_closing_date",
                schema: "public",
                table: "AccountingPeriodCloses",
                columns: new[] { "status", "closing_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriodCloses",
                schema: "public");
        }
    }
}
