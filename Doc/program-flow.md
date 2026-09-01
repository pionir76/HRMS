# 프로그램 흐름 설명

현재까지 작성된 코드가 실제로 어떻게 동작하는지 정리한 문서. 요구사항은 [overview.md](overview.md), DB 설치는 [setup.md](setup.md) 참고.

## 1. 전체 그림

```text
[앱 시작]
  Program.cs
   ├─ DbContext(PostgreSQL) 등록
   └─ CompressorPollingService 등록 → 백그라운드에서 자동 시작
             │
             │ (3초마다 무한 반복)
             ▼
   ┌───────────────────────────────────────────┐
   │ CompressorPollingService                   │
   │  1. DB에서 활성 압축기 목록 조회            │
   │  2. 압축기마다 동시에 값 읽기               │
   │     (테스트 모드: 랜덤값 / 실모드: PC-Link) │
   │  3. 성공/실패에 따라 통신상태 갱신          │
   │  4. 값 저장 + 채널별 경보 판정(AlarmEvaluator)│
   │  5. 압축기 → 장비 상태 집계                 │
   │     (EquipmentStatusAggregator)             │
   └───────────────────────────────────────────┘
             │
             ▼
        PostgreSQL (hrms DB)
             ▲
             │ 조회만
   ┌─────────────────────────────────────────┐
   │ EquipmentsController / CompressorsController │
   │  프론트 요청 시 DB를 직접 조회해서 JSON 응답 │
   └─────────────────────────────────────────┘
```

폴링(수집·판정)과 API(조회)는 완전히 독립적으로 동작한다. 폴링 서비스는 프론트 요청과 무관하게 항상 돌고 있고, API는 그 결과가 쌓인 DB를 그냥 읽기만 한다.

## 2. 앱 시작 (Program.cs)

1. `AddControllers()` — REST API 컨트롤러 활성화
2. `AddDbContext<AppDbContext>()` — PostgreSQL 연결 (연결 문자열은 `appsettings.Development.json`)
3. `AddHostedService<CompressorPollingService>()` — 앱이 뜨자마자 폴링 루프가 백그라운드에서 자동 시작됨 (별도로 실행시킬 필요 없음)
4. JWT 인증 등록(`AddAuthentication().AddJwtBearer(...)`) — 서명 키/발급자는 `Jwt:Key`, `Jwt:Issuer` 설정값 (자세한 내용은 8장 참고)
5. `Users` 테이블이 비어있으면 관리자 계정(`admin`/`admin1234`)을 자동 생성 (최초 1회만 동작)
6. `EventLog`(System 카테고리)에 시작 이벤트 기록 — 예: `"HRMS 백엔드 시작 (TestMode=True, 장비 105대, 압축기 244대)"`. 화면 없이 도는 Windows Service라 "그때 정상적으로 떴는지"를 나중에 확인할 유일한 흔적이며, 여기까지 도달했다는 것 자체가 DB 연결이 정상이라는 뜻이라 별도 "DB 연결 확인" 로그는 두지 않는다. `TestMode` 값을 같이 남겨서, 운영에 실수로 켜진 채 배포됐는지도 바로 확인할 수 있다.
7. `UseWindowsService()` — 운영 환경에서는 Windows Service로, 개발 중에는 콘솔 앱으로 동일하게 동작
8. `UseAuthentication()` → `UseAuthorization()` → `MapControllers()` — 아래 컨트롤러들의 라우팅 활성화

## 3. 압축기 폴링 루프 (Modules/Communication/CompressorPollingService.cs)

`BackgroundService`를 상속한 클래스로, `ExecuteAsync`가 앱 종료 전까지 아래를 반복한다.

### 한 사이클(`PollOnceAsync`)의 순서

