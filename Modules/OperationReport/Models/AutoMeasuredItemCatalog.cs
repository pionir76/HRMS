using HRMS.Modules.Equipment.Models;

namespace HRMS.Modules.OperationReport.Models;

//--------------------------------------------------------------------------------//
// 채널 자동측정 5개 항목의 ItemKey ↔ 채널 매핑. 이 키 이름은 프론트와 합의된 고정 문자열이며,
// 나머지 항목(사용자 직접입력 6개, 장비설정 기반 랜덤값 5개는 EquipmentRangeItemCatalog 참고)의
// 키는 백엔드가 전혀 모른다(프론트가 보내는 대로 저장만 한다 — README "공통 규칙" 참고).
// PerCompressor가 true면 압축기 수만큼 행이 반복되고, false면 장비당 1행이며 그 장비의
// 1번 압축기(SequenceNo=1) 값을 사용한다(운전전류 규칙).
//
// 냉각수온도(입구)/(출구)는 원래 CH01을 그대로 읽었지만, 장비설정 기반 랜덤값으로 사양이
// 바뀌면서 여기서 빠졌다 — EquipmentRangeItemCatalog 참고.
//--------------------------------------------------------------------------------//
public record AutoMeasuredItem(string ItemKey, ChannelNo ChannelNo, bool PerCompressor);

public static class AutoMeasuredItemCatalog
{
    public static readonly IReadOnlyList<AutoMeasuredItem> Items =
    [
        new("토출압력", ChannelNo.CH05, PerCompressor: true),
        new("토출가스온도", ChannelNo.CH02, PerCompressor: true),
        new("흡입압력", ChannelNo.CH04, PerCompressor: true),
        new("오일압력", ChannelNo.CH06, PerCompressor: true),
        new("운전전류", ChannelNo.CH07, PerCompressor: false),
    ];

    public static bool IsAutoMeasured(string itemKey) => Items.Any(i => i.ItemKey == itemKey);
}
