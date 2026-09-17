namespace HRMS.Modules.RepairLog.Models;

//--------------------------------------------------------------------------------//
// 수리일지 1건. 장비 1개에 여러 건이 자유롭게 쌓이는 로그성 데이터라, 점검일지(장비+주차)나
// 운전일지(장비+날짜)처럼 유니크 제약을 두지 않는다.
//
// 결재 데이터(Level1~3)는 별도 테이블 없이 여기에 인라인 컬럼으로 직접 둔다(점검일지/운전일지와
// 동일한 방식 — Modules/Approval/README.md 참고). 이 문서유형은 Level1(안전관리원)이 "해당없음"
// 이라 Level1 컬럼은 실제로 채워지지 않지만, 공용 결재 로직(GetLevel/SetLevel이 1~3을 그대로
// 훑는다)과 스키마 모양을 다른 문서와 같게 유지하려고 컬럼 자체는 그대로 둔다.
//
// ApproverName/CreatedByUserName 등은 그 시점 이름 스냅샷이다 — 나중에 사용자 이름이나 역할이
// 바뀌어도 이미 찍힌 기록은 그대로 남는다.
//--------------------------------------------------------------------------------//
public class RepairLog
{
    public int Id { get; set; }
    public int EquipmentId { get; set; } // 생성 후 변경 불가(PUT 요청에 포함하지 않음)

    public required string Title { get; set; } // 제목
    public DateTimeOffset PerformedAt { get; set; } // 일시(날짜+시각)
    public string? Location { get; set; } // 수리장소
    public string? PerformedBy { get; set; } // 실시자 — 외부 업체일 수 있어 시스템 계정과 무관한 텍스트
    public string? Target { get; set; } // 대상(수리한 부위/부품)
    public string? StateBefore { get; set; } // 이전 상태
    public string? StateAfter { get; set; } // 이후 상태
    public string? Result { get; set; } // 결과
    public string? FailureCause { get; set; } // 고장원인
    public string? PreventiveAction { get; set; } // 방지대책
    public string? Opinion { get; set; } // 종합의견

    public int CreatedByUserId { get; set; }
    public required string CreatedByUserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public int? Level1ApproverId { get; set; } // 안전관리원 — 이 문서유형에서는 "해당없음"이라 항상 비어있다
    public string? Level1ApproverName { get; set; }
    public DateTimeOffset? Level1ApprovedAt { get; set; }

    public int? Level2ApproverId { get; set; } // 안전관리책임자
    public string? Level2ApproverName { get; set; }
    public DateTimeOffset? Level2ApprovedAt { get; set; }

    public int? Level3ApproverId { get; set; } // 안전관리총괄자
    public string? Level3ApproverName { get; set; }
    public DateTimeOffset? Level3ApprovedAt { get; set; }
}
