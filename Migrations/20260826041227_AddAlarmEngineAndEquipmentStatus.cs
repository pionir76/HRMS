using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmEngineAndEquipmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlarmStatus",
                table: "Equipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationStatus",
                table: "Equipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RunningStatus",
                table: "Equipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AlarmStatus",
                table: "CompressorSensorCurrents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingSince",
                table: "CompressorSensorCurrents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "CompressorChannelSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CommunicationStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RunningStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "AlarmStatus",
                table: "CompressorSensorCurrents");

            migrationBuilder.DropColumn(
                name: "PendingSince",
                table: "CompressorSensorCurrents");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "CompressorChannelSettings");
        }
    }
}
