using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddBankDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankDetail",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    account_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    account_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    iban = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    branch_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    branch_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    swift_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_bank_detail_id_tenant_id", x => new { x.id, x.tenant_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankDetail");
        }
    }
}
