// "Equipment"가 클래스명이자 형제 네임스페이스(HRMS.Modules.Equipment)라 컴파일러가 헷갈려해서
// 별칭을 둔다 — OperationAutoFillService.cs도 같은 이유로 동일한 별칭을 쓴다.
using EquipmentEntity = HRMS.Modules.Equipment.Models.Equipment;

namespace HRMS.Modules.OperationReport.Models;

//--------------------------------------------------------------------------------//
// 장비설정 기반 랜덤값 5개 항목의 ItemKey ↔ Equipment 속성(Has/Min/Max) 매핑. 채널을 읽지
// 않고, 그 장비가 "사용함"이면 Equipment에 설정된 min~max 사이의 랜덤값을 만들어 기입한다
// (README "항목별 자동화 방식 — B. 장비설정 기반 랜덤값" 참고). "사용 안 함"이거나 min/max가
// 설정 안 됐으면 "/"다 — OperationAutoFillService에서 판정한다.
//--------------------------------------------------------------------------------//
public record EquipmentRangeItem(string ItemKey, Func<EquipmentEntity, bool> Has, Func<EquipmentEntity, decimal?> Min, Func<EquipmentEntity, decimal?> Max);

public static class EquipmentRangeItemCatalog
{
    public static readonly IReadOnlyList<EquipmentRangeItem> Items =
    [
        new("냉각수온도(입구)", e => e.HasCoolingWater, e => e.CoolingWaterInletMin, e => e.CoolingWaterInletMax),
        new("냉각수온도(출구)", e => e.HasCoolingWater, e => e.CoolingWaterOutletMin, e => e.CoolingWaterOutletMax),
        new("브라인온도(입구)", e => e.HasBrine, e => e.BrineInletMin, e => e.BrineInletMax),
        new("브라인온도(출구)", e => e.HasBrine, e => e.BrineOutletMin, e => e.BrineOutletMax),
        new("운전전압", e => e.HasVoltage, e => e.VoltageMin, e => e.VoltageMax),
    ];

    public static bool IsRangeGenerated(string itemKey) => Items.Any(i => i.ItemKey == itemKey);
}
