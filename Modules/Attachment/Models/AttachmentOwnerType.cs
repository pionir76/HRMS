namespace HRMS.Modules.Attachment.Models;

//-----------------------------------------------------------------------------//
// 첨부파일이 어느 도메인 리소스에 속하는지. 새 도메인에 첨부파일 기능을 붙일 때마다
// 값을 하나씩 추가하고, AttachmentsController.CheckDocumentOwnerAsync의 switch에
// 그 도메인의 작성자/잠금 조회도 함께 추가해야 한다.
// (수리일지는 사양 변경으로 첨부파일을 지원하지 않기로 해서 값이 없다.)
//-----------------------------------------------------------------------------//
public enum AttachmentOwnerType
{
    EquipmentPhoto = 1, // 장비 이력카드 사진(장비/설치, 슬롯당 1장). 담당 장비 기준 권한
    EquipmentInspectionHistory = 2, // 장비 검사이력 첨부파일(슬롯 없음, 다건)
    TrainingLog = 3, // 교육훈련 일지 첨부파일(슬롯 없음, 다건). 결재가 진행되면 추가/삭제가 막힌다
    Notice = 4, // 공지사항 첨부파일(슬롯 없음, 다건)
    AppointmentReport = 5, // 사용자별 선해임 신고서(1건 고정 — 내부적으로 슬롯 "report"를 써서 재업로드 시 교체). 시스템관리자만 변경
}
