using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class BackfillDetailDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Sales]
SET [discount] = ROUND(ISNULL([discount], 0) * ISNULL([qty], 0), 2)
WHERE ISNULL([discount], 0) != 0;

UPDATE [SaleSupplyDetail]
SET [discount] = ROUND(ISNULL([discount], 0) * ISNULL([qty], 0), 2)
WHERE ISNULL([discount], 0) != 0;

UPDATE [SaleRetDetail]
SET [discount] = ROUND(ISNULL([discount], 0) * ISNULL([qty], 0), 2)
WHERE ISNULL([discount], 0) != 0;

UPDATE [CustomerSupplyItem]
SET [discount] = ROUND(ISNULL([discount], 0) * ISNULL([qty], 0), 2)
WHERE ISNULL([discount], 0) != 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Sales]
SET [discount] = ROUND(ISNULL([discount], 0) / NULLIF(ISNULL([qty], 0), 0), 2)
WHERE ISNULL([discount], 0) != 0 AND ISNULL([qty], 0) != 0;

UPDATE [SaleSupplyDetail]
SET [discount] = ROUND(ISNULL([discount], 0) / NULLIF(ISNULL([qty], 0), 0), 2)
WHERE ISNULL([discount], 0) != 0 AND ISNULL([qty], 0) != 0;

UPDATE [SaleRetDetail]
SET [discount] = ROUND(ISNULL([discount], 0) / NULLIF(ISNULL([qty], 0), 0), 2)
WHERE ISNULL([discount], 0) != 0 AND ISNULL([qty], 0) != 0;

UPDATE [CustomerSupplyItem]
SET [discount] = ROUND(ISNULL([discount], 0) / NULLIF(ISNULL([qty], 0), 0), 2)
WHERE ISNULL([discount], 0) != 0 AND ISNULL([qty], 0) != 0;
");
        }
    }
}
