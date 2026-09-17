using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmDetailToEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "EventLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "LowerLimit",
                table: "EventLogs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "EventLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "UpperLimit",
                table: "EventLogs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Value",
                table: "EventLogs",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "LowerLimit",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "UpperLimit",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "EventLogs");
        }
    }
}
