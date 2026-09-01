namespace HRMS.Modules.Equipment.Models;

//------------------------------------------------------------------------------//
// API 응답 전용 타입들. 엔티티를 그대로 노출하지 않고 프론트에 필요한 필드만 담는다.
// enum은 전부 문자열(예: "운영", "연결됨", "CH01")로 내려간다 — 컨트롤러에서 .ToString()으로 변환한다.
// IsRunning/CommunicationStatus/HasAlarm은 관리자가 설정하는 Status와 별개로, 소속 압축기
// 데이터로부터 매 폴링 사이클 자동 집계되는 실시간 파생 상태다 (EquipmentStatusAggregator).
// HasAlarm은 세부 단계(경보발생대기/정상복귀대기 등) 없이 "정상"인지 아닌지만 나타낸다
// (TrendPointDto.HasAlarm과 동일한 원칙 — 실시간 현황 화면엔 확정 여부만 필요하다는 사용자 결정).
//------------------------------------------------------------------------------//
public record EquipmentDto(
    int Id,
    string Region,
    string BuildingName,
    string Name,
    string Status,
    bool IsRunning,
    string CommunicationStatus,
    bool HasAlarm);

public record CompressorDto(
    int Id,
    int SequenceNo,
    string? IpAddress,
    string? MacAddress,
    string CommunicationStatus,
    bool HasAlarm);

// 실시간 현황 화면 상단 카운트용 집계. GET /api/summary.
public record SystemSummaryDto(
    int TotalEquipmentCount,
    int TotalCompressorCount,
    int RunningEquipmentCount,
    int CommunicationFailedCompressorCount);

//------------------------------------------------------------------------------//
// 압축기 목록에 소속 장비명을 같이 보여줄 때 쓰는 평탄화된(join된) 형태.
//------------------------------------------------------------------------------//
public record CompressorFlatDto(
    int Id,
    string BuildingName,
    string EquipmentName,
    string? IpAddress,
    string? MacAddress,
    string CommunicationStatus,
    bool HasAlarm);

//------------------------------------------------------------------------------//
// Value는 TLC 원시값(raw int16) 그대로다 — 소수점 변환은 프론트가 담당한다.
//------------------------------------------------------------------------------//
public record ChannelValueDto(
    string ChannelNo, 
    short Value, 
    DateTimeOffset MeasuredAt);

//------------------------------------------------------------------------------//
// 채널 설정 조회 응답. LowerLimit/UpperLimit도 채널값과 같은 raw 스케일이다.
// DecimalPlaces가 프론트가 raw 값을 실제 값으로 표시할 때 참고할 소수점 자리수다.
//------------------------------------------------------------------------------//
public record ChannelSettingDto(
    string ChannelNo,
    string ChannelName,
    string Unit,
    bool Enabled,
    short? LowerLimit,
    short? UpperLimit,
    bool AlarmEnabled,
    int? AlarmDelaySeconds,
    int? AlarmClearDelaySeconds,
    int DecimalPlaces);
