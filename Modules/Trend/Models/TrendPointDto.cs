namespace HRMS.Modules.Trend.Models;

// GET /api/compressors/{id}/trend 응답 항목 하나. 그 시점의 채널값 7개 + 상태 스냅샷.
// 통신이 끊겼던 구간은 채널값이 그대로 반복되면서 CommunicationStatus가 "끊김"/"재접속중"으로 나오므로,
// 프론트는 이 필드로 "값이 실제로 유지된 것"과 "통신 장애로 값이 멈춘 것"을 구분해서 그릴 수 있다.
public record TrendPointDto(
    DateTimeOffset MeasuredAt,
    decimal? Ch01,
    decimal? Ch02,
    decimal? Ch03,
    decimal? Ch04,
    decimal? Ch05,
    decimal? Ch06,
    decimal? Ch07,
    string RunningStatus,
    string AlarmStatus,
    string CommunicationStatus);
