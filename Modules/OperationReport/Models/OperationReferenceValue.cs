namespace HRMS.Modules.OperationReport.Models;

//--------------------------------------------------------------------------------//
// 운전일지 기준값(14개, 예: "1.8MPa 이하"). 압축기별로 반복되는 항목도 기준값 자체는
// 압축기 구분 없이 장비당 1개다. 새 OperationLog를 만들 때 그 장비의 가장 최근 로그의
// 기준값을 그대로 복사해서 시작한다(README "기준값" 절 참고) — 매번 스냅샷을 저장하므로
// 나중에 값을 바꿔도 이미 결재된 과거 문서는 안 바뀐다.
//--------------------------------------------------------------------------------//
public class OperationReferenceValue
{
    public int Id { get; set; }
    public int LogId { get; set; }
    public string ItemKey { get; set; } = "";
    public string? ReferenceText { get; set; }
}
