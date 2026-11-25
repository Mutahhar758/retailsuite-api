using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddManufacturingExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BomExpense",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    bom_master_id = table.Column<int>(type: "integer", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    calculation_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_bom_expense_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_bom_expense_bom_master_bom_master_id_tenant_id",
                        columns: x => new { x.bom_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "BomMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bom_expense_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionExpense",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    production_master_id = table.Column<int>(type: "integer", nullable: false),
                    v_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    v_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_id = table.Column<string>(type: "text", nullable: true),
                    calculation_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    planned_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_production_expense_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_production_expense_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_expense_production_master_production_master_id_ten",
                        columns: x => new { x.production_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ProductionMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bom_expense_account_id_tenant_id",
                schema: "public",
                table: "BomExpense",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_expense_bom_master_id_tenant_id",
                schema: "public",
                table: "BomExpense",
                columns: new[] { "bom_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_account_id_tenant_id",
                schema: "public",
                table: "ProductionExpense",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_production_master_id_tenant_id",
                schema: "public",
                table: "ProductionExpense",
                columns: new[] { "production_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_v_type_v_no_seq",
                schema: "public",
                table: "ProductionExpense",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomExpense",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ProductionExpense",
                schema: "public");
        }
    }
}