1. **대상 조회** — `Compressors`와 `Equipments`를 조인해서, 소속 장비가 미운영/철거/사용중지가 아닌 압축기만 뽑는다 (테스트 모드가 아니면 IP가 있는 것만).
2. **동시 통신** — 뽑힌 압축기 전부에 대해 값을 동시에 읽는다.
   - **테스트 모드**(`Communication:TestMode = true`): 실제 통신 없이 항상 성공 처리하고, CH01~07에 `-200~1200`(raw int16 기준) 범위의 랜덤값을 채운다. 기본 경보 상/하한(raw 0~1000)을 자연스럽게 넘나들도록 범위를 넉넉하게 잡아서, 경보 상태 전이를 실제로 관찰할 수 있게 했다.
   - **실제 모드**: `PcLinkClient.ReadChannelsAsync()`를 `Task.WhenAll`로 동시 호출. 압축기 하나가 타임아웃 나도 `try/catch`로 감싸져 있어서 다른 압축기 호출에 영향을 주지 않는다.
   - 이 단계에서는 DB에 아무것도 쓰지 않는다(동시 실행 중 `DbContext`를 건드리면 스레드 안전성 문제가 생기므로, 결과만 메모리에 모아둔다).
3. **통신상태 반영** — 성공하면 `연결됨`, 실패인데 직전이 `연결됨`이었으면 `재접속중`, 그 외 실패는 `끊김`. 같은 시점에 **통신 장애 경보**(`HasCommunicationAlarm`)도 갱신한다 — 성공하면 즉시 `false`로 초기화, 실패하면 처음 끊긴 시각(`DisconnectedSince`)만 기록해뒀다가 `CommunicationFailureAlarmDelay`(30초 고정, 통신 모듈 공통값) 이상 계속 끊긴 상태일 때만 `true`로 켠다. 통신이 불안정해서 짧게 끊겼다 붙었다 하는 것까지 매번 경보로 잡지 않기 위한 디바운스이며, 채널값 기준 `AlarmStatus`와는 완전히 별개다(사용자 결정).
4. **현재값 + 채널 경보 판정** (`UpdateCurrentValuesAsync`) — 성공한 압축기만 대상으로:
   - CH01~07 값을 `CompressorSensorCurrent`에 UPSERT (압축기당 최대 7행 고정, 계속 늘어나지 않음)
   - 값을 갱신한 직후 `AlarmEvaluator.Evaluate()`로 그 채널의 경보 상태까지 같이 판정
5. **저장** — `SaveChangesAsync()`로 3~4단계 변경사항 커밋.
6. **장비 단위 집계** (`EquipmentStatusAggregator.UpdateAsync()`) — 채널 → 압축기 → 장비 순으로 "가장 심각한 상태"를 집계하고, 운전전류 임계값으로 운전/정지를 판정한다. 압축기·장비 수가 몇백 대 수준이라 매 사이클 전체를 다시 계산해도 부담 없다.

### PC-Link 통신 (Modules/Communication/Protocol/PcLinkClient.cs)

- 압축기 1대와 통신하는 전 과정(TCP 연결 → 명령 전송 → 응답 수신 → 파싱)을 담당하는 정적 클래스 하나.
- 읽는 레지스터, 국번, 포트가 전부 고정값(상수)이라 매번 조립할 필요 없이 같은 명령을 계속 보낸다.
- 체크섬은 실제로 계산한다 (`(문자열 바이트 합) mod 256`을 16진수로).
- 응답이 `OK`가 아니거나 체크섬이 안 맞으면 이유를 따지지 않고 전부 실패 처리한다.
- 레지스터 값은 16비트 2의 보수로 해석해서 영하 온도 같은 음수도 정확히 변환한다. 변환한 값은 원시값(raw int16) 그대로 반환하며, 소수점 스케일링은 여기서도 이후 어디서도 하지 않는다 — 프론트가 표시할 때 담당한다.
- **테스트 모드일 때는 아예 호출되지 않는다** — 실제 장비 네트워크 없이도 전체 파이프라인(통신상태 → 경보 판정 → 장비 집계)을 검증할 수 있게 하기 위함.

### 경보 판정 (Modules/Alarm/AlarmEvaluator.cs)

채널 하나의 `CompressorSensorCurrent`를 값이 갱신될 때마다 판정한다. 상태 전이:

```text
정상 --(범위 밖)--> 경보발생대기 --(AlarmDelaySeconds 경과)--> 경보발생
경보발생 --(범위 안)--> 정상복귀대기 --(AlarmClearDelaySeconds 경과)--> 정상
```

