using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_CreatedAt",
                table: "EventLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EquipmentId_CreatedAt",
                table: "EventLogs",
                columns: new[] { "EquipmentId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventLogs_CreatedAt",
                table: "EventLogs");

            migrationBuilder.DropIndex(
                name: "IX_EventLogs_EquipmentId_CreatedAt",
                table: "EventLogs");
        }
    }
}
