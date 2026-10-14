using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddAmountToDetailTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "recurring_chain_id",
                schema: "public",
                table: "SaleSupplyMaster",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "SaleRetDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "PurchaseRetDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "PurchaseDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recurring_chain_id",
                schema: "public",
                table: "GL1",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringChains",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    voucher_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_run_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_voucher_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_recurring_chains_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_master_recurring_chain_id",
                schema: "public",
                table: "SaleSupplyMaster",
                column: "recurring_chain_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl1_recurring_chain_id",
                schema: "public",
                table: "GL1",
                column: "recurring_chain_id");

            migrationBuilder.Sql(@"
UPDATE public.""Sales""
SET amount = CASE 
    WHEN COALESCE(packing, 0) > 0 THEN 
        (qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0)
    ELSE 
        (qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0) + (COALESCE(sec_qty, 0) * COALESCE(sec_rate, 0))
END
WHERE amount IS NULL;

UPDATE public.""SaleSupplyDetail""
SET amount = CASE 
    WHEN COALESCE(packing, 0) > 0 THEN 
        (qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0) + COALESCE(add_less, 0)
    ELSE 
        (qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0) + COALESCE(add_less, 0) + (COALESCE(sec_qty, 0) * COALESCE(sec_rate, 0))
END
WHERE amount IS NULL;

UPDATE public.""SaleRetDetail""
SET amount = CASE 
    WHEN COALESCE(packing, 0) > 0 THEN 
        qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))
    ELSE 
        (qty * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + (COALESCE(sec_qty, 0) * COALESCE(sec_rate, 0))
END
WHERE amount IS NULL;

UPDATE public.""PurchaseDetail""
SET amount = CASE 
    WHEN COALESCE(packing, 0) > 0 THEN 
        (qty * rate) + COALESCE(add_less, 0)
    ELSE 
        (qty * rate) + COALESCE(add_less, 0) + (COALESCE(sec_qty, 0) * COALESCE(sec_rate, 0))
END
WHERE amount IS NULL;

UPDATE public.""PurchaseRetDetail""
SET amount = CASE 
    WHEN COALESCE(packing, 0) > 0 THEN 
        qty * rate
    ELSE 
        (qty * rate) + (COALESCE(sec_qty, 0) * COALESCE(sec_rate, 0))
END
WHERE amount IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringChains",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_master_recurring_chain_id",
                schema: "public",
                table: "SaleSupplyMaster");

            migrationBuilder.DropIndex(
                name: "ix_gl1_recurring_chain_id",
                schema: "public",
                table: "GL1");

            migrationBuilder.DropColumn(
                name: "recurring_chain_id",
                schema: "public",
                table: "SaleSupplyMaster");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "PurchaseDetail");

            migrationBuilder.DropColumn(
                name: "recurring_chain_id",
                schema: "public",
                table: "GL1");
        }
    }
}
