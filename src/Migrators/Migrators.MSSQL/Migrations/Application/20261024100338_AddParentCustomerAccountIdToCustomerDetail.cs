using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddParentCustomerAccountIdToCustomerDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parent_customer_account_id",
                table: "CustomerDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_parent_customer_account_id_tenant_id",
                table: "CustomerDetail",
                columns: new[] { "parent_customer_account_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_detail_chart_of_account_parent_customer_account_id_tenant_id",
                table: "CustomerDetail",
                columns: new[] { "parent_customer_account_id", "tenant_id" },
                principalTable: "ChartOfAccount",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_detail_chart_of_account_parent_customer_account_id_tenant_id",
                table: "CustomerDetail");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_parent_customer_account_id_tenant_id",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "parent_customer_account_id",
                table: "CustomerDetail");
        }
    }
}
