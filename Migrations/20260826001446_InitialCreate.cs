using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region = table.Column<string>(type: "text", nullable: false),
                    BuildingName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    RatedPower = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedVoltage = table.Column<decimal>(type: "numeric", nullable: true),
                    CompressorType = table.Column<string>(type: "text", nullable: true),
                    CompressorCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    CoolingTowerType = table.Column<string>(type: "text", nullable: true),
                    CoolingTowerCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    LegalRefrigerationCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    UsRefrigerationCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InstallDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PermitNumber = table.Column<string>(type: "text", nullable: true),
                    Refrigerant = table.Column<string>(type: "text", nullable: true),
                    ChargeAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    KgsManagementNumber = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    HighPressureTestPressure = table.Column<decimal>(type: "numeric", nullable: true),
                    LowPressureTestPressure = table.Column<decimal>(type: "numeric", nullable: true),
                    OverPressureCutoff = table.Column<decimal>(type: "numeric", nullable: true),
                    SafetyValveSetPointCondenser = table.Column<decimal>(type: "numeric", nullable: true),
                    SafetyValveSetPointEvaporator = table.Column<decimal>(type: "numeric", nullable: true),
                    RunningCurrentThreshold = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipments");
        }
    }
}
