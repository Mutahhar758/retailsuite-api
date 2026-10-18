using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddCustomerCategoryAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_category_id",
                schema: "public",
                table: "CustomerDetail",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerCategory",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    active = table.Column<decimal>(type: "numeric(1,0)", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_customer_category_id_tenant_id", x => new { x.id, x.tenant_id });
                });

            migrationBuilder.CreateTable(
                name: "ItemCustomerCategoryPrice",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<string>(type: "text", nullable: false),
                    customer_category_id = table.Column<string>(type: "text", nullable: false),
                    pri_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sec_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_item_customer_category_price_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_item_customer_category_price_customer_category_customer_categor",
                        columns: x => new { x.customer_category_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "CustomerCategory",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_customer_category_price_item_detail_item_id_tenant_id",
                        columns: x => new { x.item_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemDetail",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_detail_customer_category_id_tenant_id",
                schema: "public",
                table: "CustomerDetail",
                columns: new[] { "customer_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_customer_category_id_tenant_id",
                schema: "public",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "customer_category_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_item_id_customer_category_id",
                schema: "public",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "item_id", "customer_category_id", "tenant_id" },
                unique: true,
                filter: "deleted_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_item_customer_category_price_item_id_tenant_id",
                schema: "public",
                table: "ItemCustomerCategoryPrice",
                columns: new[] { "item_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_detail_customer_category_customer_category_id_tenant",
                schema: "public",
                table: "CustomerDetail",
                columns: new[] { "customer_category_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "CustomerCategory",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_detail_customer_category_customer_category_id_tenant",
                schema: "public",
                table: "CustomerDetail");

            migrationBuilder.DropTable(
                name: "ItemCustomerCategoryPrice",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CustomerCategory",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_customer_detail_customer_category_id_tenant_id",
                schema: "public",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "customer_category_id",
                schema: "public",
                table: "CustomerDetail");
        }
    }
}
