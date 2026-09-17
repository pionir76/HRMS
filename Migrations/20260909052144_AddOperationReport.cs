using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationItemValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "text", nullable: false),
                    CompressorId = table.Column<int>(type: "integer", nullable: true),
                    Time0900 = table.Column<string>(type: "text", nullable: true),
                    Time1300 = table.Column<string>(type: "text", nullable: true),
                    Time1600 = table.Column<string>(type: "text", nullable: true),
                    Time2100 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationItemValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationItemValues_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperationItemValues_OperationLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "OperationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationReferenceValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "text", nullable: false),
                    ReferenceText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationReferenceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationReferenceValues_OperationLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "OperationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationItemValues_CompressorId",
                table: "OperationItemValues",
                column: "CompressorId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationItemValues_LogId_ItemKey_CompressorId",
                table: "OperationItemValues",
                columns: new[] { "LogId", "ItemKey", "CompressorId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_EquipmentId_Date",
                table: "OperationLogs",
                columns: new[] { "EquipmentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationReferenceValues_LogId_ItemKey",
                table: "OperationReferenceValues",
                columns: new[] { "LogId", "ItemKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationItemValues");

            migrationBuilder.DropTable(
                name: "OperationReferenceValues");

            migrationBuilder.DropTable(
                name: "OperationLogs");
        }
    }
}
