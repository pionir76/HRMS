using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Operation.Models;

namespace HRMS.Modules.Equipment.Models;

// 냉동장비. 하나의 장비는 압축기 1대 이상으로 구성된다 (overview.md 4.1).
// 대부분의 물리적 사양 필드는 현재 값이 없는 상태로 시드되어 있고(Doc/EqmtList.md 참고),
// 실제 값은 나중에 웹 화면에서 채워 넣을 예정이라 전부 nullable이다.
public class Equipment
{
    public int Id { get; set; }

    public required string Region { get; set; }
    public required string BuildingName { get; set; }
    public required string Name { get; set; }
    public EquipmentStatus Status { get; set; }

    public string? ModelName { get; set; }
    public decimal? RatedPower { get; set; }
    public decimal? RatedVoltage { get; set; }
    public string? CompressorType { get; set; }
    public decimal? CompressorCapacity { get; set; }
    public string? CoolingTowerType { get; set; }
    public decimal? CoolingTowerCapacity { get; set; }
    public decimal? LegalRefrigerationCapacity { get; set; }
    public decimal? UsRefrigerationCapacity { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? InstallDate { get; set; }
    public string? PermitNumber { get; set; }
    public string? Refrigerant { get; set; }
    public decimal? ChargeAmount { get; set; }
    public string? KgsManagementNumber { get; set; }
    public string? Manufacturer { get; set; }
    public decimal? HighPressureTestPressure { get; set; }
    public decimal? LowPressureTestPressure { get; set; }
    public decimal? OverPressureCutoff { get; set; }
    public decimal? SafetyValveSetPointCondenser { get; set; }
    public decimal? SafetyValveSetPointEvaporator { get; set; }

    // 압축기 개별이 아니라 장비 단위로 하나만 존재한다. 소속 압축기 중 CH07(운전전류)이
    // 이 값을 넘는 압축기가 하나라도 있으면 장비를 운전(Running) 상태로 판단한다 (overview.md 4.7).
    public decimal? RunningCurrentThreshold { get; set; }

    // 아래 3개는 관리자가 설정하는 위 Status와 달리, 소속 압축기 데이터로부터 매 폴링 사이클마다
    // 자동 계산되는 실시간 파생 상태다 (EquipmentStatusAggregator.cs). 별도 테이블로 안 빼고
    // Compressor 때와 같은 방식으로 여기 직접 필드로 둔다.
    public RunningStatus RunningStatus { get; set; }
    public AlarmStatus AlarmStatus { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; }
}
