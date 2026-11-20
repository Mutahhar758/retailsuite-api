using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddDynamicRecurringChainSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "days_of_week",
                table: "RecurringChains",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "interval",
                table: "RecurringChains",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                table: "RecurringChains",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_of_week",
                table: "RecurringChains");

            migrationBuilder.DropColumn(
                name: "interval",
                table: "RecurringChains");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "RecurringChains");
        }
    }
}
