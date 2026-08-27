using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class TrendStatusBinaryEquipmentLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmStatus",
                table: "CompressorMeasurements");

            migrationBuilder.DropColumn(
                name: "CommunicationStatus",
                table: "CompressorMeasurements");

            migrationBuilder.AddColumn<bool>(
                name: "HasAlarm",
                table: "CompressorMeasurements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsConnected",
                table: "CompressorMeasurements",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAlarm",
                table: "CompressorMeasurements");

            migrationBuilder.DropColumn(
                name: "IsConnected",
                table: "CompressorMeasurements");

            migrationBuilder.AddColumn<int>(
                name: "AlarmStatus",
                table: "CompressorMeasurements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationStatus",
                table: "CompressorMeasurements",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
