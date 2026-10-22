using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    //--------------------------------------------------------------------------------//
    // 센서 채널 이름 변경(2026-10-22 고객사 요청): CH01 저온→흡입온도, CH02 고온→토출온도,
    // CH04 저압→흡입압력, CH05 고압→토출압력. 스키마 변경 없이 기존 데이터만 바꾼다.
    // 옛 기본 이름 그대로인 행만 바꾸고, 사용자가 직접 다른 이름으로 바꾼 행은 건드리지 않는다.
    // 신규 압축기 기본값은 ChannelDefaults.cs, 새로 설치하는 DB는 seed_from_csv.sql이 맡는다.
    //--------------------------------------------------------------------------------//
    /// <inheritdoc />
    public partial class RenameSensorChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "CompressorChannelSettings" SET "ChannelName" = CASE "ChannelNo"
                    WHEN 1 THEN '흡입온도' WHEN 2 THEN '토출온도'
                    WHEN 4 THEN '흡입압력' WHEN 5 THEN '토출압력' END
                WHERE ("ChannelNo", "ChannelName") IN ((1, '저온'), (2, '고온'), (4, '저압'), (5, '고압'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "CompressorChannelSettings" SET "ChannelName" = CASE "ChannelNo"
                    WHEN 1 THEN '저온' WHEN 2 THEN '고온'
                    WHEN 4 THEN '저압' WHEN 5 THEN '고압' END
                WHERE ("ChannelNo", "ChannelName") IN ((1, '흡입온도'), (2, '토출온도'), (4, '흡입압력'), (5, '토출압력'));
                """);
        }
    }
}
