namespace HRMS.Modules.Trend.Models;

//--------------------------------------------------------------------------------//
// GET /api/compressors/{id}/trend 응답 항목 하나. 그 시점의 채널값 7개 + 상태 스냅샷.
// IsRunning/HasAlarm/IsConnected는 압축기 자신이 아니라 소속 장비의 집계 상태다.
//--------------------------------------------------------------------------------//
public record TrendPointDto(
    DateTimeOffset MeasuredAt,
    short? Ch01,
    short? Ch02,
    short? Ch03,
    short? Ch04,
    short? Ch05,
    short? Ch06,
    short? Ch07,
    bool IsRunning,
    bool HasAlarm,
    bool IsConnected);
