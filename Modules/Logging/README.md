# Logging 모듈

이벤트 로그 모듈.

다음 4가지 이벤트를 단일 EventLog 테이블에 카테고리로 구분해 기록한다. 별도 로깅 프레임워크(Serilog 등)는 쓰지 않고 .NET 기본 `ILogger` + DB 저장만 사용한다.

- `UserAccess` — 사용자 접속(로그인/로그아웃)
- `EmergencyStop` — 비상정지 수행
- `Communication` — 압축기 통신 장애 경보(`HasCommunicationAlarm`)가 **켜지는 순간만** 기록. 연결됨/끊김/재접속중 상태 전이 자체나 복구는 기록하지 않는다(사용자 결정 — 물량이 너무 잦아짐)
- `Alarm` — 채널 경보의 "발생"(**경보발생대기→경보발생**)과 "해제"(정상복귀대기→정상) 확정 순간만 기록. 중간 대기 상태는 기록하지 않는다. **`정상복귀대기→경보발생`(해제 지연 대기 중 값이 다시 범위를 벗어남)은 기록하지 않는다** — 해제된 적 없는 같은 경보의 연장이기 때문이다(아래 "중복 기록 버그" 참고)
- `System` — 시스템 시작/오류. 현재는 `Program.cs`에서 앱 시작 시 한 번(TestMode 여부·장비/압축기 수 포함)만 기록하고, 오류 쪽은 아직 없음(전역 예외 처리 미들웨어 미구현)

물량 통제 원칙: 폴링 주기(3초)가 아니라 **상태가 실제로 전이될 때만** 기록한다. 그래도 경보 지연시간(`AlarmDelaySeconds`/`AlarmClearDelaySeconds`)이 설정 안 된 채로 값이 자주 요동치면 이벤트가 많이 쌓일 수 있다 — 실사용 시 지연시간을 적절히 설정해야 한다.

## 경보 중복 기록 버그 (2026-09-16 수정)

`AlarmEvaluator`의 상태 머신에는 `경보발생 → 정상복귀대기 → 경보발생` 경로가 있다(해제 지연을 기다리는 중 값이 다시 범위를 벗어나면 즉시 경보발생으로 되돌아간다). 기록 조건이 `previous != 경보발생 && current == 경보발생`이어서 **이 경로까지 "새 경보 발생"으로 잡혔다.**

증상: 해제 지연이 길고(예: 300초) 값이 경계에서 흔들리는 채널에서, 해제된 적 없는 **같은 경보가 수십 초 간격으로 수천 건** 쌓였다. 실제로 장비 39의 CH05는 하루에 이탈 1,557건 / **복귀 0건**이었다(복귀는 300초를 연속으로 정상 범위에 머물러야 찍히는데 그 전에 계속 재이탈).

수정: 발생 판정을 `previous == 경보발생대기 && current == 경보발생`으로 좁혔다. 경보발생 진입 경로는 경보발생대기와 정상복귀대기 둘뿐이라 이 조건이면 "진짜 신규 발생"만 남는다.

주의할 점:
- **TestMode만의 문제가 아니었다.** 원인은 "해제 지연이 길고 값이 경계에서 진동한다"이고, 이는 실제 현장에서도 성립하는 조건이다.
- 이 버그로 쌓여 있던 로그는 **2026-09-16에 `EventLogs` 전체를 비우면서 함께 삭제**했다(사용자 결정, 120,256건). 그래서 그 이전 이벤트 이력(로그인·결재 포함)은 조회되지 않는다.
- 프론트가 "연속된 같은 경보를 하나의 구간으로 묶어 표시"하는 로직을 갖고 있는데, 수정 후에는 발생 1건 ↔ 해제 1건 쌍으로 기록되므로 구간의 끝을 해제 이벤트로 닫는 방식이 정상 동작한다.

## 내부 구성

