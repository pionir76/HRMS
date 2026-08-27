using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class RunningStatusToBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RunningStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RunningStatus",
                table: "CompressorMeasurements");

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "Equipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "CompressorMeasurements",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "CompressorMeasurements");

            migrationBuilder.AddColumn<int>(
                name: "RunningStatus",
                table: "Equipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RunningStatus",
                table: "CompressorMeasurements",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
