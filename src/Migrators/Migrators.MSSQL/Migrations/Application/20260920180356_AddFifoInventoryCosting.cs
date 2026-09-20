using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddFifoInventoryCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "tran_type",
                table: "ItemTransaction",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "cost_amount",
                table: "ItemTransaction",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_price",
                table: "ItemTransaction",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "remaining_qty",
                table: "ItemTransaction",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE [ItemTransaction] SET [remaining_qty] = [qty_in] WHERE [tran_type] = 'in';");

            migrationBuilder.CreateTable(
                name: "transaction_fifo_mapping",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    out_transaction_id = table.Column<int>(type: "int", nullable: false),
                    in_transaction_id = table.Column<int>(type: "int", nullable: false),
                    qty_consumed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_transaction_fifo_mapping_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_transaction_fifo_mapping_item_transaction_in_transaction_id_tenant_id",
                        columns: x => new { x.in_transaction_id, x.tenant_id },
                        principalTable: "ItemTransaction",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transaction_fifo_mapping_item_transaction_out_transaction_id_tenant_id",
                        columns: x => new { x.out_transaction_id, x.tenant_id },
                        principalTable: "ItemTransaction",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_item_id_tran_type_v_date_id",
                table: "ItemTransaction",
                columns: new[] { "item_id", "tran_type", "v_date", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_in_transaction_id",
                table: "transaction_fifo_mapping",
                column: "in_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_in_transaction_id_tenant_id",
                table: "transaction_fifo_mapping",
                columns: new[] { "in_transaction_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_out_transaction_id",
                table: "transaction_fifo_mapping",
                column: "out_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_out_transaction_id_tenant_id",
                table: "transaction_fifo_mapping",
                columns: new[] { "out_transaction_id", "tenant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_fifo_mapping");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_item_id_tran_type_v_date_id",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "cost_amount",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "cost_price",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "remaining_qty",
                table: "ItemTransaction");

            migrationBuilder.AlterColumn<string>(
                name: "tran_type",
                table: "ItemTransaction",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
