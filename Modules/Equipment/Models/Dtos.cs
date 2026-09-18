namespace HRMS.Modules.Equipment.Models;

//------------------------------------------------------------------------------//
// API 응답 전용 타입들. 엔티티를 그대로 노출하지 않고 프론트에 필요한 필드만 담는다.
// enum은 전부 문자열(예: "운영", "연결됨", "CH01")로 내려간다 — 컨트롤러에서 .ToString()으로 변환한다.
// IsRunning/CommunicationStatus/HasAlarm은 관리자가 설정하는 Status와 별개로, 소속 압축기
// 데이터로부터 매 폴링 사이클 자동 집계되는 실시간 파생 상태다 (EquipmentStatusAggregator).
// HasAlarm은 세부 단계 없이 **확정된 경보인지**만 나타낸다 — 경보발생/정상복귀대기면 true,
// 경보발생대기/경보비활성화/정상이면 false (AlarmStatusExtensions.IsConfirmedAlarm, 사용자 결정 2026-09-17).
// TrendPointDto.HasAlarm과 동일한 원칙 — 실시간 현황 화면엔 확정 여부만 필요하다는 사용자 결정.
//
// 장비 속성 전체(장비관리 화면용). 필드 구성/의미는 Modules/Equipment/README.md "장비 속성"
// 절이 최신 기준이다 — Equipment 엔티티를 고칠 때는 이 DTO와 README를 같이 갱신한다.
// IsRunning/CommunicationStatus/HasAlarm 3개만 자동 계산값이고, 나머지는 전부 관리자/담당자가
// 직접 입력하는 설정값이다(RunningCurrentThreshold 포함).
//
// RunningCurrentThresholdDecimalPlaces: RunningCurrentThreshold 자체는 raw int16라 소수점
// 자리수 정보가 없다(백엔드는 소수점 처리를 안 한다는 원칙 — 프론트가 raw<->실제값 변환에 필요한
// 힌트만 내려준다). 이 장비의 1번 압축기(SequenceNo 최소, 운전일지의 "1번 압축기" 규칙과 동일)
// CH07 채널 설정의 DecimalPlaces를 그대로 가져온 것이다. 압축기가 하나도 없으면 null.
//
// HasEquipmentPhoto/HasInstallationPhoto: 장비 이력카드용 사진 2장(장비 사진/설치 사진)이
// 실제로 등록돼 있는지만 알려주는 가벼운 플래그다 — 사진 원본은 여기 안 실리고
// Modules/Attachment(GET /api/attachments?ownerType=EquipmentPhoto&ownerId={id})로 따로 받는다.
//------------------------------------------------------------------------------//
public record EquipmentDto(
    int Id,
    string Region,
    string? BuildingNumber,
    string BuildingName,
    string? Location,
    string Name,
    string Status,
    string? ModelName,
    string? Manufacturer,
    string? PermitNumber,
    string? ManagementNumber,
    decimal? LegalRefrigerationCapacity,
    decimal? UsRefrigerationCapacity,
    string? Refrigerant,
    decimal? ChargeAmount,
    string? CompressorManufacturer,
    string? CompressorType,
    int? CompressorCount,
    decimal? CompressorCapacity,
    string? CoolingTowerManufacturer,
    string? CoolingTowerType,
    int? CoolingTowerCount,
    decimal? CoolingTowerCapacity,
    string? CondenserType,
    string? EvaporatorType,
    decimal? DesignPressure,
    decimal? OverPressureCutoff,
    decimal? RatedVoltage,
    decimal? RatedCurrent,
    string? SafetyValve,
    string? Notes,
    bool HasCoolingWater,
    decimal? CoolingWaterInletMin,
    decimal? CoolingWaterInletMax,
    decimal? CoolingWaterOutletMin,
    decimal? CoolingWaterOutletMax,
    bool HasBrine,
    decimal? BrineInletMin,
    decimal? BrineInletMax,
    decimal? BrineOutletMin,
    decimal? BrineOutletMax,
    bool HasVoltage,
    decimal? VoltageMin,
    decimal? VoltageMax,
    short? RunningCurrentThreshold,
    int? RunningCurrentThresholdDecimalPlaces,
    bool IsRunning,
    string CommunicationStatus,
    bool HasAlarm,
    bool HasEquipmentPhoto,
    bool HasInstallationPhoto);

