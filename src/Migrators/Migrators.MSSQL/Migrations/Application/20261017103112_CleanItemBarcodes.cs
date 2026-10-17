using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class CleanItemBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [ItemDetail]
SET [barcode] = LTRIM(RTRIM(REPLACE(REPLACE([barcode], CHAR(13), ''), CHAR(10), '')))
WHERE [barcode] IS NOT NULL 
  AND ([barcode] LIKE '%' + CHAR(13) + '%' 
    OR [barcode] LIKE '%' + CHAR(10) + '%' 
    OR [barcode] LIKE ' %' 
    OR [barcode] LIKE '% ');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