- **경보확인 기능은 없다** — 시스템 사양에 사용자 확인 절차를 두지 않기로 했다. 경보발생 상태는 값이 정상 범위로 돌아올 때까지 그대로 유지된다.
- 채널이 `Enabled=false`거나 `AlarmEnabled=false`면 판정 대신 `경보비활성화`로 표시된다.
- 히스테리시스는 없다 — 경계값 부근에서는 상태가 자주 바뀔 수 있다(의도된 단순화).
- 지연시간 계산을 위해 `CompressorSensorCurrent.PendingSince`(그 상태로 바뀐 시각)를 같이 저장한다.

### 장비 상태 집계 (Modules/Equipment/EquipmentStatusAggregator.cs)

- 경보는 "정상", 통신은 "연결됨"만 문제없는 상태다. 나머지는 전부 "문제없음이 아님"이라는 같은 의미이고, 여러 하위 상태가 섞여 있을 때 어느 걸 표시용으로 고를지는 우선순위를 나열한 `AggregateAlarm`/`AggregateCommunication`(`if (있으면) return 그 값;`을 순서대로 나열, 제네릭이나 점수 계산 없음)이 정한다.
- **채널 → 압축기**: 압축기 소속 채널 7개의 `AlarmStatus`를 `AggregateAlarm`에 넘겨 `Compressor.AlarmStatus`로.
- **압축기 → 장비**: 소속 압축기들의 `CommunicationStatus`/`AlarmStatus`를 각각 `AggregateCommunication`/`AggregateAlarm`에 넘겨 장비 필드로. `IsRunning`(bool)은 별도 판정 — `CommunicationStatus == 연결됨`인 압축기 중 CH07(운전전류)이 `Equipment.RunningCurrentThreshold`를 넘는 압축기가 하나라도 있으면 `true`.
- `RunningCurrentThreshold`가 설정 안 된 장비는 판정하지 않고 `IsRunning = false`로 처리한다(안전한 기본값).
- **통신이 끊긴 압축기는 마지막 값이 얼마였든 운전 판정에서 제외한다.** `CompressorSensorCurrent`는 통신 성공 시에만 갱신되므로, 통신이 끊기면 값이 그 자리에 얼어붙는다 — 이 조건이 없으면 "끊기기 직전 전류가 높았던 압축기"가 통신 두절 이후에도 계속 운전 중으로 잘못 집계된다. 실제로 압축기 2대를 통신 불가 상태로 만들고(마지막 값은 임계값보다 훨씬 높게 유지) 장비가 정지로 정확히 판정되는지 검증했다.

## 4. 데이터 모델 (Modules/Equipment/Models/)

```text
Equipment (장비)
  IsRunning, AlarmStatus, CommunicationStatus  ← 압축기 집계 결과 (관리자가 설정하는 Status와는 별개)
   │ 1
   │ N
Compressor (압축기) ── SequenceNo(장비 내 순번), IpAddress, MacAddress, CommunicationStatus, AlarmStatus
   │ 1                │ 1
   │ 7                │ 7
CompressorChannelSetting        CompressorSensorCurrent
(채널명/단위, 사용여부,           (채널별 현재값 — 폴링마다 덮어씀, 누적 안 됨)
 경보 상/하한, 지연시간,          + AlarmStatus, PendingSince (경보 판정용 상태)
 표시 소수점)
                                  │
                                  │ 1분마다 스냅샷
                                  ▼
                          CompressorMeasurement (Modules/Trend)
                          (Ch01~07 + IsRunning/HasAlarm/IsConnected(전부 bool) — 압축기 자신이 아니라
                           소속 Equipment의 집계 상태를 복사한 것. 계속 누적됨 — CompressorSensorCurrent와 달리 안 지워짐)
```

