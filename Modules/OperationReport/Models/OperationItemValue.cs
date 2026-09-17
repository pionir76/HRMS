namespace HRMS.Modules.OperationReport.Models;

//--------------------------------------------------------------------------------//
// 운전일지 표시 항목(16개) 중 하나의 하루 4개 시각(09/13/16/21시) 값. ItemKey는 레이블이 아니라
// 식별용 키일 뿐이다 — 표시 형식(구분/레이블/순서 등)은 백엔드가 관리하지 않는다(README 참고).
// CompressorId는 압축기별로 반복되는 4개 항목(토출압력/토출가스온도/흡입압력/오일압력)만 채워지고,
// 나머지 12개 장비 단위 항목은 null이다. (LogId, ItemKey, CompressorId) 유일성은 DB 제약이 아니라
// 애플리케이션 코드가 보장한다 — NULL이 섞인 복합키라 Postgres 유니크 제약으로는 깔끔하게 안 된다.
//--------------------------------------------------------------------------------//
public class OperationItemValue
{
    public int Id { get; set; }
    public int LogId { get; set; }
    public string ItemKey { get; set; } = "";
    public int? CompressorId { get; set; }

    public string? Time0900 { get; set; }
    public string? Time1300 { get; set; }
    public string? Time1600 { get; set; }
    public string? Time2100 { get; set; }
}
