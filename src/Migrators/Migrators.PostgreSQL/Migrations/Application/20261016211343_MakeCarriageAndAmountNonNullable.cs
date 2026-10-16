using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakeCarriageAndAmountNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill NULLs in SaleSupplyDetail
            migrationBuilder.Sql(@"
                UPDATE public.""SaleSupplyDetail""
                SET gross_rate = COALESCE(gross_rate, 0),
                    discount = COALESCE(discount, 0),
                    add_less = COALESCE(add_less, 0),
                    carriage = COALESCE(carriage, 0)
                WHERE gross_rate IS NULL OR discount IS NULL OR add_less IS NULL OR carriage IS NULL;

                UPDATE public.""SaleSupplyDetail""
                SET amount = ROUND((COALESCE(qty, 0) * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0) + COALESCE(add_less, 0), 2)
                WHERE amount IS NULL OR amount = 0;
            ");

            // Backfill NULLs in Sales
            migrationBuilder.Sql(@"
                UPDATE public.""Sales""
                SET gross_rate = COALESCE(gross_rate, 0),
                    discount = COALESCE(discount, 0),
                    carriage = COALESCE(carriage, 0)
                WHERE gross_rate IS NULL OR discount IS NULL OR carriage IS NULL;

                UPDATE public.""Sales""
                SET amount = ROUND((COALESCE(qty, 0) * (COALESCE(gross_rate, 0) - COALESCE(discount, 0))) + COALESCE(carriage, 0), 2)
                WHERE amount IS NULL OR amount = 0;
            ");

            migrationBuilder.AlterColumn<decimal>(
                name: "gross_rate",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "add_less",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "gross_rate",
                schema: "public",
                table: "Sales",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "gross_rate",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "add_less",
                schema: "public",
                table: "SaleSupplyDetail",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "gross_rate",
                schema: "public",
                table: "Sales",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "Sales",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "carriage",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "Sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);
        }
    }
}
