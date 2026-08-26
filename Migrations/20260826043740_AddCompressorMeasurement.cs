using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCompressorMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompressorMeasurements",
                columns: table => new
                {
                    CompressorId = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ch01 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch02 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch03 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch04 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch05 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch06 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ch07 = table.Column<decimal>(type: "numeric", nullable: true),
                    RunningStatus = table.Column<int>(type: "integer", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false),
                    CommunicationStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressorMeasurements", x => new { x.CompressorId, x.MeasuredAt });
                    table.ForeignKey(
                        name: "FK_CompressorMeasurements_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompressorMeasurements");
        }
    }
}
