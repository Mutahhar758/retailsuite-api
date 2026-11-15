using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddItemFifoRebuildFromDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "fifo_rebuild_from_date",
                schema: "public",
                table: "ItemDetail",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_detail_fifo_rebuild_from_date",
                schema: "public",
                table: "ItemDetail",
                column: "fifo_rebuild_from_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_item_detail_fifo_rebuild_from_date",
                schema: "public",
                table: "ItemDetail");

            migrationBuilder.DropColumn(
                name: "fifo_rebuild_from_date",
                schema: "public",
                table: "ItemDetail");
        }
    }
}
