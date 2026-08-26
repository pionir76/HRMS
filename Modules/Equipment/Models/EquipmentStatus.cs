namespace HRMS.Modules.Equipment.Models;

// 장비 운영 상태 (overview.md 4.1). 미운영/철거/사용중지 상태인 장비의 압축기는
// CompressorPollingService의 수집 대상에서 제외된다.
public enum EquipmentStatus
{
    운영,
    미운영,
    수리중,
    점검중,
    철거예정,
    철거,
    사용중지,
    기타
}
