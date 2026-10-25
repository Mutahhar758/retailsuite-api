using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddGeminiApiLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeminiApiLogs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    turn_index = table.Column<int>(type: "integer", nullable: false),
                    model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    prompt = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    current_route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    request_body = table.Column<string>(type: "text", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    tool_calls = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gemini_api_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gemini_api_logs_correlation_id",
                table: "GeminiApiLogs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_gemini_api_logs_created_at",
                table: "GeminiApiLogs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_gemini_api_logs_tenant_id",
                table: "GeminiApiLogs",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeminiApiLogs");
        }
    }
}
