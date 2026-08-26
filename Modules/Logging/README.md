# Logging 모듈

이벤트 로그 모듈.

다음 4가지 이벤트를 단일 EventLog 테이블에 카테고리로 구분해 기록한다. 별도 로깅 프레임워크(Serilog 등)는 쓰지 않고 .NET 기본 `ILogger` + DB 저장만 사용한다.

- `UserAccess` — 사용자 접속(로그인/로그아웃)
- `EmergencyStop` — 비상정지 수행
- `Communication` — 압축기 통신 불능/복구 (상태가 실제로 바뀔 때만)
- `Alarm` — 경보 발생 (장비/압축기/채널 기준)
- `System` — 시스템 시작/오류

## 내부 구성 (예정)

- `Models/` — EventLog 엔티티
- `Services/` — EventLog 저장 헬퍼
- `Controllers/` — 이벤트 로그 조회 API (관리자용)
