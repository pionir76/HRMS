# Auth 모듈

사용자 및 권한 모듈.

- 로그인/로그아웃 (JWT Bearer 토큰, 만료 12시간, refresh 없음 — 만료되면 재로그인)
- 비밀번호는 `PasswordHasher<User>`(ASP.NET Core 표준 해시)로 암호화 저장. Identity 프레임워크 전체는 쓰지 않음
- 사용자 권한 3단계 (`UserRole`: 조회/운영/관리자) + 비상정지는 `CanEmergencyStop` 별도 bool 플래그
- 사용자 접속 로그는 `Modules/Logging`의 EventLog(UserAccess 카테고리)에 기록
- 계정 잠금(로그인 실패 다회 시 잠김)은 20명 규모에 비해 과하다고 판단해 **구현하지 않음**
- 자체 회원가입/사용자 관리 API는 없음. 최초 관리자 계정(`admin`/`admin1234`)은 앱 최초 실행 시 `Program.cs`에서 자동 시드되며, 이후 계정 추가는 DB에 직접 입력 (setup.md 참고)

## 내부 구성

- `Controllers/AuthController.cs` — `POST /api/auth/login`, `POST /api/auth/logout`
- `Services/JwtTokenService.cs` — 토큰 발급
- `Models/` — `User`, `UserRole`, 요청/응답 DTO

기존 조회 API(Equipments/Compressors/Trend/Utilization)는 전부 `[Authorize]`가 적용되어 로그인 없이는 호출할 수 없다.
