# Auth 모듈

사용자 및 권한 모듈.

- 로그인/로그아웃 (JWT Bearer 토큰, 만료 12시간, refresh 없음 — 만료되면 재로그인)
- 비밀번호는 `PasswordHasher<User>`(ASP.NET Core 표준 해시)로 암호화 저장. Identity 프레임워크 전체는 쓰지 않음
- 사용자 역할 5종 (`UserRole`: 시스템관리자/안전관리총괄자/안전관리책임자/안전관리원/일반관리자). `시스템관리자`만 전체 권한, 나머지 4개는 현재 전부 조회만 가능하고 역할 간 권한 차이는 없음(추후 보고서 결재 기능에서 차이가 생길 예정, overview.md 6장)
- 비상정지 권한은 사용자별 플래그가 아니라 **역할 기준으로 일괄 적용**할 예정이다(예: "안전관리책임자만 가능"). 어느 역할에 부여할지는 아직 미정(overview.md 12장)이라 지금은 관련 코드/필드가 없다.
- 사용자 접속 로그는 `Modules/Logging`의 EventLog(UserAccess 카테고리)에 기록
- 계정 잠금(로그인 실패 다회 시 잠김)은 20명 규모에 비해 과하다고 판단해 **구현하지 않음**
- 자체 회원가입/사용자 관리 API는 없음. 최초 관리자 계정(`admin`/`admin1234`)은 앱 최초 실행 시 `Program.cs`에서 자동 시드되며, 이후 계정 추가는 DB에 직접 입력 (setup.md 참고)
- `User`는 로그인 정보 외에 안전관리 담당자 인적사항(성명/직책/법정교육일/차기교육일/부서/대직자)도 같이 관리한다(overview.md 6.3). `대직자`는 시스템 계정이 아닌 이름 텍스트로만 기록한다.
- `UserEquipment`로 사용자-담당장비를 다대다로 연결한다(한 사용자가 여러 장비를, 한 장비를 여러 사용자가 담당 가능).

## 내부 구성

- `Controllers/AuthController.cs` — `POST /api/auth/login`, `POST /api/auth/logout`
- `Services/JwtTokenService.cs` — 토큰 발급
- `Models/` — `User`, `UserRole`, `UserEquipment`, 요청/응답 DTO

기존 조회 API(Equipments/Compressors/Trend/Utilization)는 전부 `[Authorize]`가 적용되어 로그인 없이는 호출할 수 없다.
