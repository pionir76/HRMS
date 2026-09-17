using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class RedesignEquipmentAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallDate",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ManufactureDate",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "SafetyValveSetPointEvaporator",
                table: "Equipments",
                newName: "VoltageMin");

            migrationBuilder.RenameColumn(
                name: "SafetyValveSetPointCondenser",
                table: "Equipments",
                newName: "VoltageMax");

            migrationBuilder.RenameColumn(
                name: "RatedPower",
                table: "Equipments",
                newName: "SafetyValve");

            migrationBuilder.RenameColumn(
                name: "LowPressureTestPressure",
                table: "Equipments",
                newName: "RatedCurrent");

            migrationBuilder.RenameColumn(
                name: "KgsManagementNumber",
                table: "Equipments",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "HighPressureTestPressure",
                table: "Equipments",
                newName: "DesignPressure");

            migrationBuilder.AddColumn<decimal>(
                name: "BrineMax",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BrineMin",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingNumber",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompressorCount",
                table: "Equipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompressorManufacturer",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CondenserType",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoolingTowerCount",
                table: "Equipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoolingTowerManufacturer",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoolingWaterMax",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoolingWaterMin",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaporatorType",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBrine",
                table: "Equipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasCoolingWater",
                table: "Equipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVoltage",
                table: "Equipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Equipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagementNumber",
                table: "Equipments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrineMax",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "BrineMin",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "BuildingNumber",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CompressorCount",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CompressorManufacturer",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CondenserType",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CoolingTowerCount",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CoolingTowerManufacturer",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CoolingWaterMax",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CoolingWaterMin",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "EvaporatorType",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "HasBrine",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "HasCoolingWater",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "HasVoltage",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ManagementNumber",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "VoltageMin",
                table: "Equipments",
                newName: "SafetyValveSetPointEvaporator");

            migrationBuilder.RenameColumn(
                name: "VoltageMax",
                table: "Equipments",
                newName: "SafetyValveSetPointCondenser");

            migrationBuilder.RenameColumn(
                name: "SafetyValve",
                table: "Equipments",
                newName: "RatedPower");

            migrationBuilder.RenameColumn(
                name: "RatedCurrent",
                table: "Equipments",
                newName: "LowPressureTestPressure");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Equipments",
                newName: "KgsManagementNumber");

            migrationBuilder.RenameColumn(
                name: "DesignPressure",
                table: "Equipments",
                newName: "HighPressureTestPressure");

            migrationBuilder.AddColumn<DateOnly>(
                name: "InstallDate",
                table: "Equipments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ManufactureDate",
                table: "Equipments",
                type: "date",
                nullable: true);
        }
    }
}
