using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Opinion = table.Column<string>(type: "text", nullable: true),
                    Level1ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level1ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level1ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level2ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level2ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level2ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level3ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level3ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level3ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionResults",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemNo = table.Column<string>(type: "text", nullable: false),
                    Sun = table.Column<string>(type: "text", nullable: true),
                    Mon = table.Column<string>(type: "text", nullable: true),
                    Tue = table.Column<string>(type: "text", nullable: true),
                    Wed = table.Column<string>(type: "text", nullable: true),
                    Thu = table.Column<string>(type: "text", nullable: true),
                    Fri = table.Column<string>(type: "text", nullable: true),
                    Sat = table.Column<string>(type: "text", nullable: true),
                    Memo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionResults", x => new { x.LogId, x.ItemNo });
                    table.ForeignKey(
                        name: "FK_InspectionResults_InspectionLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "InspectionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionLogs_EquipmentId_WeekStartDate",
                table: "InspectionLogs",
                columns: new[] { "EquipmentId", "WeekStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionResults");

            migrationBuilder.DropTable(
                name: "InspectionLogs");
        }
    }
}
