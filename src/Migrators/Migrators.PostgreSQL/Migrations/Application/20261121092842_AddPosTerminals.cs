using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPosTerminals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosTerminals",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    machine_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    terminal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    machine_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_seen_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_pos_terminals_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_machine_id",
                schema: "public",
                table: "PosTerminals",
                columns: new[] { "machine_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_terminal_code",
                schema: "public",
                table: "PosTerminals",
                columns: new[] { "terminal_code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosTerminals",
                schema: "public");
        }
    }
}
