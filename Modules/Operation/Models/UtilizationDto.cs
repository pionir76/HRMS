namespace HRMS.Modules.Operation.Models;

// UtilizationPercent가 null이면 해당 기간에 트렌드 기록이 전혀 없다는 뜻(TotalMinutes=0).
// TotalMinutes를 같이 내려주는 이유: 요청한 기간 전체가 아니라 "실제 기록이 있는 분"만으로
// 비율을 계산하므로, 프론트에서 데이터가 부족한 기간인지 이 값으로 확인할 수 있게 하기 위함.
public record UtilizationDto(
    int EquipmentId,
    DateOnly From,
    DateOnly To,
    int TotalMinutes,
    int RunningMinutes,
    decimal? UtilizationPercent);