- `Models/EventLog.cs` — `Category`, `Message`(표시용 한글 문장), `Username`, 참조 필드 `EquipmentId`/`CompressorId`/`ChannelNo`, 그리고 경보 상세 스냅샷 `Value`/`LowerLimit`/`UpperLimit`/`DecimalPlaces`/`Unit`(전부 nullable — 카테고리에 따라 해당 없는 것도 있음). 참조 필드는 FK 제약 없이 단순 정수 컬럼으로 둔다 — 로그는 원본 레코드가 삭제돼도 남아야 하는 이력이라서다.
- `Models/EventLogCategory.cs` — 카테고리
- `Models/EventLogDto.cs` — 조회 API 응답용 DTO(enum들을 문자열로 변환) + `ToDto()` 확장 메서드. 필드가 13개라 컨트롤러마다 따로 매핑하면 한쪽만 고치고 지나가기 쉬워서 변환을 한 곳에 모아뒀다.
- `EventLogger.cs` — 저장 헬퍼. 일반 이벤트는 `LogAsync(db, category, message, username, equipmentId, compressorId, channelNo)`, 경보는 상세 스냅샷을 함께 남기는 `LogAlarmAsync(db, message, equipmentId, compressorId, channelNo, value, setting)`을 쓴다(일반 메서드에 파라미터를 더 붙이면 인자가 12개가 되어 호출부가 읽기 어려워진다). 서비스 계층 없이 정적 메서드로 단순하게 구현
- `Controllers/EventLogsController.cs` — 실시간 피드용 `GET /api/events?since=&take=`
- `Controllers/EquipmentEventsController.cs` — 자료 조회 화면용 `GET /api/equipments/{id}/events?date=&category=`. 경로는 장비 하위지만 도메인이 로깅이라 여기 둔다(`Modules/Operation`의 `UtilizationController`가 `/api/equipments/{id}/utilization`을 담당하는 것과 같은 방식)

## 경보 상세 스냅샷 (2026-09-16 추가)

자료 조회 화면에서 이벤트를 클릭하면 "저압 11.78 MPa (기준 0.00~10.00)"처럼 세부를 보여줘야 해서, 경보 확정 순간의 raw 측정값과 그 시점 채널 설정(정상 범위·단위·소수점)을 `EventLog`에 함께 저장한다.

- **임계값·단위·소수점까지 스냅샷으로 박아두는 이유**: 채널 설정은 나중에 바뀔 수 있는데, 과거 경보 이력을 "지금 설정" 기준으로 표시하면 그때 무슨 일이 있었는지가 왜곡된다(결재자 이름을 승인 시점 값으로 남기는 것과 같은 원칙).
- `Value`/`LowerLimit`/`UpperLimit`은 채널값과 같은 **raw int16** 스케일이다 — 백엔드는 소수점 처리를 하지 않고, `DecimalPlaces`는 프론트 표시용 힌트다.
- **2026-09-16 이전에 쌓인 경보 이벤트(약 11.6만 건)는 이 필드들이 `null`** 이다(소급 기록 불가). 프론트는 값이 없는 경우를 처리해야 한다.
- 경보 외 카테고리(로그인/통신/시스템/결재)는 이 필드를 채우지 않는다.
- `Controllers/EventLogsController.cs` — `GET /api/events?since=&take=` 조회 API

**메시지는 서버에서 완성된 한글 문장으로 조립해서 저장한다** (예: "A지구 부식내구동의 염수챔버 1호기의 압축기 1번 전압값이 범위를 벗어났습니다"). 코드/구조화 데이터만 저장하고 프론트가 문장을 조립하는 방식도 검토했으나, 참조 필드로 이미 필터링/네비게이션은 가능하니 문장 조립까지 서버가 맡는 게 더 단순하다고 판단했다 — 조립 로직은 `Modules/Communication/CompressorPollingService.cs`(경보/통신장애 이벤트가 발생하는 지점)에 있다.

`UserAccess`/`Alarm`/`Communication`/`System`(현재는 시작 이벤트 하나만)은 구현됨. `EmergencyStop`은 비상정지 기능을 만들 때 같은 헬퍼로 추가하면 된다.

이벤트 로그 조회 API는 `GET /api/events`로 구현됨(로그인한 사용자면 누구나 조회 가능 — 관리자로 한정하지 않음). `since`/`take` 파라미터로 폴링 방식 조회를 지원한다(api-manual.md 9번 참고).
