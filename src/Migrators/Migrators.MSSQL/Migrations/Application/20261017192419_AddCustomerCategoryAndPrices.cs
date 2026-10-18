using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddCustomerCategoryAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_category_id",
                table: "CustomerDetail",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerCategory",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_category_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ItemCustomerCategoryPrice",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    customer_category_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    pri_rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    sec_rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_customer_category_price_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_customer_category_price_customer_category_customer_category_id_tenant_id",
                        columns: x => new { x.customer_category_id, x.tenant_id },
                        principalTable: "CustomerCategory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_customer_category_price_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_customer_category_id_tenant_id",
                table: "CustomerDetail",
                columns: new[] { "customer_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_customer_category_id_tenant_id",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "customer_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_item_id_customer_category_id",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "item_id", "customer_category_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_item_id_tenant_id",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_detail_customer_category_customer_category_id_tenant_id",
                table: "CustomerDetail",
                columns: new[] { "customer_category_id", "tenant_id" },
                principalTable: "CustomerCategory",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_detail_customer_category_customer_category_id_tenant_id",
                table: "CustomerDetail");

            migrationBuilder.DropTable(
                name: "ItemCustomerCategoryPrice");

            migrationBuilder.DropTable(
                name: "CustomerCategory");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_customer_category_id_tenant_id",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "customer_category_id",
                table: "CustomerDetail");
        }
    }
}
