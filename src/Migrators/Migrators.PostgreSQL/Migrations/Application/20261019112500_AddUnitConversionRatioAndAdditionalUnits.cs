using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddUnitConversionRatioAndAdditionalUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "additional_units",
                schema: "public",
                table: "ItemDetail",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "conversion_ratio",
                schema: "public",
                table: "Units",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parent_unit_id",
                schema: "public",
                table: "Units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_title",
                schema: "public",
                table: "Units",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_units_parent_unit_id_tenant_id",
                schema: "public",
                table: "Units",
                columns: new[] { "parent_unit_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_units_units_parent_unit_id_tenant_id",
                schema: "public",
                table: "Units",
                columns: new[] { "parent_unit_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "Units",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_units_units_parent_unit_id_tenant_id",
                schema: "public",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "ix_units_parent_unit_id_tenant_id",
                schema: "public",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "additional_units",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "conversion_ratio",
                schema: "public",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "parent_unit_id",
                schema: "public",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "short_title",
                schema: "public",
                table: "Units");
        }
    }
}
