# Equipment 모듈

장비 및 압축기 관리 모듈. **장비 관련 내용은 앞으로 이 문서에 정리한다** — 속성이 추가/변경되면 여기부터 갱신한다.

- 장비(Equipment) 등록/조회/수정, 운영상태 관리
- 압축기(Compressor) 등록/조회/수정, IP/포트/수집주기 등 통신 설정
- 센서 채널(Channel) 설정 관리
- 담당 장비 기준 목록 필터(2026-09-28): `GET /api/equipments?mine=true`는 로그인 사용자가 담당인 장비만 반환한다(시스템관리자는 전체). 장비관리·점검일지·운전일지 화면이 쓰며, 그 화면들의 쓰기 권한(`EquipmentAccess`)과 범위를 맞추는 것이 목적이다. 필터링을 프론트에 맡기지 않은 이유: API는 그대로 열려 있어 권한 경계가 되지 못하고, 158대 전체(약 190KB)를 매번 보내게 된다
- 설정값 일괄 적용(2026-10-01, `Controllers/BulkSettingsController.cs`, api-manual 77·78번) — **백엔드만 구현하고 프론트 반영은 보류**(사용자 결정 2026-10-01 — 초기 운영 단계에서 장비마다 맞춰 둔 설정값을 한 번에 덮어쓸 위험이 커서). 관리자가 고른 여러 장비에 채널 상·하한·지연시간·경보 켜기/끄기·소수점, 운전전류 기준값을 한 번에 적용한다. 시스템관리자 전용. **전부-아니면-전무**(설정이 일부만 바뀐 채 남으면 장비마다 기준이 달라져서), 사용 안 함 채널은 건너뜀. 상·하한은 raw 값이라 단위·소수점 자리수가 같은 채널끼리만 같은 값을 넣을 수 있다(같은 50이 온도에선 5.0℃, 압력에선 0.50MPa). 검증 규칙은 개별 수정 API와 같고, 한쪽 한계만 바꿔 하한 > 상한이 되는 압축기가 하나라도 생기면 거부한다
- 장비/압축기/채널 설정값 검증(2026-09-28): 장비 상태는 정의된 8개만(`Enum.IsDefined`), 압축기 순번은 1 이상 + 장비 내 유일, 채널은 하한≤상한·지연시간 0 이상·소수점 0~4·레지스터 주소 0~9999. 그 전에는 전부 그대로 저장되어 영구 경보나 화면 오류로 이어졌다
- 장비 운영상태에 따른 수집·노출 제외 — **상태가 `운영`인 장비만 수집하고 실시간 현황 API(`/api/summary`, `/api/compressors`, `/api/equipments/status`)에 노출한다**(2026-09-17). 장비관리용 `GET /api/equipments`, `GET /api/equipments/{id}`는 상태를 되돌릴 수 있도록 모든 장비를 보여준다

## 내부 구성

- `Controllers/EquipmentsController.cs` — 장비 조회/등록(`POST`)/수정(`PUT`) API. 등록은 시스템관리자 전용, 수정은 담당 장비만(`EquipmentAccess`)
- `Controllers/CompressorsController.cs` — 압축기 조회/수정(`PUT`) API, 채널 설정 조회/수정(`PUT`) API. 압축기 등록/삭제 API는 없음(아래 "장비관리 접근 제어·삭제 정책" 참고)
- `Controllers/SummaryController.cs` — 실시간 현황 화면용 전체 집계 API(`GET /api/summary`)
- `Models/` — 엔티티, DTO
- `EquipmentAccess.cs` — 장비관리 영역 전용 접근 제어 헬퍼(시스템관리자 또는 담당 장비 판정)
- `Models/ChannelDefaults.cs` — 압축기 등록 시 CH01~07 채널 설정 7행을 기본값(채널명/단위는 기존 시드와 동일, 소수점 자리수는 단위별 기본값)으로 생성

## 장비관리 접근 제어 · 삭제 정책

- **등록**(`POST /api/equipments`)은 시스템관리자만 가능하다 — 신규 장비는 아직 담당자가 없어서 "담당 장비" 개념이 성립하지 않는다.
- **수정**(`PUT /api/equipments/{id}`, `PUT /api/compressors/{id}`, `PUT /api/compressors/{id}/channel-settings/{channelNo}`)은 시스템관리자 또는 그 장비의 담당자(`UserEquipment`)만 가능하다. 이 제약은 장비관리 API에만 적용되고, 점검일지/운전일지 저장 API에는 적용하지 않는다(그쪽은 사양 미확정으로 보류 — 사용자 결정).
- **삭제 API는 없다.** 장비를 없애려면 `Status`를 `철거`로 바꾼다 — 이력 데이터가 전부 보존된다. 압축기는 상태 필드 자체가 없다 — 압축기의 "철거"는 소속 장비의 `Status`를 따르는 것으로 본다(압축기 단독 철거 개념 없음).
- **압축기는 장비 등록(`POST /api/equipments`) 시점에만 생성 가능하고, 그 이후로는 개수가 영구히 고정된다** — 추가/제거 API 자체가 없다(사용자 결정, 어떤 이유로도 변경 불가).

