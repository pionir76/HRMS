namespace HRMS.Modules.Equipment.Models;

// API 응답 전용 타입들. 엔티티를 그대로 노출하지 않고 프론트에 필요한 필드만 담는다.
// enum은 전부 문자열(예: "운영", "연결됨", "CH01")로 내려간다 — 컨트롤러에서 .ToString()으로 변환한다.

public record EquipmentDto(int Id, string Region, string BuildingName, string Name, string Status);

public record CompressorDto(int Id, string? IpAddress, string? MacAddress, string CommunicationStatus, string AlarmStatus);

// 압축기 목록에 소속 장비명을 같이 보여줄 때 쓰는 평탄화된(join된) 형태.
public record CompressorFlatDto(
    int Id,
    string BuildingName,
    string EquipmentName,
    string? IpAddress,
    string? MacAddress,
    string CommunicationStatus,
    string AlarmStatus);

public record ChannelValueDto(string ChannelNo, decimal Value, DateTimeOffset MeasuredAt);
