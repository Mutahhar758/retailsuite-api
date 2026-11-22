using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPosTerminals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosTerminals",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    machine_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    terminal_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    machine_name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    last_seen_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_pos_terminals_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_machine_id",
                table: "PosTerminals",
                columns: new[] { "machine_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_terminal_code",
                table: "PosTerminals",
                columns: new[] { "terminal_code", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosTerminals");
        }
    }
}
