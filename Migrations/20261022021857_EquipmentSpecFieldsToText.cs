using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    //--------------------------------------------------------------------------------//
    // 장비 이력카드 사양 칸 11개를 숫자 → 글자(text)로 바꾼다(2026-10-22 고객사 요청 — "100RT",
    // "약 25", "2대"처럼 단위·설명을 붙여 적을 수 있게). 기존 값은 글자로 변환해서 보존한다.
    //
    // EF가 만드는 기본 변환(numeric → text 그대로)은 12.50처럼 끝의 0이 남을 수 있어서,
    // trim_scale(PostgreSQL 13+)로 화면에 보이던 그대로(12.5) 남기도록 SQL을 직접 썼다.
    //--------------------------------------------------------------------------------//
    /// <inheritdoc />
    public partial class EquipmentSpecFieldsToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Equipments"
                    ALTER COLUMN "LegalRefrigerationCapacity" TYPE text USING trim_scale("LegalRefrigerationCapacity")::text,
                    ALTER COLUMN "UsRefrigerationCapacity"    TYPE text USING trim_scale("UsRefrigerationCapacity")::text,
                    ALTER COLUMN "ChargeAmount"               TYPE text USING trim_scale("ChargeAmount")::text,
                    ALTER COLUMN "CompressorCount"            TYPE text USING "CompressorCount"::text,
                    ALTER COLUMN "CompressorCapacity"         TYPE text USING trim_scale("CompressorCapacity")::text,
                    ALTER COLUMN "CoolingTowerCount"          TYPE text USING "CoolingTowerCount"::text,
                    ALTER COLUMN "CoolingTowerCapacity"       TYPE text USING trim_scale("CoolingTowerCapacity")::text,
                    ALTER COLUMN "DesignPressure"             TYPE text USING trim_scale("DesignPressure")::text,
                    ALTER COLUMN "OverPressureCutoff"         TYPE text USING trim_scale("OverPressureCutoff")::text,
                    ALTER COLUMN "RatedVoltage"               TYPE text USING trim_scale("RatedVoltage")::text,
                    ALTER COLUMN "RatedCurrent"               TYPE text USING trim_scale("RatedCurrent")::text;
                """);
        }

        //--------------------------------------------------------------------------------//
        // 되돌릴 때는 숫자로 읽을 수 있는 값만 남기고, "100RT"처럼 글자가 섞인 값은 NULL이 된다.
        //--------------------------------------------------------------------------------//
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Equipments"
                    ALTER COLUMN "LegalRefrigerationCapacity" TYPE numeric USING CASE WHEN "LegalRefrigerationCapacity" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "LegalRefrigerationCapacity"::numeric END,
                    ALTER COLUMN "UsRefrigerationCapacity"    TYPE numeric USING CASE WHEN "UsRefrigerationCapacity" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "UsRefrigerationCapacity"::numeric END,
                    ALTER COLUMN "ChargeAmount"               TYPE numeric USING CASE WHEN "ChargeAmount" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "ChargeAmount"::numeric END,
                    ALTER COLUMN "CompressorCount"            TYPE integer USING CASE WHEN "CompressorCount" ~ '^-?[0-9]+$' THEN "CompressorCount"::integer END,
                    ALTER COLUMN "CompressorCapacity"         TYPE numeric USING CASE WHEN "CompressorCapacity" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "CompressorCapacity"::numeric END,
                    ALTER COLUMN "CoolingTowerCount"          TYPE integer USING CASE WHEN "CoolingTowerCount" ~ '^-?[0-9]+$' THEN "CoolingTowerCount"::integer END,
                    ALTER COLUMN "CoolingTowerCapacity"       TYPE numeric USING CASE WHEN "CoolingTowerCapacity" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "CoolingTowerCapacity"::numeric END,
                    ALTER COLUMN "DesignPressure"             TYPE numeric USING CASE WHEN "DesignPressure" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "DesignPressure"::numeric END,
                    ALTER COLUMN "OverPressureCutoff"         TYPE numeric USING CASE WHEN "OverPressureCutoff" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "OverPressureCutoff"::numeric END,
                    ALTER COLUMN "RatedVoltage"               TYPE numeric USING CASE WHEN "RatedVoltage" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "RatedVoltage"::numeric END,
                    ALTER COLUMN "RatedCurrent"               TYPE numeric USING CASE WHEN "RatedCurrent" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN "RatedCurrent"::numeric END;
                """);
        }
    }
}
