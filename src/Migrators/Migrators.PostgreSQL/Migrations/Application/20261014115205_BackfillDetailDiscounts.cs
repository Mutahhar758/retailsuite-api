using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class BackfillDetailDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.""Sales""
SET discount = ROUND(COALESCE(discount, 0) * COALESCE(qty, 0), 2)
WHERE COALESCE(discount, 0) != 0;

UPDATE public.""SaleSupplyDetail""
SET discount = ROUND(COALESCE(discount, 0) * COALESCE(qty, 0), 2)
WHERE COALESCE(discount, 0) != 0;

UPDATE public.""SaleRetDetail""
SET discount = ROUND(COALESCE(discount, 0) * COALESCE(qty, 0), 2)
WHERE COALESCE(discount, 0) != 0;

UPDATE public.""CustomerSupplyItem""
SET discount = ROUND(COALESCE(discount, 0) * COALESCE(qty, 0), 2)
WHERE COALESCE(discount, 0) != 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.""Sales""
SET discount = ROUND(COALESCE(discount, 0) / NULLIF(COALESCE(qty, 0), 0), 2)
WHERE COALESCE(discount, 0) != 0 AND COALESCE(qty, 0) != 0;

UPDATE public.""SaleSupplyDetail""
SET discount = ROUND(COALESCE(discount, 0) / NULLIF(COALESCE(qty, 0), 0), 2)
WHERE COALESCE(discount, 0) != 0 AND COALESCE(qty, 0) != 0;

UPDATE public.""SaleRetDetail""
SET discount = ROUND(COALESCE(discount, 0) / NULLIF(COALESCE(qty, 0), 0), 2)
WHERE COALESCE(discount, 0) != 0 AND COALESCE(qty, 0) != 0;

UPDATE public.""CustomerSupplyItem""
SET discount = ROUND(COALESCE(discount, 0) / NULLIF(COALESCE(qty, 0), 0), 2)
WHERE COALESCE(discount, 0) != 0 AND COALESCE(qty, 0) != 0;
");
        }
    }
}
