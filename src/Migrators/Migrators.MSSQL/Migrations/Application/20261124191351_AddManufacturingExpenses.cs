using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddManufacturingExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BomExpense",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    bom_master_id = table.Column<int>(type: "int", nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    calculation_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_bom_expense_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_bom_expense_bom_master_bom_master_id_tenant_id",
                        columns: x => new { x.bom_master_id, x.tenant_id },
                        principalTable: "BomMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bom_expense_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionExpense",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    production_master_id = table.Column<int>(type: "int", nullable: false),
                    v_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    v_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    seq = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    account_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    calculation_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    planned_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_production_expense_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_production_expense_chart_of_account_account_id_tenant_id",
                        columns: x => new { x.account_id, x.tenant_id },
                        principalTable: "ChartOfAccount",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_expense_production_master_production_master_id_tenant_id",
                        columns: x => new { x.production_master_id, x.tenant_id },
                        principalTable: "ProductionMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bom_expense_account_id_tenant_id",
                table: "BomExpense",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_expense_bom_master_id_tenant_id",
                table: "BomExpense",
                columns: new[] { "bom_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_account_id_tenant_id",
                table: "ProductionExpense",
                columns: new[] { "account_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_production_master_id_tenant_id",
                table: "ProductionExpense",
                columns: new[] { "production_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_expense_v_type_v_no_seq",
                table: "ProductionExpense",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomExpense");

            migrationBuilder.DropTable(
                name: "ProductionExpense");
        }
    }
}
