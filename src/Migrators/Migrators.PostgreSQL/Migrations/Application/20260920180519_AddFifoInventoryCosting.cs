using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddFifoInventoryCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_amount",
                schema: "public",
                table: "ItemTransaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_price",
                schema: "public",
                table: "ItemTransaction",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "remaining_qty",
                schema: "public",
                table: "ItemTransaction",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE \"public\".\"ItemTransaction\" SET remaining_qty = qty_in WHERE tran_type = 'in';");

            migrationBuilder.CreateTable(
                name: "transaction_fifo_mapping",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    out_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    in_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    qty_consumed = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "text", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_transaction_fifo_mapping_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_transaction_fifo_mapping_item_transaction_in_transaction_id_",
                        columns: x => new { x.in_transaction_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemTransaction",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transaction_fifo_mapping_item_transaction_out_transaction_id",
                        columns: x => new { x.out_transaction_id, x.tenant_id },
                        principalSchema: "public",
                        principalTable: "ItemTransaction",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_transaction_item_id_tran_type_v_date_id",
                schema: "public",
                table: "ItemTransaction",
                columns: new[] { "item_id", "tran_type", "v_date", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_in_transaction_id",
                schema: "public",
                table: "transaction_fifo_mapping",
                column: "in_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_in_transaction_id_tenant_id",
                schema: "public",
                table: "transaction_fifo_mapping",
                columns: new[] { "in_transaction_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_out_transaction_id",
                schema: "public",
                table: "transaction_fifo_mapping",
                column: "out_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_fifo_mapping_out_transaction_id_tenant_id",
                schema: "public",
                table: "transaction_fifo_mapping",
                columns: new[] { "out_transaction_id", "tenant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_fifo_mapping",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_item_transaction_item_id_tran_type_v_date_id",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "cost_amount",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "cost_price",
                schema: "public",
                table: "ItemTransaction");

            migrationBuilder.DropColumn(
                name: "remaining_qty",
                schema: "public",
                table: "ItemTransaction");
        }
    }
}
