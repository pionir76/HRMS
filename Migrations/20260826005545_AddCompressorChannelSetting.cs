using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCompressorChannelSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompressorChannelSettings",
                columns: table => new
                {
                    CompressorId = table.Column<int>(type: "integer", nullable: false),
                    ChannelNo = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LowerLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    AlarmEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AlarmDelaySeconds = table.Column<int>(type: "integer", nullable: true),
                    AlarmClearDelaySeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressorChannelSettings", x => new { x.CompressorId, x.ChannelNo });
                    table.ForeignKey(
                        name: "FK_CompressorChannelSettings_Compressors_CompressorId",
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
                name: "CompressorChannelSettings");
        }
    }
}
