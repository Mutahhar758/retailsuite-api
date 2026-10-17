using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class CleanItemBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.""ItemDetail""
SET barcode = TRIM(REPLACE(REPLACE(barcode, CHR(13), ''), CHR(10), ''))
WHERE barcode IS NOT NULL 
  AND (barcode LIKE '%' || CHR(13) || '%' 
    OR barcode LIKE '%' || CHR(10) || '%' 
    OR barcode LIKE ' %' 
    OR barcode LIKE '% ');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
