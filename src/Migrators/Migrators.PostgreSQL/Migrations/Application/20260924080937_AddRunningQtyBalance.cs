using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddRunningQtyBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "running_qty_balance",
                schema: "public",
                table: "ItemTransaction",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_item_id_v_date_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "item_id", "v_date", "id" });

            migrationBuilder.Sql(@"
WITH cte AS (
    SELECT id, tenant_id, item_id,
           SUM(qty_in - qty_out) OVER (
               PARTITION BY tenant_id, item_id 
               ORDER BY v_date, COALESCE(v_time, '00:00:00'::time), 
                        CASE WHEN tran_type = 'in' THEN 0 ELSE 1 END, 
                        id
               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
           ) AS calculated_running_balance
    FROM ""public"".""ItemTransaction""
    WHERE deleted_on IS NULL AND item_id IS NOT NULL
)
UPDATE ""public"".""ItemTransaction"" it
SET running_qty_balance = cte.calculated_running_balance
FROM cte
WHERE it.id = cte.id AND it.tenant_id = cte.tenant_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_item_transaction_item_id_v_date_id",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "running_qty_balance",
                schema: "public",
                table: "ItemTransaction");
        }
    }
}
