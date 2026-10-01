using System.Text.Json.Serialization;

namespace HRMS.Modules.Approval;

//--------------------------------------------------------------------------------//
// 결재가 붙는 문서유형. DB에 저장하는 값이 아니라 일괄 결재 API의 요청/응답에서 문서를
// 가리키는 식별자로만 쓴다.
//
// 이름이 한글이 아니라 영문인 이유: 첨부파일의 ownerType(AttachmentOwnerType)과 같은
// "식별자" 성격이고, 프론트가 화면에 쓰는 한글 이름은 LabelFor로 따로 내려주기 때문이다.
// 그래야 문서유형이 늘어도 프론트가 한글 이름을 하드코딩할 필요가 없다(프론트 요청 2026-09-28).
// 선언 순서가 곧 화면에 보이는 그룹 순서다.
//
// 문서유형별 결재 3단계 운영 방식은 각 컨트롤러의 Level1~3Mode 상수로 흩어져 있었는데,
// 일괄 결재가 4종을 한꺼번에 판정해야 해서 여기 한 곳으로 모았다(2026-09-28).
//--------------------------------------------------------------------------------//
[JsonConverter(typeof(JsonStringEnumConverter<ApprovalDocumentType>))]
public enum ApprovalDocumentType
{
    InspectionLog,
    OperationLog,
    TrainingLog,
    RepairLog
}

public static class ApprovalDocuments
{
    public static readonly ApprovalDocumentType[] All = Enum.GetValues<ApprovalDocumentType>();

    // 점검일지/운전일지는 안전관리원부터 3단계 전부, 수리일지/교육훈련은 안전관리원 단계가
    // 없다(해당없음). 바꾸면 해당 문서의 단건 결재 API와 일괄 결재가 같이 따라간다.
    public static ApprovalLevelModes ModesFor(ApprovalDocumentType type) => type switch
    {
        ApprovalDocumentType.InspectionLog => new(ApprovalLevelMode.Required, ApprovalLevelMode.Required, ApprovalLevelMode.Required),
        ApprovalDocumentType.OperationLog => new(ApprovalLevelMode.Required, ApprovalLevelMode.Required, ApprovalLevelMode.Required),
        ApprovalDocumentType.TrainingLog => new(ApprovalLevelMode.NotApplicable, ApprovalLevelMode.Required, ApprovalLevelMode.Required),
        ApprovalDocumentType.RepairLog => new(ApprovalLevelMode.NotApplicable, ApprovalLevelMode.Required, ApprovalLevelMode.Required),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // 화면에 그대로 표시되는 한글 이름.
    public static string LabelFor(ApprovalDocumentType type) => type switch
    {
        ApprovalDocumentType.InspectionLog => "점검일지",
        ApprovalDocumentType.OperationLog => "운전일지",
        ApprovalDocumentType.TrainingLog => "교육훈련",
        ApprovalDocumentType.RepairLog => "수리일지",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
