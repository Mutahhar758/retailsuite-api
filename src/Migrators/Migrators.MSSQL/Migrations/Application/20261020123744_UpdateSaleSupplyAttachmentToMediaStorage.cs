using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class UpdateSaleSupplyAttachmentToMediaStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_supply_attachments_documents_document_id_tenant_id",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropIndex(
                name: "ix_sale_supply_attachments_document_id_tenant_id",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropColumn(
                name: "document_id",
                table: "SaleSupplyAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "SaleSupplyAttachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "SaleSupplyAttachments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "media_id",
                table: "SaleSupplyAttachments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_name",
                table: "SaleSupplyAttachments");

            migrationBuilder.DropColumn(
                name: "media_id",
                table: "SaleSupplyAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "SaleSupplyAttachments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "document_id",
                table: "SaleSupplyAttachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_sale_supply_attachments_document_id_tenant_id",
                table: "SaleSupplyAttachments",
                columns: new[] { "document_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_sale_supply_attachments_documents_document_id_tenant_id",
                table: "SaleSupplyAttachments",
                columns: new[] { "document_id", "tenant_id" },
                principalTable: "documents",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
