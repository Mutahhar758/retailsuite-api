using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddParentCustomerAccountIdToCustomerDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parent_customer_account_id",
                schema: "public",
                table: "CustomerDetail",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_parent_customer_account_id_tenant_id",
                schema: "public",
                table: "CustomerDetail",
                columns: new[] { "parent_customer_account_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_detail_chart_of_account_parent_customer_account_id_te",
                schema: "public",
                table: "CustomerDetail",
                columns: new[] { "parent_customer_account_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "ChartOfAccount",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_detail_chart_of_account_parent_customer_account_id_te",
                schema: "public",
                table: "CustomerDetail");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_parent_customer_account_id_tenant_id",
                schema: "public",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "parent_customer_account_id",
                schema: "public",
                table: "CustomerDetail");
        }
    }
}
