using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddAmountToDetailTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "SaleSupplyDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "SaleRetDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "PurchaseRetDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "PurchaseDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE [Sales]
SET [amount] = CASE 
    WHEN ISNULL([packing], 0) > 0 THEN 
        ([qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))) + ISNULL([carriage], 0)
    ELSE 
        ([qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))) + ISNULL([carriage], 0) + (ISNULL([sec_qty], 0) * ISNULL([sec_rate], 0))
END
WHERE [amount] IS NULL;

UPDATE [SaleSupplyDetail]
SET [amount] = CASE 
    WHEN ISNULL([packing], 0) > 0 THEN 
        ([qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))) + ISNULL([carriage], 0) + ISNULL([add_less], 0)
    ELSE 
        ([qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))) + ISNULL([carriage], 0) + ISNULL([add_less], 0) + (ISNULL([sec_qty], 0) * ISNULL([sec_rate], 0))
END
WHERE [amount] IS NULL;

UPDATE [SaleRetDetail]
SET [amount] = CASE 
    WHEN ISNULL([packing], 0) > 0 THEN 
        [qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))
    ELSE 
        ([qty] * (ISNULL([gross_rate], 0) - ISNULL([discount], 0))) + (ISNULL([sec_qty], 0) * ISNULL([sec_rate], 0))
END
WHERE [amount] IS NULL;

UPDATE [PurchaseDetail]
SET [amount] = CASE 
    WHEN ISNULL([packing], 0) > 0 THEN 
        ([qty] * [rate]) + ISNULL([add_less], 0)
    ELSE 
        ([qty] * [rate]) + ISNULL([add_less], 0) + (ISNULL([sec_qty], 0) * ISNULL([sec_rate], 0))
END
WHERE [amount] IS NULL;

UPDATE [PurchaseRetDetail]
SET [amount] = CASE 
    WHEN ISNULL([packing], 0) > 0 THEN 
        [qty] * [rate]
    ELSE 
        ([qty] * [rate]) + (ISNULL([sec_qty], 0) * ISNULL([sec_rate], 0))
END
WHERE [amount] IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount",
                table: "SaleSupplyDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "SaleRetDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "PurchaseRetDetail");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "PurchaseDetail");
        }
    }
}
