# Logging 모듈

이벤트 로그 모듈.

다음 4가지 이벤트를 단일 EventLog 테이블에 카테고리로 구분해 기록한다. 별도 로깅 프레임워크(Serilog 등)는 쓰지 않고 .NET 기본 `ILogger` + DB 저장만 사용한다.

- `UserAccess` — 사용자 접속(로그인/로그아웃)
- `EmergencyStop` — 비상정지 수행
- `Communication` — 압축기 통신 장애 경보(`HasCommunicationAlarm`)가 **켜지는 순간만** 기록. 연결됨/끊김/재접속중 상태 전이 자체나 복구는 기록하지 않는다(사용자 결정 — 물량이 너무 잦아짐)
- `Alarm` — 채널 경보의 "발생"(경보발생대기→경보발생)과 "해제"(정상복귀대기→정상) 확정 순간만 기록. 중간 대기 상태는 기록하지 않는다
- `System` — 시스템 시작/오류. 현재는 `Program.cs`에서 앱 시작 시 한 번(TestMode 여부·장비/압축기 수 포함)만 기록하고, 오류 쪽은 아직 없음(전역 예외 처리 미들웨어 미구현)

물량 통제 원칙: 폴링 주기(3초)가 아니라 **상태가 실제로 전이될 때만** 기록한다. 그래도 경보 지연시간(`AlarmDelaySeconds`/`AlarmClearDelaySeconds`)이 설정 안 된 채로 값이 자주 요동치면 이벤트가 많이 쌓일 수 있다 — 실사용 시 지연시간을 적절히 설정해야 한다.

## 내부 구성

- `Models/EventLog.cs` — `Category`, `Message`(표시용 한글 문장), `Username`, 그리고 참조 필드 `EquipmentId`/`CompressorId`/`ChannelNo`(전부 nullable — 카테고리에 따라 해당 없는 것도 있음). 참조 필드는 FK 제약 없이 단순 정수 컬럼으로 둔다 — 로그는 원본 레코드가 삭제돼도 남아야 하는 이력이라서다.
- `Models/EventLogCategory.cs` — 카테고리
- `Models/EventLogDto.cs` — 조회 API 응답용 DTO(enum들을 문자열로 변환)
- `EventLogger.cs` — 저장 헬퍼(`EventLogger.LogAsync(db, category, message, username, equipmentId, compressorId, channelNo)`), 서비스 계층 없이 정적 메서드로 단순하게 구현
- `Controllers/EventLogsController.cs` — `GET /api/events?since=&take=` 조회 API

**메시지는 서버에서 완성된 한글 문장으로 조립해서 저장한다** (예: "A지구 부식내구동의 염수챔버 1호기의 압축기 1번 전압값이 범위를 벗어났습니다"). 코드/구조화 데이터만 저장하고 프론트가 문장을 조립하는 방식도 검토했으나, 참조 필드로 이미 필터링/네비게이션은 가능하니 문장 조립까지 서버가 맡는 게 더 단순하다고 판단했다 — 조립 로직은 `Modules/Communication/CompressorPollingService.cs`(경보/통신장애 이벤트가 발생하는 지점)에 있다.

`UserAccess`/`Alarm`/`Communication`/`System`(현재는 시작 이벤트 하나만)은 구현됨. `EmergencyStop`은 비상정지 기능을 만들 때 같은 헬퍼로 추가하면 된다.

이벤트 로그 조회 API는 `GET /api/events`로 구현됨(로그인한 사용자면 누구나 조회 가능 — 관리자로 한정하지 않음). `since`/`take` 파라미터로 폴링 방식 조회를 지원한다(api-manual.md 9번 참고).
