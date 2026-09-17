# InspectionReport 모듈

점검일지 모듈. Front_Work.md 9번 화면(점검일지)에 대응한다. 원래는 `Modules/Reports`라는 하나의 모듈이 문서류 전체를 다룰 예정이었으나, 문서유형마다 필드·결재·자동화 구조가 상당히 달라서 전부 전용 모듈로 분리했다(운전일지 `Modules/OperationReport`, 장비 검사이력 `Modules/EquipmentInspectionHistory`, 수리일지 `Modules/RepairLog`, 교육훈련 일지 `Modules/TrainingLog`, 공지사항 `Modules/Notice`). 그 결과 빈 껍데기만 남은 `Modules/Reports`는 2026-09-16에 삭제됐다.

- 점검일지 1건 = 장비 1개 + 주(일~토) 1개
- 점검항목 10개(고정 양식, `InspectionItemCatalog`) × 요일별 결과(O/`/`/X) + 항목별 비고
- 결재 3단계(안전관리원/안전관리책임자/안전관리총괄자) 전부 필수 — 판정 로직은 [Modules/Approval](../Approval/README.md) 공용 헬퍼 재사용
- 미기록 요일 자동 이어채우기(`InspectionAutoFillService`) — 매일 한국시간 00:00에, 사용자가 기록하지 않은 요일 칸을 전날 값으로 자동 채운다. 주 경계도 이어진다(일요일엔 지난주 토요일 값을 새 주로 이어받음) — 한 번 기록이 시작되면 사용자가 몇 달을 손대지 않아도 계속 유지된다

## 최초 기록 정책 (사양 확정)

**시스템은 어떤 경우에도 임의의 초기값(예: 전부 "O")을 프로그램적으로 만들어 넣지 않는다.** 담당 장비에 대한 점검일지를 최초 1회는 반드시 사용자가 직접 작성해야 한다 — 이게 있어야 자동 이어채우기가 이어받을 값이 생긴다. 검토했던 대안들(①시스템 셋업 시 전 장비를 "O"로 시드, ②자동 기록 시점에 이전 값이 없으면 "O"로 기본값 채우기)은 모두 채택하지 않기로 확정했다 — 법정 자체검사 문서라서 "실제로 확인해서 정상"과 "아무도 확인 안 해서 시스템이 채운 값"이 데이터상 구분되지 않는 걸 감수할 수 없다고 판단했다.

즉 이 모듈의 자동화는 순수하게 **사용자가 이미 입력한 값을 이어가는 것**까지만 하고, 값을 새로 지어내지는 않는다. 담당 장비에 대해 한 번도 기록이 없는 항목은 계속 `null`로 남고, 그 상태로는 자동 이어채우기가 절대 채우지 않는다(`InspectionAutoFillService`가 데이터가 있는 로그/항목만 훑는 것도 이 정책 때문이다 — "기록 없음"과 "정상 확인됨"을 시스템이 섞지 않으려는 설계).

## 내부 구성

- `Models/InspectionLog.cs` — 점검일지 엔티티. 결재 데이터(Level1~3 ApproverId/ApproverName/ApprovedAt)를 인라인 컬럼으로 직접 갖는다.
- `Models/InspectionResult.cs` — 점검일지 1건 안의 점검항목별 요일 결과(복합키 `LogId`+`ItemNo`)
- `Models/InspectionItemCatalog.cs` — 점검항목 10개 고정 목록(코드 상수, DB 테이블 아님)
- `Models/Dtos.cs` — 조회/저장 API DTO
- `Controllers/InspectionLogsController.cs` — 조회/저장/결재 API
- `Controllers/InspectionItemsController.cs` — 점검항목 고정 목록 조회 API
- `InspectionAutoFillService.cs` — 매일 한국시간 00:00에 실행되는 `BackgroundService`. 사용자가 이미 기록한 값은 절대 덮어쓰지 않고, 비어있는 요일만 바로 전날 값으로 채운다. 결재가 진행 중이어도 이 자동 기록은 멈추지 않는다(수동 저장 API의 결재 잠금과 무관 — [Modules/Approval/README.md](../Approval/README.md) 참고).

API 상세는 [api-manual.md](../../Doc/api-manual.md) 참고.
