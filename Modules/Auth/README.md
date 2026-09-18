# Auth 모듈

사용자 및 권한 모듈.

- 로그인/로그아웃 (JWT Bearer 토큰, 만료 12시간, refresh 없음 — 만료되면 재로그인)
- 비밀번호는 `PasswordHasher<User>`(ASP.NET Core 표준 해시)로 암호화 저장. Identity 프레임워크 전체는 쓰지 않음
- 사용자 역할 5종 (`UserRole`: 시스템관리자/안전관리총괄자/안전관리책임자/안전관리원/일반관리원). `시스템관리자`만 전체 권한, 나머지 4개는 현재 전부 조회만 가능하고 역할 간 권한 차이는 없음(추후 보고서 결재 기능에서 차이가 생길 예정, overview.md 6장)
- 비상정지 권한은 사용자별 플래그가 아니라 **역할 기준으로 일괄 적용**할 예정이다(예: "안전관리책임자만 가능"). 어느 역할에 부여할지는 아직 미정(overview.md 12장)이라 지금은 관련 코드/필드가 없다.
- 사용자 접속 로그는 `Modules/Logging`의 EventLog(UserAccess 카테고리)에 기록
- 계정 잠금(로그인 실패 다회 시 잠김)은 20명 규모에 비해 과하다고 판단해 **구현하지 않음**
- 자체 회원가입은 없다. 최초 관리자 계정(`admin`/`admin1234`)은 앱 최초 실행 시 `Program.cs`에서 자동 시드되고, **이후 계정 등록·수정·비활성화는 조직관리 API(`Modules/Organization`, `/api/users`)로 한다**(2026-09-17). 이 모듈은 엔티티와 로그인만 담당한다.
- `User`는 로그인 정보 외에 안전관리 담당자 인적사항(성명/소속팀/직위/연락처1·2/대직자/선임여부/법정교육일/차기교육일)도 같이 관리한다. `대직자`는 시스템 계정이 아닌 이름 텍스트로만 기록한다. 조직관리 화면의 **담당업무 = `Role`** 이다(결재 권한과 같은 값).
- 역할명 `일반관리자`는 2026-09-17에 `일반관리원`으로 바뀌었다(enum이 순번으로 저장돼 DB 변경 없음).
- `UserEquipment`로 사용자-담당장비를 다대다로 연결한다(한 사용자가 여러 장비를, 한 장비를 여러 사용자가 담당 가능).

## 내부 구성

- `Controllers/AuthController.cs` — `POST /api/auth/login`, `POST /api/auth/logout`
- `Services/JwtTokenService.cs` — 토큰 발급
- `Models/` — `User`, `UserRole`, `UserEquipment`, 요청/응답 DTO
- `CurrentUser.cs` — JWT 클레임에서 로그인 사용자 id/역할을 꺼내는 `ClaimsPrincipal` 확장 메서드(`GetUserId`/`IsSystemAdmin`/`TryGetUser`). 같은 파싱 코드가 컨트롤러 10여 곳에 복사돼 있던 것을 여기로 모았다(2026-09-18)

기존 조회 API(Equipments/Compressors/Trend/Utilization)는 전부 `[Authorize]`가 적용되어 로그인 없이는 호출할 수 없다.
