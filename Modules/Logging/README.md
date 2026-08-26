# Logging 모듈

이벤트 로그 모듈.

다음 4가지 이벤트를 단일 EventLog 테이블에 카테고리로 구분해 기록한다. 별도 로깅 프레임워크(Serilog 등)는 쓰지 않고 .NET 기본 `ILogger` + DB 저장만 사용한다.

- `UserAccess` — 사용자 접속(로그인/로그아웃)
- `EmergencyStop` — 비상정지 수행
- `Communication` — 압축기 통신 불능/복구 (상태가 실제로 바뀔 때만)
- `Alarm` — 경보 발생 (장비/압축기/채널 기준)
- `System` — 시스템 시작/오류

## 내부 구성

- `Models/EventLog.cs`, `Models/EventLogCategory.cs` — EventLog 엔티티/카테고리
- `EventLogger.cs` — 저장 헬퍼(`EventLogger.LogAsync(db, category, message, username)`), 서비스 계층 없이 정적 메서드로 단순하게 구현

**현재 구현된 것은 `UserAccess`(로그인/로그아웃) 뿐**이다 (`Modules/Auth/Controllers/AuthController.cs`에서 호출). 나머지 카테고리(EmergencyStop/Communication/Alarm/System)는 해당 기능을 만들 때 같은 헬퍼로 기록을 추가하면 된다.

이벤트 로그 조회 API(관리자용)는 아직 없음 — 필요해지면 추가.