- `Equipment`, `Compressor`는 `Id` 기반 일반 기본키.
- `CompressorChannelSetting`, `CompressorSensorCurrent`는 압축기 1대당 정확히 7행(CH01~07)이 고정이라 `(CompressorId, ChannelNo)` 복합키를 쓴다 — 의미 없는 별도 일련번호를 만들지 않기 위함.
- `CompressorMeasurement`는 압축기 1대당 그 분(`MeasuredAt`)에 정확히 1행이라 `(CompressorId, MeasuredAt)` 복합키. 채널은 행이 아니라 컬럼(Ch01~07)으로 펼쳐서 저장한다(하루 데이터량을 7분의 1로 줄이기 위함).
- `CompressorSensorCurrent.Value`, `CompressorMeasurement.Ch01~07`은 전부 `short`(raw int16) 타입이다. TLC가 보내는 원시값을 가공 없이 그대로 저장하며, 표시용 소수점 자리수(`CompressorChannelSetting.DecimalPlaces`)는 나중에 프론트가 값을 화면에 표시할 때 쓰라고 남겨둔 값일 뿐, 백엔드 저장/계산에는 관여하지 않는다.
- `Compressor`는 의도적으로 필드가 적다. IP/포트/타임아웃 등 상당수는 시스템 공통값이거나 아직 필요하지 않아 뺐다. `SequenceNo`(장비 내 순번)는 예외로 추가됨 — 향후 경보 메시지에 "압축기 1번"처럼 표시하기 위한 용도(`Infrastructure/Seed/apply_compressor_sequence.sql`로 `ROW_NUMBER() OVER (PARTITION BY EquipmentId ORDER BY Id)` 계산해서 채움).
- `CompressorChannelSetting.ChannelName`/`Unit`(채널 한글명/단위)은 채널 번호로 결정되는 시스템 공통값이라 압축기마다 다르지 않지만, 경보 메시지 생성 시 한 행 조회만으로 필요한 정보를 다 얻도록 일부러 같이 저장해뒀다(`apply_channel_names.sql`로 채움).
- `Equipment.IsRunning`/`AlarmStatus`/`CommunicationStatus`는 관리자가 설정하는 `Equipment.Status`(운영/미운영 등)와 완전히 다른 개념이다 — 압축기 데이터로부터 매 폴링 사이클 자동 계산되는 실시간 파생값이다. `IsRunning`은 "운전/정지" 둘뿐이라 처음부터 enum이 아니라 `bool`이다.
- `AlarmStatus`(Modules/Alarm/Models)는 5가지 상태만 있다: 정상/경보발생대기/경보발생/정상복귀대기/경보비활성화. "경보확인"과 "경보해제"는 상태로 존재하지 않는다.
- `Equipment`는 `(BuildingName, Name)` 조합에 유니크 인덱스가 걸려있다 — 같은 시설동에 이름이 완전히 같은 장비 두 개는 등록할 수 없다(시도하면 DB가 에러로 거부). `compressor_seed.sql`이 이 텍스트 조합으로 압축기를 소속 장비에 연결하기 때문에(Id가 아니라 이름으로 매칭), 이 유니크 제약이 깨지면 그 매칭이 압축기 하나를 두 장비에 중복 연결하는 식으로 조용히 틀어질 수 있다. 실제 자산 목록도 동명 설비는 "1호기/2호기"처럼 구분해서 이 제약을 이미 만족한다(overview.md 4.1 참고).

## 5.1 트렌드 기록 (Modules/Trend/TrendRecordingService.cs)

`CompressorPollingService`와 완전히 독립된 별도 `BackgroundService`. 압축기와 직접 통신하지 않고, 이미 3초마다 갱신되고 있는 `CompressorSensorCurrent`를 **매분 정각(초=0)에 스냅샷 찍어 `CompressorMeasurement`에 그대로 복사**한다.

- 다음 정각까지 남은 시간을 매번 다시 계산해서 대기한다(고정 60초 대기가 아님) — 그래야 10:00:00, 10:01:00처럼 정확한 정각에 기록되고 오차가 누적되지 않는다. 기록 시각도 실행된 실제 시각이 아니라 의도된 정각 값을 그대로 쓴다.
- `MeasuredAt`은 UTC로 계산·저장한다. Npgsql이 `timestamptz` 컬럼에 UTC(offset 0)가 아닌 `DateTimeOffset`은 거부하기 때문이다. 한국은 UTC+9시(분 단위 오차 없음)라 정각 판단 자체엔 영향이 없다.
- 통신 이력이 한 번도 없는 압축기도 매분 행을 만든다(채널값은 NULL).
- `IsRunning`/`HasAlarm`/`IsConnected` 셋 다 압축기 개별이 아니라 **소속 장비의 집계 상태를 그대로 복사**한다(사용자 결정). 같은 장비의 압축기 여러 대는 이 세 값이 전부 동일하다.
- `HasAlarm`/`IsConnected`는 `Equipment.AlarmStatus`/`CommunicationStatus`(5단계/3단계 세부 상태)를 그대로 저장하지 않고 `!= 정상`/`== 연결됨` 여부만 boolean으로 남긴다 — 장비 단위에서는 "정상이냐 아니냐", "연결됐냐 아니냐"만 의미가 있다는 원칙(경보/통신은 이분법, [EquipmentStatusAggregator.cs](../Modules/Equipment/EquipmentStatusAggregator.cs) 참고)을 트렌드 기록에도 동일하게 적용한 것이다. 트렌드 화면에서 "왜 이 압축기 값이 그대로 유지되는지"(통신장애 때문인지)는 `IsConnected`로 구분한다.
- 데이터 사용량은 [Modules/Trend/README.md](../Modules/Trend/README.md)에 실측치로 정리되어 있다(하루 약 52MB, 1년 약 18.4GB, 압축기 244대 기준).

