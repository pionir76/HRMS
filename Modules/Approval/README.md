# Approval 모듈

결재 판정 공용 로직 + 일괄 결재 API 모듈. DB 테이블이나 엔티티는 없다 — 결재 데이터 자체는 각 문서 엔티티에 Level1~3(ApproverId/ApproverName/ApprovedAt) 인라인 컬럼으로 직접 저장하고, 이 모듈은 그 데이터에 적용할 순서/권한 판정 규칙만 공유한다.

현재 이 규칙을 쓰는 문서: 점검일지(`InspectionReport`, 3단계 전부 Required), 운전일지(`OperationReport`), 수리일지(`RepairLog`, 레벨1 해당없음), 교육훈련 일지(`TrainingLog`, 레벨1 해당없음 + 장비 무관).

문서유형마다 별도 Approval 테이블(문서유형+문서ID 다형성 FK)을 두지 않기로 한 이유, 3단계 고정/전결(Delegated)/해당없음(NotApplicable) 개념은 [api-manual.md](../../Doc/api-manual.md)의 "결재 시스템" 절 참고.

## 내부 구성

- `ApprovalLevelMode.cs` — `Required`/`NotApplicable`/`Delegated`. 문서 인스턴스가 아니라 문서유형 자체의 고정 사양이다.
- `ApprovalDocumentType.cs` — 문서유형 4종(`InspectionLog`/`OperationLog`/`TrainingLog`/`RepairLog`)과 유형별 결재 3단계 운영 방식(`ApprovalDocuments.ModesFor`), 화면에 쓰는 한글 이름(`LabelFor`). 2026-09-28 이전에는 이 값이 컨트롤러마다 `Level1~3Mode` 상수로 흩어져 있었는데, 일괄 결재가 4종을 한꺼번에 판정해야 해서 한 곳으로 모았다.
- `IApprovable.cs` — 문서 4종이 공통으로 갖는 `Level1~3` 컬럼. 컬럼 이름이 이미 같아서 선언만 붙였고 **DB 스키마는 바뀌지 않았다**.
- `ApprovalLevels.cs` — `IApprovable`의 레벨 읽기/쓰기(`Get`/`Set`/`Reset`)와 레벨 번호 기준 판정(`CanApprove`/`CanCancel`/`IsSatisfied`). 4개 컨트롤러가 각자 갖고 있던 동일한 private `GetLevel`/`SetLevel`/`IsLevelSatisfied`를 대체한다(일괄 결재가 다섯 번째 복사본이 되지 않도록 2026-09-28에 정리).
- `ApprovalStepDto.cs` — 조회 응답용 레벨 하나(`Status`/`ApproverName`/`ApprovedAt`)
- `ApprovalRules.cs` — 역할→레벨 매핑, 순서 판정(`CanApprove`/`CanCancel`), 상태 DTO 변환(`ToDto`)을 모아둔 정적 헬퍼. 결재가 필요한 문서 컨트롤러는 이 클래스를 그대로 재사용한다(`Modules/InspectionReport/Controllers/InspectionLogsController.cs` 참고).
- `Controllers/ApprovalsController.cs` — 일괄 결재 API(아래).

## 결재 규칙 요약

- 레벨은 항상 3단계 고정(안전관리원/안전관리책임자/안전관리총괄자). 문서유형에 따라 특정 레벨이 없거나(`NotApplicable`) 항상 전결(`Delegated`)일 수 있는데, 둘 다 "실제 승인 절차가 없다"는 점에서 동작은 같고 프론트 표시 문구만 다르다(`/` vs `//전결`).
- 승인은 호출자 본인 기준으로만 처리된다 — 승인 API에 "누구를 승인시킬지"를 별도로 넘기지 않는다. 판정 기준은 문서가 장비에 매달려 있는지에 따라 갈린다:
  - **장비에 매달린 문서**(점검일지/운전일지/수리일지): Role + 그 장비의 담당자인지(`UserEquipment`)를 함께 확인한다.
  - **장비와 무관한 전사 문서**(교육훈련 일지): 검사할 장비가 없으므로 **Role만** 확인한다(사용자 결정 — `Modules/TrainingLog/README.md` 참고).
