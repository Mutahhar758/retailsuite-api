using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddManufacturingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BomMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    finished_item_id = table.Column<string>(type: "text", nullable: false),
                    output_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    labor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    overhead_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    packaging_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_bom_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_bom_master_item_detail_finished_item_id_tenant_id",
                        columns: x => new { x.finished_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bom_master_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomDetail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    bom_master_id = table.Column<int>(type: "integer", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    raw_item_id = table.Column<string>(type: "text", nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    scrap_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_bom_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_bom_detail_bom_master_bom_master_id_tenant_id",
                        columns: x => new { x.bom_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "BomMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bom_detail_item_detail_raw_item_id_tenant_id",
                        columns: x => new { x.raw_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bom_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionMaster",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    v_date = table.Column<DateOnly>(type: "date", nullable: false),
                    v_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    v_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    v_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bom_master_id = table.Column<int>(type: "integer", nullable: true),
                    finished_item_id = table.Column<string>(type: "text", nullable: false),
                    target_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    produced_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    batch_no = table.Column<string>(type: "text", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    total_material_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_overhead_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_production_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_production_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    narration_id = table.Column<string>(type: "text", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_stage = table.Column<string>(type: "text", nullable: true),
                    issued_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_production_master_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_production_master_bom_master_bom_master_id_tenant_id",
                        columns: x => new { x.bom_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "BomMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_master_item_detail_finished_item_id_tenant_id",
                        columns: x => new { x.finished_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_master_narration_narration_id_tenant_id",
                        columns: x => new { x.narration_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Narration",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_master_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionDetail",
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
                    raw_item_id = table.Column<string>(type: "text", nullable: false),
                    planned_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_id = table.Column<string>(type: "text", nullable: true),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_issued = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_production_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_production_detail_item_detail_raw_item_id_tenant_id",
                        columns: x => new { x.raw_item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_detail_production_master_production_master_id_tena",
                        columns: x => new { x.production_master_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ProductionMaster",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_detail_units_unit_id_tenant_id",
                        columns: x => new { x.unit_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "Units",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bom_detail_bom_master_id_tenant_id",
                schema: "public",
                table: "BomDetail",
                columns: new[] { "bom_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_detail_raw_item_id_tenant_id",
                schema: "public",
                table: "BomDetail",
                columns: new[] { "raw_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_detail_unit_id_tenant_id",
                schema: "public",
                table: "BomDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_master_finished_item_id_tenant_id",
                schema: "public",
                table: "BomMaster",
                columns: new[] { "finished_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bom_master_unit_id_tenant_id",
                schema: "public",
                table: "BomMaster",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_detail_production_master_id_tenant_id",
                schema: "public",
                table: "ProductionDetail",
                columns: new[] { "production_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_detail_raw_item_id_tenant_id",
                schema: "public",
                table: "ProductionDetail",
                columns: new[] { "raw_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_detail_unit_id_tenant_id",
                schema: "public",
                table: "ProductionDetail",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_detail_v_type_v_no_seq",
                schema: "public",
                table: "ProductionDetail",
                columns: new[] { "v_type", "v_no", "seq", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_production_master_bom_master_id_tenant_id",
                schema: "public",
                table: "ProductionMaster",
                columns: new[] { "bom_master_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_master_finished_item_id_tenant_id",
                schema: "public",
                table: "ProductionMaster",
                columns: new[] { "finished_item_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_master_narration_id_tenant_id",
                schema: "public",
                table: "ProductionMaster",
                columns: new[] { "narration_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_master_unit_id_tenant_id",
                schema: "public",
                table: "ProductionMaster",
                columns: new[] { "unit_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_production_master_v_type_v_no",
                schema: "public",
                table: "ProductionMaster",
                columns: new[] { "v_type", "v_no", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ProductionDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ProductionMaster",
                schema: "public");

            migrationBuilder.DropTable(
                name: "BomMaster",
                schema: "public");
        }
    }
}
