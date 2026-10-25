using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTokensToGeminiApiLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "input_tokens",
                table: "GeminiApiLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "output_tokens",
                table: "GeminiApiLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_tokens",
                table: "GeminiApiLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "input_tokens",
                table: "GeminiApiLogs");

            migrationBuilder.DropColumn(
                name: "output_tokens",
                table: "GeminiApiLogs");

            migrationBuilder.DropColumn(
                name: "total_tokens",
                table: "GeminiApiLogs");
        }
    }
}
