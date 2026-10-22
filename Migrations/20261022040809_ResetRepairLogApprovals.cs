using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    //--------------------------------------------------------------------------------//
    // 수리일지 결재에 안전관리원(1단계)을 추가하면서(2026-10-22 고객사 요청), 결재가 이미
    // 진행된 기존 수리일지의 결재를 전부 초기화한다. 시스템관리자의 "결재 전체 리셋"과 같은 처리다.
    //
    // 그대로 두면 "안전관리원은 대기인데 책임자는 승인됨"인 문서가 생긴다. 이런 문서는 안전관리원이
    // 나중에 1단계를 승인할 수도 있어서(1단계는 앞 단계가 없다) 순서가 뒤집힌다. 새 규칙으로
    // 처음부터 다시 결재받게 한다. 결재가 풀리면 수정 잠금도 함께 풀린다.
    //
    // 납품 초기화(reset_for_delivery.sql)는 수리일지를 전부 지우므로 납품 서버에는 영향이 없다.
    //--------------------------------------------------------------------------------//
    /// <inheritdoc />
    public partial class ResetRepairLogApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "RepairLogs" SET
                    "Level1ApproverId" = NULL, "Level1ApproverName" = NULL, "Level1ApprovedAt" = NULL,
                    "Level2ApproverId" = NULL, "Level2ApproverName" = NULL, "Level2ApprovedAt" = NULL,
                    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
                WHERE "Level1ApprovedAt" IS NOT NULL OR "Level2ApprovedAt" IS NOT NULL OR "Level3ApprovedAt" IS NOT NULL;
                """);
        }

        // 지운 결재 기록은 되살릴 수 없다 — 되돌려도 결재 칸은 비어 있는 그대로다.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