## 장비 속성 (`Equipment` 엔티티)

`Region`/`BuildingName`/`Name`/`Status`를 제외한 나머지는 전부 nullable이다 — 현재 값 없이 시드되어 있고, 실제 값은 나중에 웹 화면에서 채워 넣을 예정이다.

### 기본 정보

| 한글명   | 필드명             | 타입              | 비고                                                                            |
| -------- | ------------------ | ----------------- | ------------------------------------------------------------------------------- |
| 지역     | `Region`           | `string` (필수)   | A/B/C지구                                                                       |
| 건물번호 | `BuildingNumber`   | `string?`         | 예: `A1-1` — 숫자만이 아니라 동-호 형식의 코드라 문자열                         |
| 건물이름 | `BuildingName`     | `string` (필수)   | 실제 건물 명칭(예: `장비 내구동`). 시설동명. `(BuildingName, Name)` 조합 유니크 |
| 위치     | `Location`         | `string?`         | 건물 내 세부 위치. 예: `옥상`                                                   |
| 장비명   | `Name`             | `string` (필수)   |                                                                                 |
| 상태     | `Status`           | `EquipmentStatus` | 운영/미운영/수리중/점검중/철거예정/철거/사용중지/기타 — 관리자가 직접 설정      |
| 모델명   | `ModelName`        | `string?`         |                                                                                 |
| 제조사   | `Manufacturer`     | `string?`         |                                                                                 |
| 허가번호 | `PermitNumber`     | `string?`         |                                                                                 |
| 관리번호 | `ManagementNumber` | `string?`         |                                                                                 |

### 냉동/냉매

| 한글명         | 필드명                       | 타입       |
| -------------- | ---------------------------- | ---------- |
| 냉동능력(법정) | `LegalRefrigerationCapacity` | `decimal?` |
| 냉동능력(US)   | `UsRefrigerationCapacity`    | `decimal?` |
| 사용냉매       | `Refrigerant`                | `string?`  |
| 냉매충전량     | `ChargeAmount`               | `decimal?` |

### 압축기 / 냉각탑

| 한글명       | 필드명                     | 타입       | 비고                                                                                                                                                                                                   |
| ------------ | -------------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 압축기제조사 | `CompressorManufacturer`   | `string?`  |                                                                                                                                                                                                        |
| 압축기형식   | `CompressorType`           | `string?`  |                                                                                                                                                                                                        |
| 압축기수량   | `CompressorCount`          | `int?`     | **표준 사양값이다.** 실제 `Compressor` 테이블 행 수(COUNT)와는 무관하게 별도로 관리한다 — 장비 이력카드 조회에 쓰이며, 현장 실측이 사양과 다를 수 있어도 이 속성값 자체를 그대로 신뢰한다(사용자 결정) |
| 압축기용량   | `CompressorCapacity`       | `decimal?` |                                                                                                                                                                                                        |
| 냉각탑제조사 | `CoolingTowerManufacturer` | `string?`  |                                                                                                                                                                                                        |
| 냉각탑형식   | `CoolingTowerType`         | `string?`  |                                                                                                                                                                                                        |
| 냉각탑수량   | `CoolingTowerCount`        | `int?`     |                                                                                                                                                                                                        |
| 냉각탑용량   | `CoolingTowerCapacity`     | `decimal?` |                                                                                                                                                                                                        |

### 기타 설비 사양

| 한글명            | 필드명               | 타입       |
| ----------------- | -------------------- | ---------- |
| 응축기형식        | `CondenserType`      | `string?`  |
| 증발기형식        | `EvaporatorType`     | `string?`  |
| 설계압력          | `DesignPressure`     | `decimal?` |
| 과압차단압력(HPC) | `OverPressureCutoff` | `decimal?` |
| 정격전압          | `RatedVoltage`       | `decimal?` |
| 정격전류          | `RatedCurrent`       | `decimal?` |
| 안전밸브          | `SafetyValve`        | `string?`  |
| 기타사항          | `Notes`              | `string?`  |

`SafetyValve`는 위치/구경/수량/작동압력을 함께 적는 복합 서술형 항목이라(실물 이력카드 라벨: "안전밸브(위치/구경/수량/작동압력)") 단일 숫자값이 아니라 자유 텍스트다.

### 냉각수 / 브라인 / 전압 — 유무 + 정상범위

냉각수·브라인은 운전일지에도 입구/출구가 별도 항목이라(9번 항목 참고) 정상범위도 입구/출구 따로 둔다. 전압은 운전일지에도 단일 항목이라 구분이 없다.

| 한글명                | 필드명                                            | 타입       |
| --------------------- | ------------------------------------------------- | ---------- |
| 냉각수유무            | `HasCoolingWater`                                 | `bool`     |
| 냉각수 입구 min / max | `CoolingWaterInletMin` / `CoolingWaterInletMax`   | `decimal?` |
| 냉각수 출구 min / max | `CoolingWaterOutletMin` / `CoolingWaterOutletMax` | `decimal?` |
| 브라인유무            | `HasBrine`                                        | `bool`     |
| 브라인 입구 min / max | `BrineInletMin` / `BrineInletMax`                 | `decimal?` |
| 브라인 출구 min / max | `BrineOutletMin` / `BrineOutletMax`               | `decimal?` |
| 전압유무              | `HasVoltage`                                      | `bool`     |
| 전압min / max         | `VoltageMin` / `VoltageMax`                       | `decimal?` |

