using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddChatIdToGeminiApiLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chat_id",
                table: "GeminiApiLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_gemini_api_logs_chat_id",
                table: "GeminiApiLogs",
                column: "chat_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gemini_api_logs_chat_id",
                table: "GeminiApiLogs");

            migrationBuilder.DropColumn(
                name: "chat_id",
                table: "GeminiApiLogs");
        }
    }
}