## 5. 조회 API (Modules/Equipment/Controllers/)

서비스/리포지토리 계층 없이 컨트롤러가 `AppDbContext`를 직접 조회한다. 이 규모(사용자 20명, 압축기 수백 대)에서는 매 요청 DB 직접 조회로 충분하다고 판단해서 별도 캐시를 두지 않았다.

아래 4개 컨트롤러(Equipments/Compressors/Trend/Utilization)는 전부 클래스 레벨에 `[Authorize]`가 붙어 있어, 로그인해서 받은 토큰 없이는 호출할 수 없다 (8장 참고).

| 엔드포인트 | 설명 |
|---|---|
| `GET /api/equipments` | 장비 전체 목록 |
| `GET /api/equipments/{id}` | 장비 단건 |
| `GET /api/equipments/{id}/compressors` | 해당 장비의 압축기 목록(통신/경보 상태 포함) |
| `GET /api/compressors` | 압축기 전체 목록(소속 장비명 조인 포함) |
| `GET /api/compressors/{id}/channels` | 해당 압축기의 CH01~07 현재값 |
| `GET /api/compressors/{id}/channel-settings` | 해당 압축기의 CH01~07 채널 설정(채널명/단위/경보 상하한/표시 소수점 자리수) |
| `GET /api/compressors/{id}/trend?date=yyyy-MM-dd` | 해당 압축기의 하루치 트렌드(1분 단위, 채널 전체 + 상태 스냅샷). `date` 생략 시 오늘(한국 시간) |
| `GET /api/equipments/{id}/utilization?from=yyyy-MM-dd&to=yyyy-MM-dd` | 지정 기간의 장비 가동률(%). `to` 생략 시 `from` 하루, 둘 다 생략 시 오늘 |
| `GET /api/summary` | 전체/운전중 장비 수, 전체/통신불량 압축기 수 집계 (실시간 현황 상단 카운트용) |
| `GET /api/events?since=&take=` | 이벤트 로그 조회 (실시간 현황 이벤트 피드용). `since` 없으면 최신순, 있으면 오래된순 |

enum(운영상태, 통신상태, 경보상태, 채널번호)은 전부 `"운영"`, `"연결됨"`, `"CH01"`처럼 사람이 읽을 수 있는 문자열로 응답에 나간다. 이 변환은 항상 DB에서 엔티티를 먼저 가져온 뒤(`ToListAsync()` 등) 메모리에서 처리한다 — EF Core가 SQL 안에서 enum 문자열 변환을 안정적으로 처리하지 못할 수 있어서다.

채널값(`/channels`의 `value`, `/trend`의 `ch01`~`ch07`)은 반대로 **가공하지 않고** TLC 원시값(raw int16) 그대로 내려간다 — 소수점 변환은 백엔드 어디에서도 하지 않고 프론트가 담당한다(사용자 결정). 경보 상/하한(`CompressorChannelSetting.LowerLimit/UpperLimit`)과 운전전류 임계값(`Equipment.RunningCurrentThreshold`)도 같은 raw 스케일로 저장되어 있어서, `AlarmEvaluator`/`EquipmentStatusAggregator`의 비교 로직도 raw 값끼리 순수 정수 비교만 한다.

