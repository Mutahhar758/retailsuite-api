using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class UpdateSaleSupplyAttachmentToMediaStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_supply_attachments_documents_document_id_tenant_id",
                schema: "public",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_attachments_document_id_tenant_id",
                schema: "public",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropColumn(
                name: "document_id",
                schema: "public",
                table: "SaleSupplyAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "public",
                table: "SaleSupplyAttachments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "public",
                table: "SaleSupplyAttachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "media_id",
                schema: "public",
                table: "SaleSupplyAttachments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "public",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropColumn(
                name: "media_id",
                schema: "public",
                table: "SaleSupplyAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "public",
                table: "SaleSupplyAttachments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "document_id",
                schema: "public",
                table: "SaleSupplyAttachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_attachments_document_id_tenant_id",
                schema: "public",
                table: "SaleSupplyAttachments",
                columns: new[] { "document_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_sale_supply_attachments_documents_document_id_tenant_id",
                schema: "public",
                table: "SaleSupplyAttachments",
                columns: new[] { "document_id", "tenant_id" },
                principalSchema: "public",
                principalTable: "documents",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