**연결 완료.** 운전일지(`Modules/OperationReport`) 자동 기록(`OperationAutoFillService`, `EquipmentRangeItemCatalog`)에서 이 장비가 냉각수/브라인/전압을 "사용함"(`Has* = true`)이면 이 min~max 범위 안의 랜덤값(소수점 1자리 반올림)을, "사용 안 함"이거나 min/max 중 하나라도 `NULL`이면 `/`를 기입한다.

### 운전 판정 설정 (관리자가 설정하는 입력값)

| 한글명          | 필드명                    | 타입     | 비고                                                                                                                                                                                          |
| --------------- | ------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 운전전류 임계값 | `RunningCurrentThreshold` | `short?` | 압축기 CH07(운전전류) 판정 임계값(raw int16). 압축기 개별이 아니라 장비 단위로 관리자가 설정하는 값이다 — 아래 `IsRunning`은 이 값을 근거로 시스템이 계산하는 **결과**이므로 서로 섞지 않는다 |

`RunningCurrentThreshold`는 raw int16라 그 자체로는 소수점 자리수를 알 수 없다. `GET`/`POST`/`PUT` 응답(`EquipmentDto`)에는 이를 보완하는 `RunningCurrentThresholdDecimalPlaces`(`int?`, 응답 전용 — 요청 본문엔 없음)가 같이 내려간다. 이 장비의 1번 압축기(`SequenceNo` 최소) CH07 채널 설정의 `DecimalPlaces`를 그대로 가져온 값으로, 프론트가 raw↔실제값 변환에 쓰는 힌트다. 압축기가 하나도 없는 장비면 `null`.

**채널 `DecimalPlaces`의 단위별 기본값**(사용자 결정, 2026-09-14): 압력(`MPa`) 2자리, 온도(`℃`)/전압(`V`)/전류(`A`) 1자리. 신규 압축기 등록(`ChannelDefaults.CreateAll`) 시 이 기준으로 채워지고, 이미 등록돼 있던 기존 채널 설정도 이 기준으로 DB에서 일괄 보정했다(API를 거치지 않은 직접 UPDATE). 담당자가 24번(`PUT .../channel-settings/{channelNo}`) API로 개별 수정하는 건 자유다 — 이건 어디까지나 "기본값"이지 강제 규칙은 아니다.

### 압축기 레지스터 주소 (사양 추가 2026-09-18, 아직 컬럼 없음)

CH01~CH07을 읽어올 D-Register 주소는 시스템 공통값이 아니라 **압축기별 설정값**이다. 표준 장비는 360/361/362/364/365/366/367이지만, LG 냉동기(배터리 #1~#7)는 TLC 1대가 압축기 42대를 중계하면서 압축기마다 주소가 다르다(주소표는 [Doc/pclink protocol.md](../../Doc/pclink%20protocol.md)). 관리 화면에서 입력할 수 있도록 `Compressor`에 주소 7개를 추가할 예정이다(`Modules/Communication/README.md`의 작업 항목 참고). 센서가 없는 채널은 주소를 비우고 채널 설정의 `Enabled`를 false로 둔다 — LG 42대의 CH03·CH06이 이미 그 상태다.

### 실시간 파생 상태 (관리자가 직접 설정하지 않음 — 위 설정값을 근거로 시스템이 자동 계산)

| 필드명                | 타입                  | 설명                                                                                                                                                        |
| --------------------- | --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IsRunning`           | `bool`                | 소속 압축기 중 하나라도 CH07 값이 `RunningCurrentThreshold`를 넘으면 `true`. 소속 압축기 데이터로부터 매 폴링 사이클 자동 집계(`EquipmentStatusAggregator`) |
| `AlarmStatus`         | `AlarmStatus`         | 위와 동일                                                                                                                                                   |
| `CommunicationStatus` | `CommunicationStatus` | 위와 동일                                                                                                                                                   |

### 장비 이력카드 사진 (응답 전용, `Equipment` 엔티티 필드 아님)

장비 사진/설치 사진 자체는 `Equipment` 테이블이 아니라 [Modules/Attachment](../Attachment/README.md)가 관리한다(`ownerType=EquipmentPhoto`, `ownerId`=장비 Id, `slot`은 `equipment`/`installation`). `EquipmentDto`에는 사진 존재 여부만 가벼운 플래그로 같이 내려간다.

| 필드명                 | 타입   | 설명                                                                           |
| ---------------------- | ------ | ------------------------------------------------------------------------------ |
| `HasEquipmentPhoto`    | `bool` | 장비 사진(`slot=equipment`) 등록 여부. `GetPhotoFlagsAsync`가 조회 시점에 계산 |
| `HasInstallationPhoto` | `bool` | 설치 사진(`slot=installation`) 등록 여부                                       |