//------------------------------------------------------------------------------//
// POST/PUT 요청 본문. EquipmentDto에서 자동 계산값 3개(IsRunning/CommunicationStatus/HasAlarm)와
// Id를 뺀 나머지 전부 — 그 자동 계산값들은 관리자/담당자가 편집할 수 없는 값이기 때문이다.
// Status는 다른 API와 마찬가지로 한글 문자열로 받는다(예: "운영").
//------------------------------------------------------------------------------//
public record SaveEquipmentRequest(
    string Region,
    string? BuildingNumber,
    string BuildingName,
    string? Location,
    string Name,
    string Status,
    string? ModelName,
    string? Manufacturer,
    string? PermitNumber,
    string? ManagementNumber,
    decimal? LegalRefrigerationCapacity,
    decimal? UsRefrigerationCapacity,
    string? Refrigerant,
    decimal? ChargeAmount,
    string? CompressorManufacturer,
    string? CompressorType,
    int? CompressorCount,
    decimal? CompressorCapacity,
    string? CoolingTowerManufacturer,
    string? CoolingTowerType,
    int? CoolingTowerCount,
    decimal? CoolingTowerCapacity,
    string? CondenserType,
    string? EvaporatorType,
    decimal? DesignPressure,
    decimal? OverPressureCutoff,
    decimal? RatedVoltage,
    decimal? RatedCurrent,
    string? SafetyValve,
    string? Notes,
    bool HasCoolingWater,
    decimal? CoolingWaterInletMin,
    decimal? CoolingWaterInletMax,
    decimal? CoolingWaterOutletMin,
    decimal? CoolingWaterOutletMax,
    bool HasBrine,
    decimal? BrineInletMin,
    decimal? BrineInletMax,
    decimal? BrineOutletMin,
    decimal? BrineOutletMax,
    bool HasVoltage,
    decimal? VoltageMin,
    decimal? VoltageMax,
    short? RunningCurrentThreshold);

//------------------------------------------------------------------------------//
// 신규 장비 등록 요청. 압축기는 이 시점에만 같이 생성할 수 있다 — 등록 이후에는 압축기
// 추가/제거 API가 없어서 개수가 영구히 고정된다(사용자 결정). SequenceNo는 이 목록의
// 순서대로 서버가 1부터 자동 부여한다.
//------------------------------------------------------------------------------//
public record CreateCompressorInput(string? IpAddress, string? MacAddress);

public record CreateEquipmentRequest(SaveEquipmentRequest Equipment, List<CreateCompressorInput> Compressors);

public record SaveCompressorRequest(string? IpAddress, string? MacAddress, int SequenceNo);

public record SaveChannelSettingRequest(
    string? ChannelName,
    string? Unit,
    bool Enabled,
    short? LowerLimit,
    short? UpperLimit,
    bool AlarmEnabled,
    int? AlarmDelaySeconds,
    int? AlarmClearDelaySeconds,
    int DecimalPlaces);

public record CompressorDto(
    int Id,
    int SequenceNo,
    string? IpAddress,
    string? MacAddress,
    string CommunicationStatus,
    bool HasAlarm);

//------------------------------------------------------------------------------//
// 실시간 현황 5초 갱신용 경량 상태. GET /api/equipments/status.
// EquipmentDto(속성 40여 개)를 매번 받지 않도록 화면에 필요한 상태값만 담는다.
// 필드 의미는 EquipmentDto/CompressorDto의 같은 이름 필드와 동일하다.
//------------------------------------------------------------------------------//
public record EquipmentStatusDto(
    int Id,
    string Region,
    string BuildingName,
    string Name,
    string Status,
    bool IsRunning,
    string CommunicationStatus,
    bool HasAlarm,
    List<CompressorStatusDto> Compressors);

public record CompressorStatusDto(
    int Id,
    int SequenceNo,
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
    int EquipmentId,
    int SequenceNo,
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
