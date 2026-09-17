using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;

namespace HRMS.Modules.Equipment.Models;

//----------------------------------------------------------------------------------//
// 냉동장비. 하나의 장비는 압축기 1대 이상으로 구성된다 (overview.md 4.1).
// 대부분의 속성 필드는 현재 값이 없는 상태로 시드되어 있고, 실제 값은 나중에 웹 화면에서
// 채워 넣을 예정이라 전부 nullable이다(필수 식별 항목인 Region/BuildingName/Name/Status 제외).
// 전체 속성 목록과 각 필드의 의미는 이 모듈 README.md "장비 속성" 절이 최신 기준이다 — 이
// 엔티티를 고칠 때는 README도 같이 갱신한다.
//----------------------------------------------------------------------------------//
public class Equipment
{
    public int Id { get; set; }

    //----------------------------------------------------------------------------------//
    // 기본 정보
    //----------------------------------------------------------------------------------//
    public required string Region { get; set; } // 지역
    public string? BuildingNumber { get; set; } // 건물번호
    public required string BuildingName { get; set; } // 건물이름(시설동명)
    public string? Location { get; set; } // 위치
    public required string Name { get; set; } // 장비명
    public EquipmentStatus Status { get; set; } // 상태

    public string? ModelName { get; set; } // 모델명
    public string? Manufacturer { get; set; } // 제조사
    public string? PermitNumber { get; set; } // 허가번호
    public string? ManagementNumber { get; set; } // 관리번호

    //----------------------------------------------------------------------------------//
    // 냉동/냉매
    //----------------------------------------------------------------------------------//
    public decimal? LegalRefrigerationCapacity { get; set; } // 냉동능력(법정)
    public decimal? UsRefrigerationCapacity { get; set; } // 냉동능력(US)
    public string? Refrigerant { get; set; } // 사용냉매
    public decimal? ChargeAmount { get; set; } // 냉매충전량

    //----------------------------------------------------------------------------------//
    // 압축기 — CompressorCount는 표준 사양값이다. 실제 Compressor 테이블 행 수(COUNT)와는
    // 별개로 관리한다 — 현장 실측이 사양과 다를 수 있어서, 속성값 자체를 그대로 신뢰한다(사용자 결정).
    //----------------------------------------------------------------------------------//
    public string? CompressorManufacturer { get; set; } // 압축기제조사
    public string? CompressorType { get; set; } // 압축기형식
    public int? CompressorCount { get; set; } // 압축기수량
    public decimal? CompressorCapacity { get; set; } // 압축기용량

    //----------------------------------------------------------------------------------//
    // 냉각탑
    //----------------------------------------------------------------------------------//
    public string? CoolingTowerManufacturer { get; set; } // 냉각탑제조사
    public string? CoolingTowerType { get; set; } // 냉각탑형식
    public int? CoolingTowerCount { get; set; } // 냉각탑수량
    public decimal? CoolingTowerCapacity { get; set; } // 냉각탑용량

    public string? CondenserType { get; set; } // 응축기형식
    public string? EvaporatorType { get; set; } // 증발기형식

    public decimal? DesignPressure { get; set; } // 설계압력
    public decimal? OverPressureCutoff { get; set; } // 과압차단압력(HPC)

    public decimal? RatedVoltage { get; set; } // 정격전압
    public decimal? RatedCurrent { get; set; } // 정격전류

    public string? SafetyValve { get; set; } // 안전밸브(위치/구경/수량/작동압력 등 복합 서술형 — 단일 숫자값이 아니라 자유 텍스트)

    public string? Notes { get; set; } // 기타사항

    //----------------------------------------------------------------------------------//
    // 냉각수/브라인/전압 사용 여부 및 정상 범위(Min~Max). 운전일지 자동 기록에서 "사용함"이면
    // 이 범위 안의 랜덤값을, "사용 안 함"이면 "/"를 표시하는 데 쓰일 예정이다 — 아직 OperationReport
    // 쪽은 연결하지 않았고, 이 필드들만 먼저 추가해둔 상태다(추후 별도 작업에서 연결 예정).
    // 냉각수/브라인은 운전일지에도 입구/출구가 별도 항목이라 범위도 입구/출구 따로 둔다.
    // 전압은 운전일지에도 단일 항목이라 입구/출구 구분이 없다.
    //----------------------------------------------------------------------------------//
    public bool HasCoolingWater { get; set; } // 냉각수유무
    public decimal? CoolingWaterInletMin { get; set; } // 냉각수 입구 min
    public decimal? CoolingWaterInletMax { get; set; } // 냉각수 입구 max
    public decimal? CoolingWaterOutletMin { get; set; } // 냉각수 출구 min
    public decimal? CoolingWaterOutletMax { get; set; } // 냉각수 출구 max

    public bool HasBrine { get; set; } // 브라인유무
    public decimal? BrineInletMin { get; set; } // 브라인 입구 min
    public decimal? BrineInletMax { get; set; } // 브라인 입구 max
    public decimal? BrineOutletMin { get; set; } // 브라인 출구 min
    public decimal? BrineOutletMax { get; set; } // 브라인 출구 max

    public bool HasVoltage { get; set; } // 전압유무
    public decimal? VoltageMin { get; set; }
    public decimal? VoltageMax { get; set; }

    //----------------------------------------------------------------------------------//
    // 압축기 개별이 아니라 장비 단위로 하나만 존재한다. 소속 압축기 중 CH07(운전전류)이
    // 이 값을 넘는 압축기가 하나라도 있으면 장비를 운전(Running) 상태로 판단한다 (overview.md 4.7).
    // CH07 원시값(raw int16)과 직접 비교하므로 이 값도 raw 스케일이다 — 소수점 가공 없음.
    //----------------------------------------------------------------------------------//
    public short? RunningCurrentThreshold { get; set; }

    //----------------------------------------------------------------------------------//
    // 아래 3개는 관리자가 설정하는 위 Status와 달리, 소속 압축기 데이터로부터 매 폴링 사이클마다
    // 자동 계산되는 실시간 파생 상태다 (EquipmentStatusAggregator.cs). 별도 테이블로 안 빼고
    // Compressor 때와 같은 방식으로 여기 직접 필드로 둔다.
    //----------------------------------------------------------------------------------//
    public bool IsRunning { get; set; }
    public AlarmStatus AlarmStatus { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; }

    //----------------------------------------------------------------------------------//
    // 장비 등록 시(POST /api/equipments) 압축기를 한 번에 같이 생성하기 위한 탐색 속성이다.
    // 등록 이후에는 압축기 추가/제거 API 자체가 없다 — 개수가 영구히 고정된다(사용자 결정).
    // 조회는 이 컬렉션이 아니라 Modules/Equipment/Controllers의 전용 엔드포인트를 쓴다.
    //----------------------------------------------------------------------------------//
    public List<Compressor> Compressors { get; set; } = [];
}