> 참고: `EquipmentDto`에 `IsRunning`/`CommunicationStatus`/`AlarmStatus`가 전부 노출되어 있다(프론트 실시간 현황 화면 요구사항).

### 트렌드 조회(`/trend`)의 날짜 처리

`date`는 사용자가 생각하는 **한국 시간(KST) 기준 하루**로 해석한다. DB엔 UTC로 저장돼 있어서, 컨트롤러가 `날짜 00:00 KST ~ 다음날 00:00 KST` 구간을 UTC로 변환해서 조회한다. 이 변환에도 `TrendRecordingService`와 같은 제약이 적용된다 — Npgsql은 쿼리 파라미터로 넘기는 `DateTimeOffset`도 UTC(offset 0)만 받아서, `ToUniversalTime()`을 반드시 거쳐야 한다. 실제로 하루 경계(전날 23:59 / 당일 00:00 / 당일 23:59 / 다음날 00:00)를 데이터로 넣어서 정확히 그 날짜만 걸러지는지 검증했다.

## 6. 테스트 모드

실제 압축기 네트워크 없이 전체 파이프라인을 검증하기 위한 스위치. `appsettings.Development.json`의 `Communication:TestMode`를 `true`/`false`로 바꾸고 **앱을 재시작**하면 적용된다 (운영용 `appsettings.json`은 기본 `false`).

- 켜져 있으면: 모든 압축기가 항상 통신 성공한 것으로 처리, CH01~07에 `-200~1200` 랜덤값 생성
- 꺼져 있으면: 실제 `PcLinkClient`로 통신

## 8. 로그인 인증 (Modules/Auth, Modules/Logging)

Identity 프레임워크 전체는 쓰지 않고, JWT 발급 + `[Authorize]`만으로 최소하게 구성했다.

```text
POST /api/auth/login { username, password }
  │
  ▼
AuthController
  1. Users 테이블에서 username 조회 (IsActive=true인 것만)
  2. PasswordHasher<User>.VerifyHashedPassword로 비밀번호 검증
  3. 성공 시 EventLog(UserAccess)에 "로그인" 기록
  4. JwtTokenService가 서명된 JWT 발급 (12시간 만료, refresh 없음)
  ▼
응답: { token, username, role }
```

- `User`(Modules/Auth/Models): `Username`, `PasswordHash`, `Role`(시스템관리자/안전관리총괄자/안전관리책임자/안전관리원/일반관리자 — `시스템관리자`만 전체 권한, 나머지 4개는 현재 조회만 가능하고 권한 차이 없음), `IsActive`, 그리고 안전관리 담당자 인적사항(`FullName`, `Position`, `LegalTrainingDate`, `NextTrainingDate`, `Department`, `BackupPersonName`). 비상정지 권한은 사용자별 플래그가 아니라 나중에 역할 기준으로 일괄 적용할 예정이라(예: "안전관리책임자만 가능") `User`에 별도 필드를 두지 않는다.
- `UserEquipment`: 사용자-담당장비 다대다. `CompressorChannelSetting`과 같은 이유로 별도 Id 없이 `(UserId, EquipmentId)` 자체를 기본키로 쓴다.
- 비밀번호는 `Microsoft.AspNetCore.Identity`의 `PasswordHasher<T>` 클래스만 가져와 씀 (Identity 전체 프레임워크의 UserManager/SignInManager 등은 안 씀).
- 로그인 이후 요청은 `Authorization: Bearer {token}` 헤더로 인증한다. 기존 조회 컨트롤러 4개는 전부 `[Authorize]`가 붙어 토큰 없이는 호출 불가.
- `POST /api/auth/logout`은 서버가 토큰을 무효화하지는 않는다(JWT는 상태가 없어서 블랙리스트 없이는 그럴 수 없음) — 프론트가 토큰을 버리면 그걸로 로그아웃이고, 이 엔드포인트는 EventLog에 "로그아웃" 기록만 남긴다.
- `EventLog`(Modules/Logging): 카테고리별(`UserAccess`/`EmergencyStop`/`Communication`/`Alarm`/`System`) 단일 테이블. `EquipmentId`/`CompressorId`/`ChannelNo`(전부 nullable) 참조 필드가 있어서, `Message`(사람이 읽는 완성 문장)를 파싱하지 않고도 필터링·화면 이동이 가능하다. `UserAccess`(로그인/로그아웃)에 이어 `Alarm`/`Communication`도 구현되었다 — 자세한 내용은 아래 "경보/통신장애 이벤트 기록" 참고. 나머지(`EmergencyStop`/`System`)는 해당 기능을 만들 때 `EventLogger.LogAsync(...)` 호출을 추가하면 된다.
- 최초 관리자 계정(`admin`/`admin1234`)은 `Program.cs`에서 `Users` 테이블이 비어있을 때 자동 생성된다 (setup.md 11장 참고). 계정 잠금, 비밀번호 재설정, 사용자 관리 API는 20명 규모에 비해 과하다고 판단해 구현하지 않았다.

