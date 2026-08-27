using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class ChannelValuesRawInt16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "RunningCurrentThreshold",
                table: "Equipments",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Value",
                table: "CompressorSensorCurrents",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<short>(
                name: "Ch07",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch06",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch05",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch04",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch03",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch02",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Ch01",
                table: "CompressorMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "UpperLimit",
                table: "CompressorChannelSettings",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "LowerLimit",
                table: "CompressorChannelSettings",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RunningCurrentThreshold",
                table: "Equipments",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "CompressorSensorCurrents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch07",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch06",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch05",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch04",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch03",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch02",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Ch01",
                table: "CompressorMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UpperLimit",
                table: "CompressorChannelSettings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LowerLimit",
                table: "CompressorChannelSettings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);
        }
    }
}
