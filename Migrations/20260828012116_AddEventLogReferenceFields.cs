using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLogReferenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChannelNo",
                table: "EventLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompressorId",
                table: "EventLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "EventLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelNo",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "CompressorId",
                table: "EventLogs");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "EventLogs");
        }
    }
}