### 경보/통신장애 이벤트 기록 (Modules/Communication/CompressorPollingService.cs)

폴링마다가 아니라 **상태가 실제로 전이되는 순간에만** `EventLogger.LogAsync`를 호출한다 — 그렇지 않으면 244대 × 7채널을 3초마다 도는 구조상 이벤트가 감당 안 될 정도로 쌓인다.

- **경보**(`LogAlarmTransitionIfNeededAsync`): `AlarmEvaluator.Evaluate()` 호출 전후로 `AlarmStatus`를 비교해서, "발생"(경보발생대기→경보발생)과 "해제"(정상복귀대기→정상) 확정 순간만 기록한다. 중간 대기 상태는 기록하지 않는다.
- **통신장애**(`LogCommunicationAlarmAsync`): `HasCommunicationAlarm`이 `false`→`true`로 바뀌는 순간만 기록한다. 통신 상태(연결됨/끊김/재접속중) 자체의 전이나, 통신장애 경보의 복구는 기록하지 않는다(사용자 결정 — 너무 잦음).
- 메시지는 `"{지역} {시설동}의 {장비명}의 압축기 {SequenceNo}번 {ChannelName}값이 범위를 벗어났습니다"` 형태로 서버가 조립한다. 필요한 장비/압축기 정보(`GetCompressorContextAsync`)는 전이가 실제로 일어난 드문 순간에만 조회한다 — 매 폴링 사이클마다 미리 로드해두지 않는다.
- `AlarmDelaySeconds`/`AlarmClearDelaySeconds` 둘 다 기본값 30초(`CompressorChannelSetting.cs` 속성 기본값 + `apply_alarm_defaults.sql`)가 적용되어 있다. 채널값이 상/하한을 벗어나도(또는 정상 범위로 돌아와도) 30초 동안 그 상태가 **끊김 없이 계속 유지**되어야 확정되고, 중간에 단 한 번이라도 반대 상태로 바뀌면 대기가 취소되고 처음부터 다시 시작한다(`AlarmEvaluator`).
- **테스트 모드 관련 참고**: `GenerateTestValues()`는 매 사이클(3초)마다 완전히 독립적인 새 랜덤값을 뽑기 때문에, 30초(10사이클) 연속으로 같은 방향(상한초과/하한미만/정상범위)을 유지할 확률이 매우 낮다 — 실제로 초기화 후 40초 넘게 관찰해도 이벤트가 하나도 안 쌓이는 걸 확인했다. 실제 센서값은 값이 연속적으로 변하므로 이런 극단적인 경우가 없지만, **테스트 모드로 지연 기반 경보 확정을 눈으로 확인하려면 지연시간을 일시적으로 낮추거나(예: 3~6초) 별도 확인 방법이 필요**하다.

## 9. 아직 없는 것

이 프로젝트의 목표 기능 중 아래는 아직 구현되지 않았다 (설계는 [overview.md](overview.md)에 있음).

- **비상정지** (WRD로 D1805 비트 토글 — 정확한 비트 위치 미확정)
- **사용자 관리 API** (계정 생성/수정/역할 변경 — 현재는 최초 관리자만 자동 시드)
- **레포트 데이터 관리** (점검일지/운영일지/게시판 등)

지금 구조(단순 엔티티 + `BackgroundService` + 얇은 컨트롤러)가 위 기능들을 이어 붙이는 데 구조적인 걸림돌은 없어 보인다 — 특히 트렌드 기록은 `CompressorPollingService`와 같은 패턴(주기 실행 + DB 반영)을 재사용하면 된다.
