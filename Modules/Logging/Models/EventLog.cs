using HRMS.Modules.Equipment.Models;

namespace HRMS.Modules.Logging.Models;

//-----------------------------------------------------------------------------//
// 이벤트 로그. 시스템 전반에서 발생하는 이벤트를 기록한다. (로그인/로그아웃, 장비 통신 장애, 경보 발생 등)
// 이벤트가 어느 장비/압축기/채널에서 발생했는지(해당 없으면 null — 예: 로그인 이벤트).
// Message는 표시용 문장이고, 이 필드들은 프론트가 문자열 파싱 없이 필터링·네비게이션할 때 쓴다.
//-----------------------------------------------------------------------------//
public class EventLog
{
    public int Id { get; set; }
    public EventLogCategory Category { get; set; }
    public string Message { get; set; } = "";
    public string? Username { get; set; }

    public int? EquipmentId { get; set; }
    public int? CompressorId { get; set; }
    public ChannelNo? ChannelNo { get; set; }

    //-----------------------------------------------------------------------------//
    // 경보(Alarm) 이벤트의 상세 스냅샷. 자료 조회 화면에서 이벤트를 클릭하면 팝업에
    // "저압 1.23 MPa (기준 0.50~1.00)"처럼 보여주기 위한 값들이며, 경보 이벤트에서만
    // 채워지고 다른 카테고리는 전부 null이다.
    //
    // 임계값·단위·소수점까지 같이 박아두는 이유: 채널 설정(CompressorChannelSetting)은
    // 나중에 바뀔 수 있는데, 과거 경보 이력을 "지금 설정" 기준으로 표시하면 그때 무슨 일이
    // 있었는지가 왜곡된다(결재자 이름을 승인 시점 값으로 남기는 것과 같은 스냅샷 원칙).
    // Value/LowerLimit/UpperLimit는 채널값과 같은 raw int16 스케일이다 — 백엔드는 소수점
    // 처리를 하지 않고, DecimalPlaces는 프론트가 표시할 때 쓰는 힌트다.
    //-----------------------------------------------------------------------------//
    public short? Value { get; set; } // 경보 발생/해제 시점의 raw 측정값
    public short? LowerLimit { get; set; } // 그 시점 채널 설정의 정상 범위 하한(raw)
    public short? UpperLimit { get; set; } // 그 시점 채널 설정의 정상 범위 상한(raw)
    public int? DecimalPlaces { get; set; } // 표시 소수점 자리수(프론트 변환용)
    public string? Unit { get; set; } // 측정 단위(예: ℃, MPa, A)

    public DateTimeOffset CreatedAt { get; set; }
}
