using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTenantAiTokenUsages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantAiTokenUsages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_tokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    call_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_ai_token_usages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_ai_token_usages_tenant_id_model",
                table: "TenantAiTokenUsages",
                columns: new[] { "tenant_id", "model" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAiTokenUsages");
        }
    }
}
