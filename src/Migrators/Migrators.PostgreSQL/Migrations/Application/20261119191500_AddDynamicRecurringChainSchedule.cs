using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddDynamicRecurringChainSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "days_of_week",
                schema: "public",
                table: "RecurringChains",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "interval",
                schema: "public",
                table: "RecurringChains",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                schema: "public",
                table: "RecurringChains",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_of_week",
                schema: "public",
                table: "RecurringChains");

            migrationBuilder.DropColumn(
                name: "interval",
                schema: "public",
                table: "RecurringChains");

            migrationBuilder.DropColumn(
                name: "start_date",
                schema: "public",
                table: "RecurringChains");
        }
    }
}
