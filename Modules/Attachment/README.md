# Attachment 모듈

여러 도메인(장비 사진, 장비 검사이력, 교육훈련 일지, 공지사항)에서 공용으로 쓰는 첨부파일 모듈. 도메인마다 첨부파일 테이블/업로드 로직을 따로 만들지 않고, `OwnerType`+`OwnerId`로 소속 리소스를 가리키는 테이블 하나로 통일했다.

## 내부 구성

- `Controllers/AttachmentsController.cs` — 목록/업로드/다운로드/삭제 API
- `AttachmentStorage.cs` — 실제 파일 바이트를 파일시스템에 저장/조회/삭제하는 헬퍼(DB에는 바이너리를 넣지 않는다 — 아래 "저장 방식" 참고)
- `Models/Attachment.cs` — 엔티티
- `Models/AttachmentOwnerType.cs` — 첨부파일이 속한 도메인을 구분하는 enum
- `Models/Dtos.cs` — 응답 DTO

## 저장 방식: DB가 아니라 파일시스템

장비 사진뿐 아니라 문서형(교육훈련/공지사항)처럼 여러 파일이 계속 누적되는 용도까지 고려해서, 파일 바이트는 `appsettings`의 `FileStorage:RootPath`(기본값: 실행 파일 옆 `App_Data/attachments`) 아래 `{OwnerType}/{GUID}.{확장자}` 경로에 저장하고, DB(`Attachment.StoredPath`)에는 그 상대경로만 남긴다. DB에 `byte[]`로 저장하지 않는 이유: 파일이 누적될수록 DB 백업(`pg_dump`)/WAL 용량이 같이 커지는 걸 피하기 위함(사용자 결정).

**배포 시 주의**: 이 프로젝트는 Windows 서비스로도 배포되므로, `FileStorage:RootPath`가 상대경로면 항상 실행 파일 위치(`AppContext.BaseDirectory`) 기준으로 고정한다(현재 작업 디렉토리 기준이 아님 — 서비스로 돌 때 작업 디렉토리가 달라질 수 있어서). 서버를 옮기거나 백업할 때 DB뿐 아니라 이 폴더도 같이 챙겨야 한다 — `.gitignore`에 `App_Data/`가 포함되어 있어 저장소에는 올라가지 않는다.

## `OwnerType` / `Slot` 개념

- `OwnerType`: 어느 도메인 리소스에 속하는지. 현재 `EquipmentPhoto`(장비 이력카드의 장비 사진/설치 사진), `EquipmentInspectionHistory`([장비 검사이력](../EquipmentInspectionHistory/README.md)), `TrainingLog`([교육훈련 일지](../TrainingLog/README.md)), `Notice`([공지사항](../Notice/README.md))가 연동되어 있다. 새 도메인이 생기면 이 enum에 값을 추가하고 `CheckDocumentOwnerAsync`에도 작성자/잠금 조회를 함께 추가한다(아래 "접근 제어" 참고). **수리일지(`RepairLog`)는 사양 변경으로 첨부파일을 지원하지 않기로 해서 값이 없다.**
- `Slot`: `EquipmentPhoto`처럼 "정해진 슬롯 하나에 파일 1개, 재업로드 시 덮어쓰기"가 필요한 경우만 쓴다(`equipment`/`installation`). 슬롯 개념이 없는 다건 첨부(`EquipmentInspectionHistory`/`TrainingLog`/`Notice`)는 `Slot`이 `null`이고, 업로드할 때마다 새 행이 추가된다. 같은 `(OwnerType, OwnerId, Slot)` 조합의 유일성은 DB 유니크 인덱스(`Slot IS NOT NULL`인 경우만)로 강제한다(`AppDbContext.OnModelCreating`).
  - **주의**: `Upload`에서 "기존 행을 찾아 덮어쓸지" 판단할 때 `slot`이 `null`이면 그 조회 자체를 건너뛴다. 만약 슬롯 없이도 `(OwnerType, OwnerId, Slot=null)`로 기존 행을 찾아버리면, 같은 `OwnerId`에 두 번째 파일을 올릴 때 첫 번째 파일을 덮어써버리는 버그가 생긴다 — `EquipmentInspectionHistory`를 연동하며 실제로 발견해 고친 부분이다.

