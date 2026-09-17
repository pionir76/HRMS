# Approval 모듈

결재 판정 공용 로직 모듈. DB 테이블이나 엔티티는 없다 — 결재 데이터 자체는 각 문서 엔티티에 Level1~3(ApproverId/ApproverName/ApprovedAt) 인라인 컬럼으로 직접 저장하고, 이 모듈은 그 데이터에 적용할 순서/권한 판정 규칙만 공유한다.

현재 이 규칙을 쓰는 문서: 점검일지(`InspectionReport`, 3단계 전부 Required), 운전일지(`OperationReport`), 수리일지(`RepairLog`, 레벨1 해당없음), 교육훈련 일지(`TrainingLog`, 레벨1 해당없음 + 장비 무관).

문서유형마다 별도 Approval 테이블(문서유형+문서ID 다형성 FK)을 두지 않기로 한 이유, 3단계 고정/전결(Delegated)/해당없음(NotApplicable) 개념은 [api-manual.md](../../Doc/api-manual.md)의 "결재 시스템" 절 참고.

## 내부 구성

- `ApprovalLevelMode.cs` — `Required`/`NotApplicable`/`Delegated`. 문서 인스턴스가 아니라 문서유형 자체의 고정 사양이라, 각 문서 컨트롤러(예: `InspectionLogsController.Level1Mode`)에 상수로 선언한다.
- `ApprovalStepDto.cs` — 조회 응답용 레벨 하나(`Status`/`ApproverName`/`ApprovedAt`)
- `ApprovalRules.cs` — 역할→레벨 매핑, 순서 판정(`CanApprove`/`CanCancel`), 상태 DTO 변환(`ToDto`)을 모아둔 정적 헬퍼. 결재가 필요한 문서 컨트롤러는 이 클래스를 그대로 재사용한다(`Modules/InspectionReport/Controllers/InspectionLogsController.cs` 참고).

## 결재 규칙 요약

- 레벨은 항상 3단계 고정(안전관리원/안전관리책임자/안전관리총괄자). 문서유형에 따라 특정 레벨이 없거나(`NotApplicable`) 항상 전결(`Delegated`)일 수 있는데, 둘 다 "실제 승인 절차가 없다"는 점에서 동작은 같고 프론트 표시 문구만 다르다(`/` vs `//전결`).
- 승인은 호출자 본인 기준으로만 처리된다 — 승인 API에 "누구를 승인시킬지"를 별도로 넘기지 않는다. 판정 기준은 문서가 장비에 매달려 있는지에 따라 갈린다:
  - **장비에 매달린 문서**(점검일지/운전일지/수리일지): Role + 그 장비의 담당자인지(`UserEquipment`)를 함께 확인한다.
  - **장비와 무관한 전사 문서**(교육훈련 일지): 검사할 장비가 없으므로 **Role만** 확인한다(사용자 결정 — `Modules/TrainingLog/README.md` 참고).
- 순서를 반드시 지켜야 한다(하위 단계 미완료 시 상위 단계 승인 불가). 취소는 자기 자신의 승인만, 상위 단계가 아직 안 채워졌을 때만 가능하다. 반려 개념은 없다.
- 시스템관리자는 결재 자체를 할 수 없지만, 특정 문서의 결재 진행을 전부 무효화(리셋)해서 관련자들에게 재결재를 요구할 수 있다.
