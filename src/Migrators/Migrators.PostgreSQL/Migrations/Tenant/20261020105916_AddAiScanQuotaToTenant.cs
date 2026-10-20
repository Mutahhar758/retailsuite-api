using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddAiScanQuotaToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_scan_current_month",
                table: "Tenants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ai_scan_monthly_limit",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "ai_scan_used_this_month",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_scan_current_month",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ai_scan_monthly_limit",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ai_scan_used_this_month",
                table: "Tenants");
        }
    }
}
