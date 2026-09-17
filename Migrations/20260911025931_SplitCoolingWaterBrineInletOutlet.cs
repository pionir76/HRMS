using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class SplitCoolingWaterBrineInletOutlet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoolingWaterMin",
                table: "Equipments",
                newName: "CoolingWaterOutletMin");

            migrationBuilder.RenameColumn(
                name: "CoolingWaterMax",
                table: "Equipments",
                newName: "CoolingWaterOutletMax");

            migrationBuilder.RenameColumn(
                name: "BrineMin",
                table: "Equipments",
                newName: "CoolingWaterInletMin");

            migrationBuilder.RenameColumn(
                name: "BrineMax",
                table: "Equipments",
                newName: "CoolingWaterInletMax");

            migrationBuilder.AddColumn<decimal>(
                name: "BrineInletMax",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BrineInletMin",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BrineOutletMax",
                table: "Equipments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BrineOutletMin",
                table: "Equipments",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrineInletMax",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "BrineInletMin",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "BrineOutletMax",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "BrineOutletMin",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "CoolingWaterOutletMin",
                table: "Equipments",
                newName: "CoolingWaterMin");

            migrationBuilder.RenameColumn(
                name: "CoolingWaterOutletMax",
                table: "Equipments",
                newName: "CoolingWaterMax");

            migrationBuilder.RenameColumn(
                name: "CoolingWaterInletMin",
                table: "Equipments",
                newName: "BrineMin");

            migrationBuilder.RenameColumn(
                name: "CoolingWaterInletMax",
                table: "Equipments",
                newName: "BrineMax");
        }
    }
}