- 순서를 반드시 지켜야 한다(하위 단계 미완료 시 상위 단계 승인 불가). 취소는 자기 자신의 승인만, 상위 단계가 아직 안 채워졌을 때만 가능하다. 반려 개념은 없다.
- 시스템관리자는 결재 자체를 할 수 없지만, 특정 문서의 결재 진행을 전부 무효화(리셋)해서 관련자들에게 재결재를 요구할 수 있다.

## 일괄 결재 (2026-09-28)

담당 장비가 많은 사용자가 결재할 문서를 화면마다 찾아 들어가는 수고를 없애기 위한 기능(상단바 결재 배지 + 일괄 결재 팝업). API 3개 — `GET /api/approvals/pending`(문서유형별 목록), `GET /api/approvals/pending/count`(배지용 건수만), `POST /api/approvals/bulk`(한 문서유형의 여러 건 결재). 요청/응답 형식은 [api-manual.md](../../Doc/api-manual.md) 72~74번.

설계에서 정한 것:

- **스키마 변경 없음.** 기안자/작성자 컬럼을 추가하면 문서 4종의 테이블을 전부 고쳐야 해서 부담스럽다는 사용자 결정이라, 목록에 보여주는 정보를 "일자 + 문서정보(지역/시설동/장비명)"로 한정했다. 결재선·기안자는 표시하지 않고, 점검일지·운전일지의 `authorName`도 `null`로 둔다(두 문서는 자동 생성/자동 이어채움이라 "작성자" 개념이 약하다).
- **판정 로직을 새로 만들지 않는다.** 단건 결재와 같은 `ApprovalRules`/`ApprovalLevels`를 그대로 쓴다 — 일괄 결재에서만 통과하는 문서가 생기면 안 된다.
- **건별 처리.** 전부-아니면-전무가 아니라, 처리 못 한 건만 사유 코드(`NotFound`/`Forbidden`/`AlreadyApproved`/`WrongOrder`)와 함께 돌려준다. 목록을 받은 뒤 남이 먼저 결재한 건이 섞일 수 있는데, 한 건 때문에 전체가 롤백되면 사용자가 어디까지 됐는지 알 수 없다.
- **미운영 장비의 문서도 대상**이다(사용자 결정).
- **조회 기간 제한 없음**(사용자 결정 2026-09-28). 기간 컷오프를 두면 그 기간을 넘긴 미결재 문서가 목록에서 사라져 결재할 방법 자체가 없어진다 — 결재는 밀린 것일수록 처리해야 한다.
- 그래서 **"내 단계 미결재 + 앞 단계 완료" 조건을 SQL에서 전부 거른다**(`ApprovalsController.Pending`). 기간 제한이 없으니 메모리로 끌어와서 거르면 이미 끝난 문서까지 다 읽게 된다. 레벨마다 보는 컬럼이 달라 엔티티별로 같은 모양의 작은 switch가 4개 생기는데, EF가 인터페이스 속성을 SQL로 번역하지 못해서 어쩔 수 없다.
- 표시용 문자열(`title`/`subtitle`/`periodText`/`label`)은 서버가 만들어 보낸다. 문서유형마다 날짜 단위(주/일)와 제목 규칙이 달라서, 프론트가 유형별 분기와 한글 이름을 하드코딩하지 않아도 새 문서유형이 그대로 추가되게 하려는 것이다(프론트 요청).
- 이벤트 로그는 `EventLogger.Add`(저장하지 않는 변형)로 모았다가 결재와 함께 한 번에 저장한다. `LogAsync`는 건마다 `SaveChanges`를 호출해서 100건이면 DB 왕복도 100번이 된다.
- **일괄 취소는 제공하지 않는다**(되돌리는 동작이라 위험 — 프론트 합의). 취소는 문서별 단건 API로만.