## 접근 제어 · 확장자 제한

`OwnerType`별로 분기한다. 업로드/삭제 권한은 두 갈래이고, 조회(`GET`)는 어느 `OwnerType`이든 로그인만 요구한다.

| `OwnerType` | 업로드·삭제 권한 | 확장자 제한 |
|---|---|---|
| `EquipmentPhoto` | 시스템관리자 또는 **그 장비의 담당자**(`UserEquipment`) — `Modules/Equipment/EquipmentAccess.CanManageAsync` | `jpg`/`jpeg`/`png`만 |
| `EquipmentInspectionHistory` | **그 문서의 작성자** 또는 시스템관리자 | 없음 |
| `TrainingLog` | **그 문서의 작성자** 또는 시스템관리자 — 단 결재가 한 단계라도 승인되면 누구도 불가(`409`) | 없음 |
| `Notice` | **그 문서의 작성자** 또는 시스템관리자 | 없음 |
| `AppointmentReport` | **시스템관리자만** (`ownerId`=사용자 Id, 사용자당 1건 — 서버가 슬롯 `report`를 강제해 재업로드 시 교체) | 없음 |

**문서형 `OwnerType`의 권한은 "그 문서를 수정할 수 있는 사람만 첨부도 바꿀 수 있다"는 원칙으로 통일돼 있다**(2026-09-16 사용자 결정). 첨부 추가/삭제도 결국 문서 내용 변경이라, 수정 권한과 어긋나면 "남의 문서에 아무나 파일을 붙이거나 뗄 수 있는" 구멍이 생기기 때문이다. 이 판정은 `AttachmentsController.CheckDocumentOwnerAsync` 한 곳에서 부모 문서의 `CreatedByUserId`(+ 교육훈련은 결재 상태)를 조회해서 수행하며, 부모 문서가 없으면 `404`가 난다.

즉 이 모듈은 **도메인에 완전히 무관하지는 않다.** 원래는 부모 레코드 존재 여부조차 확인하지 않는 순수 제네릭 모듈로 설계했지만, 위 원칙을 지키려면 부모 문서를 들여다볼 수밖에 없었다(`EquipmentPhoto`가 `EquipmentAccess`를 쓰는 것과 같은 성격). **새 문서형 `OwnerType`을 추가할 때는 `CheckDocumentOwnerAsync`의 switch에 그 도메인의 작성자/잠금 조회를 반드시 함께 추가해야 한다** — 빠뜨리면 `400`(미지원 ownerType)으로 떨어진다.

파일 용량 제한(파일당 최대 10MB, `Doc/Front_Work.md` 13장 공통 규칙)은 `OwnerType`과 무관하게 항상 적용된다. 조회(`GET`)는 모든 `OwnerType`에 대해 별도 제약 없이 로그인만 요구한다(다른 조회 API와의 일관성).

## 다운로드와 인증

`GET /api/attachments/{id}`도 다른 API와 동일하게 `Authorization` 헤더가 필요하다 — 그래서 `<img src="...">`에 URL을 직접 박으면 401이 난다. 프론트는 인증된 요청으로 파일을 받은 뒤 `URL.createObjectURL()`로 변환해서 써야 한다(프론트 처리 영역, 백엔드가 별도로 해줄 건 없음).

## 아직 안 한 것

- 앞으로 새 도메인이 생기면 그때 `OwnerType` 추가
- 여러 파일 동시 업로드(멀티파트에 파일 여러 개) — 지금은 호출 1번에 파일 1개. `EquipmentInspectionHistory`/`TrainingLog`도 프론트가 파일 개수만큼 반복 호출하는 방식으로 "다건"을 구현한다. 앞으로 정말 필요해지면 그때 추가
