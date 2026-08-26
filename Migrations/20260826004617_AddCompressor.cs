using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCompressor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compressors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    MacAddress = table.Column<string>(type: "text", nullable: true),
                    CommunicationStatus = table.Column<int>(type: "integer", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compressors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compressors_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_BuildingName_Name",
                table: "Equipments",
                columns: new[] { "BuildingName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compressors_EquipmentId",
                table: "Compressors",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Compressors");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_BuildingName_Name",
                table: "Equipments");
        }
    }
}
