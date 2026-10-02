# HRMS 설치·배포 가이드

HRMS(냉동기 모니터링 시스템)를 서버에 설치하고, 발주처 테스트를 거쳐 남양연구소에 납품하기까지의 절차를 정리한 문서다.

## 납품 과정

**같은 서버 한 대가 세 단계를 거쳐 현장에 들어간다.**

| 단계 | 장소 | 네트워크 | 접속하는 사람 | 운영 모드 | 이 문서 |
|---|---|---|---|---|---|
| 1. 개발 | 개발자 PC | 로컬(`http://localhost:80`) | 개발자 | 테스트 모드 | 5부 |
| 2. 테스트 | 회사 고정 IP 회선 + 신규 서버 | **인터넷**(`http://59.16.212.237/`) | **발주처(현대)가 외부에서** | **테스트 모드**(모의값) | 1부 → 2부 |
| 3. 납품 | 남양연구소 | **폐쇄망**(IP는 현장에서 바뀜) | 연구소 내부 사용자 | **실제 모드**(현장 TLC 182대) | 3부 |

서버 구축(1부)은 테스트 서버에서 **한 번만** 한다. 납품 때는 그 서버를 **초기화해서 그대로 옮긴다**(3부).

> 1부와 3부의 절차는 2026-10-02 개발 PC에서 **그대로 리허설해서 확인했다** — 게시 → 스키마 SQL → 장비 시드 → 운영 모드 기동 → 상태·로그인·기동 이벤트 확인, 그리고 납품 초기화 스크립트까지. 문서에 적힌 숫자(장비 158대, 수집 대상 223대 등)는 그때의 실제 결과다.

**목차**

- 1부. 서버 구축 — [1. 한눈에 보기](#1-한눈에-보기) · [2. 서버 조건](#2-서버-조건) · [3. 배포 파일 만들기](#3-개발-pc에서-배포-파일-만들기) · [4. PostgreSQL 설치](#4-postgresql-설치) · [5. DB·계정](#5-db계정-만들기) · [6. 스키마](#6-db-스키마-만들기) · [7. 장비 데이터](#7-장비-데이터-넣기) · [8. 프로그램 배치와 설정](#8-프로그램-배치와-설정) · [9. 서비스 등록](#9-windows-서비스-등록) · [10. 첫 로그인](#10-첫-로그인과-관리자-비밀번호)
- 2부. 테스트 서버 운영 — [11. 테스트 서버 운영](#11-테스트-서버-운영-회사-고정-ip)
- 3부. 납품 — [12. 납품(현장 이전)](#12-납품-현장-이전)
- 4부. 운영·유지보수 — [13. 주의 사항](#13-주의-사항) · [14. 백업](#14-백업) · [15. 업데이트](#15-새-버전으로-업데이트) · [16. 로그](#16-로그-위치) · [17. 문제 해결](#17-문제-해결)
- 5부. 개발 PC 환경 — [18. 개발 DB](#18-개발-db-준비) · [19. 테스트 모드](#19-테스트-모드) · [20. 실기기 통신 테스트](#20-실기기-통신-테스트) · [21. 샘플 데이터](#21-테스트용-샘플-데이터) · [22. 장비 자료 갱신](#22-장비-자료-갱신과-원본-대조-기록) · [23. 개발 팁](#23-개발-팁)
- [부록 A. 설정 항목](#부록-a-설정-항목-appsettingsjson) · [부록 B. psql 기본 명령](#부록-b-psql-기본-명령)

---

# 1부. 서버 구축

테스트 서버(회사)에서 한 번 한다.

## 1. 한눈에 보기

**빌드는 개발 PC에서 끝내고 결과물만 가져가는** 방식이다. 서버에는 .NET SDK도, `dotnet ef` 도구도 필요 없고, 서버에서 인터넷으로 패키지를 내려받을 일도 없다(현장은 폐쇄망이다).

**화면과 API를 `http://서버주소/`(80번 포트) 하나로 서비스한다.** 백엔드가 프론트 빌드 파일(`C:\HRMS\wwwroot`)까지 직접 내보내므로 별도 웹 서버(IIS 등)가 필요 없고, 화면과 API가 같은 주소라 CORS 설정도 필요 없다. 서버 IP가 바뀌어도(테스트 → 현장) 설정을 고칠 필요가 없다.

```text
http://서버주소/            → 프론트 화면 (C:\HRMS\wwwroot\index.html)
http://서버주소/api/...     → 백엔드 API
http://서버주소/health      → 상태 확인 (로그인 불필요)
```

```text
[개발 PC]                                     [서버]
 3. 실행 파일 게시(.NET 포함)    ─┐            4. PostgreSQL 17 설치
    DB 스키마 SQL                │             5. DB·계정 만들기
    장비 시드 SQL · 확인/초기화 SQL ├─ USB 등 ─▶  6. 스키마 SQL 실행
    프론트 빌드 결과(프론트 담당)  │             7. 장비 시드 SQL 실행
    PostgreSQL 설치 파일         ─┘            8. 프로그램·프론트 배치 + appsettings.json
                                                9. Windows 서비스 등록
                                               10. 첫 로그인 → 관리자 비밀번호 변경
```

**DB는 서버에서 새로 만든다.** 개발 PC의 DB는 복사해 가지 않는다 — 테스트 계정 51명, 결재 샘플 데이터, 테스트 일지까지 같이 넘어가기 때문이다.

## 2. 서버 조건

| 항목 | 기준 | 비고 |
|---|---|---|
| OS | Windows Server 또는 Windows 10/11 (64비트) | |
| 디스크 | DB 드라이브에 **여유 50GB 이상** 권장 | 트렌드 기록이 하루 약 37MB, 1년 약 13.4GB 쌓인다. 아직 자동 정리 기능이 없다(13장) |
| 시계 | **시간이 정확해야 한다** | 매일 자정의 운전일지·점검일지 자동 기록, 매분 트렌드 기록이 서버 시계를 기준으로 돈다. 시간대 설정과 무관하게 한국시간으로 계산한다. 현장(폐쇄망)에서는 인터넷 시간 동기화가 안 된다(12.3) |
| 전원·절전 | **절전 모드 끄기** | 서버가 잠들면 수집이 멈춘다 |
| 권한 | 관리자 권한 PowerShell | 서비스 등록·방화벽 설정에 필요 |
| 설치 폴더 | **경로에 한글을 쓰지 않는다**(예: `C:\HRMS`, `C:\HRMS_setup`) | psql이 한글 경로의 파일을 읽지 못하는 경우가 있다(리허설에서 실제 발생) |
| 80번 포트 | 다른 프로그램(IIS 등)이 쓰고 있지 않아야 한다 | 8.3에서 확인 |

## 3. 개발 PC에서 배포 파일 만들기

개발 PC의 저장소 루트(`HRMS` 폴더)에서 진행한다. 결과물을 모을 폴더는 `C:\HRMS_setup`으로 가정한다.

### 3.1 실행 파일 게시

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o C:\HRMS_setup\HRMS
Remove-Item C:\HRMS_setup\HRMS\appsettings.Development.json
```

- `--self-contained`라서 **.NET 런타임이 함께 들어간다.** 서버에 .NET을 따로 설치할 필요가 없다(약 114MB, 파일 약 350개).
- 게시 결과에 **`appsettings.Development.json`이 딸려 나오므로 반드시 지운다.** 개발 PC용 설정(개발 DB 비밀번호, 개발용 JWT 키)이 들어 있다.

### 3.2 DB 스키마 SQL 만들기

```powershell
dotnet ef migrations script --idempotent -o C:\HRMS_setup\hrms_schema.sql
```

- 테이블을 만드는 SQL이다(약 22KB). `--idempotent`라서 **이미 적용된 부분은 건너뛴다** — 같은 파일을 두 번 실행해도 안전하고, 업데이트 때도 같은 방식으로 만든 파일을 그냥 실행하면 된다(15장).
- 개발 PC에 `dotnet ef` 도구가 있어야 한다(18장).

### 3.3 나머지 파일

| 가져갈 것 | 어디서 | 쓰는 곳 |
|---|---|---|
| `Infrastructure\Seed\seed_from_csv.sql` | 저장소 | 7장 장비 데이터, 12장 납품 초기화 |
| `Infrastructure\Setup\check_install.sql` | 저장소 | 설치 확인(6·7·11·12장) |
| `Infrastructure\Setup\reset_for_delivery.sql` | 저장소 | 12장 납품 초기화 |
| `Infrastructure\Compressors.csv` | 저장소 | 12.3 현장 TLC 네트워크 확인 |
| **프론트 빌드 결과**(`index.html`과 js·css 등) | **프론트 담당** | 8.1 |
| PostgreSQL 17 Windows 설치 파일 | https://www.postgresql.org/download/windows/ (EDB installer, **17.x**) | 4장 |

```text
C:\HRMS_setup\
 ├─ HRMS\                    (게시 결과 — appsettings.Development.json은 지운 상태)
 ├─ frontend\                (프론트 빌드 결과 — 이 폴더 바로 아래에 index.html)
 ├─ hrms_schema.sql
 ├─ seed_from_csv.sql
 ├─ check_install.sql
 ├─ reset_for_delivery.sql   (seed_from_csv.sql과 같은 폴더에 있어야 한다)
 ├─ Compressors.csv
 └─ postgresql-17.x-windows-x64.exe
```

이 폴더째로 서버의 같은 경로(`C:\HRMS_setup`)에 복사한다.

## 4. PostgreSQL 설치

1. 설치 파일을 실행한다. **버전 17.x**를 쓴다(18은 아직 Npgsql 드라이버 호환 검증이 덜 됐다).
2. 설치 마법사:
    - **Installation Directory**: 기본값(`C:\Program Files\PostgreSQL\17`)
    - **Select Components**: `PostgreSQL Server`, `pgAdmin 4`, `Command Line Tools` 체크. `Stack Builder`는 해제
    - **Data Directory**: 기본값. 데이터 전용 드라이브가 있으면 그쪽(예: `D:\PostgreSQL\data`) — 8.2 `StorageDrive`와 맞춘다
    - **Password**: `postgres` 관리자 비밀번호 — **반드시 기록**
    - **Port**: `5432` / **Locale**: 기본값
    - 마지막의 Stack Builder 실행은 해제하고 닫는다.
3. 확인: `Get-Service -Name postgresql*` → `Running`

## 5. DB·계정 만들기

앱은 `postgres` 관리자 계정이 아니라 **전용 계정 `hrms_app`**으로 접속한다. `postgres비밀번호`와 `hrms_app비밀번호`를 실제 값으로 바꿔 실행한다. `hrms_app` 비밀번호는 **개발 PC의 `1234`를 쓰지 말고 새로 정해 기록해 둔다.**

```powershell
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$env:PGPASSWORD = "postgres비밀번호"

& $psql -h localhost -U postgres -c "CREATE DATABASE hrms;"
& $psql -h localhost -U postgres -c "CREATE USER hrms_app WITH PASSWORD 'hrms_app비밀번호';"
& $psql -h localhost -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE hrms TO hrms_app;"

# PostgreSQL 15부터 public 스키마 기본 권한이 제한되어 따로 준다
& $psql -h localhost -U postgres -d hrms -c "GRANT ALL ON SCHEMA public TO hrms_app;"
& $psql -h localhost -U postgres -d hrms -c "ALTER SCHEMA public OWNER TO hrms_app;"

# 이후 단계는 hrms_app으로
$env:PGPASSWORD = "hrms_app비밀번호"
& $psql -h localhost -U hrms_app -d hrms -c "SELECT current_database(), current_user;"   # hrms | hrms_app
```

> 이후 psql 명령은 이 `$psql`·`$env:PGPASSWORD`(hrms_app)가 설정된 같은 창에서 실행한다고 가정한다.

> **확인 쿼리는 `check_install.sql` 파일로 실행한다.** Windows PowerShell 5.1은 `psql -c '...'`로 넘기는 쿼리 안의 큰따옴표를 지워 버려서 `"Equipments"` 같은 테이블 이름이 소문자로 바뀌고 실패한다(2026-10-02 확인). 읽기만 하는 스크립트라 몇 번을 실행해도 되고, 아직 진행하지 않은 단계의 항목은 0으로 나온다.
>
> ```powershell
> & $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\check_install.sql
> ```

## 6. DB 스키마 만들기

```powershell
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\hrms_schema.sql
```

확인 — `check_install.sql`의 **[스키마]** 항목에 마이그레이션 3개(`InitialCreate`, `AddEventLogIndexes`, `AddEmergencyStopStatus`)가 나오면 정상. **앱은 스키마를 자동으로 만들지 않는다.**

## 7. 장비 데이터 넣기

```powershell
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\seed_from_csv.sql
```

확인 — `check_install.sql`의 **[장비 데이터]**가 `158 / 246 / 1722`, 운영 장비 `149` / 수집 대상 `223`이면 정상.

> **⚠️ 이 SQL을 단독으로 다시 실행하지 않는다.** 장비·압축기를 **지우고 다시 넣는** 스크립트라, 트렌드·이벤트·일지·검사이력·**담당 장비 지정**·장비 사진이 전부 삭제된다. 납품 때 초기화가 필요하면 12장의 `reset_for_delivery.sql`을 쓴다(담당 장비는 보존된다). 전체가 한 트랜잭션이라 중간에 실패하면 아무것도 바뀌지 않는다.

들어가는 값:

| 항목 | 값 |
|---|---|
| 장비 | 158대. **운영 149대**, 미운영 9대(A지구 PDI 1동 실차환경챔버 #1, C지구 수소충전소 냉동기 8대) |
| 압축기 | 246대(장비당 1~6대). 운영 장비 소속이면서 IP가 있는 **223대가 수집 대상** |
| 채널 설정 | 압축기마다 CH01~07. 상한 1000·하한 0(raw), 경보 발생/해제 지연 30초. LG 냉동기 42대는 CH03·CH06이 "사용 안 함" |
| 운전전류 기준값 | 전 장비 raw `100`(= 10.0A) |

경보 기준값(상·하한)은 **임시 기본값**이다. 실제 운영값은 현장에서 장비관리 화면으로 장비별로 맞춘다.

## 8. 프로그램 배치와 설정

### 8.1 배치

```powershell
robocopy C:\HRMS_setup\HRMS C:\HRMS /E
robocopy C:\HRMS_setup\frontend C:\HRMS\wwwroot /E
Test-Path C:\HRMS\wwwroot\index.html      # True여야 한다
```

- 실행 파일은 `C:\HRMS\HRMS.exe`, 화면 파일은 `C:\HRMS\wwwroot\index.html`이다. `wwwroot` **바로 아래**에 `index.html`이 있어야 한다.
- `wwwroot`는 **서비스가 시작할 때** 읽는다. 서비스가 이미 돌고 있을 때 처음 넣었다면 `Restart-Service HRMS`.
- **프론트의 API 주소**: 프론트 `common\js\config.js`는 접속한 주소를 보고 API 주소를 정한다(2026-10-02 프론트 수정) — `localhost`로 열면 `http://localhost:80`, 그 밖의 주소(테스트·현장 IP)로 열면 같은 주소. 그래서 IP가 바뀌어도 고칠 필요가 없다. 예전 빌드처럼 `apiBaseUrl: "https://localhost:7253"` 같은 고정 주소가 들어 있다면 `""`(빈 문자열)로 고친다 — 그대로 두면 외부 PC에서 화면은 뜨지만 로그인부터 실패한다.

### 8.2 `appsettings.json` 설정

`C:\HRMS\appsettings.json`을 메모장으로 열어 아래 항목을 채운다. **나머지 항목은 그대로 둔다.**

```json
{
  "Urls": "http://0.0.0.0:80",

  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=hrms;Username=hrms_app;Password=hrms_app비밀번호"
  },

  "Jwt": {
    "Key": "아래 명령으로 만든 무작위 문자열",
    "Issuer": "HRMS"
  },

  "Cors": {
    "AllowedOrigins": []
  },

  "Communication": {
    "TestMode": true,
    "TimeoutMs": 1500
  },

  "SystemStatus": {
    "StorageDrive": "C:\\"
  }
}
```

| 항목 | 넣을 값 | 비고 |
|---|---|---|
| `Urls` | `http://0.0.0.0:80` | **새로 추가하는 줄**이다. 없으면 서버 자신에서만, 5000번으로 뜬다(다른 PC에서 접속 불가) |
| `ConnectionStrings:Default` | 5장의 `hrms_app` 비밀번호 | **비어 있으면 서비스가 기동하지 않는다**(의도된 동작) |
| `Jwt:Key` | 무작위 문자열 32바이트 이상 | **비어 있거나 짧으면 기동하지 않는다.** 개발 PC의 키를 쓰지 않는다 |
| `Cors:AllowedOrigins` | `[]` 그대로 | 화면과 API가 같은 주소라 필요 없다 |
| **`Communication:TestMode`** | **테스트 서버: `true`** / **납품 후: `false`** | 테스트 서버에는 현장 TLC가 없어서 실제 모드로 두면 223대가 전부 "끊김"이 된다(11.1). 납품 때 `false`로 바꾼다(12.1) |
| `SystemStatus:StorageDrive` | PostgreSQL 데이터 폴더가 있는 드라이브 | 대시보드의 저장소 사용량이 이 드라이브를 가리킨다 |

JWT 키 만들기:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

> `appsettings.json`에는 DB 비밀번호와 JWT 키가 들어간다. `C:\HRMS` 폴더는 서버 관리자만 열 수 있게 둔다.

### 8.3 80번 포트가 비어 있는지 확인

Windows Server에 IIS(웹 서버)가 켜져 있으면 80번을 이미 쓰고 있어서 HRMS가 기동하지 못한다.

```powershell
netstat -ano | findstr ":80 " | findstr LISTENING     # 아무것도 안 나오면 비어 있음
Get-Service W3SVC -ErrorAction SilentlyContinue       # IIS. 있으면 아래로 끈다
Stop-Service W3SVC; Set-Service W3SVC -StartupType Disabled
```

다른 프로그램이 80번을 쓰고 있으면(`netstat` 마지막 열이 프로세스 번호) `Get-Process -Id 번호`로 확인한다. `System`(PID 4)이면 IIS나 다른 Windows 웹 기능(http.sys)이다.

## 9. Windows 서비스 등록

### 9.1 서비스 등록·시작

```powershell
New-Service -Name HRMS -BinaryPathName "C:\HRMS\HRMS.exe" -DisplayName "HRMS 냉동기 모니터링" -StartupType Automatic

# 비정상 종료 시 1분 뒤 자동 재시작 (Stop-Service로 멈춘 경우는 재시작하지 않는다)
sc.exe failure HRMS reset= 86400 actions= restart/60000/restart/60000/restart/60000

Start-Service HRMS
Get-Service HRMS        # Running이면 정상
```

서비스가 **바로 Stopped로 바뀌면** 설정 문제다. 콘솔로 직접 띄우면 원인이 바로 보인다(17장): `Stop-Service HRMS; cd C:\HRMS; .\HRMS.exe`

### 9.2 방화벽 — 80번만 열고 DB 포트는 막는다

```powershell
# 화면·API
New-NetFirewallRule -DisplayName "HRMS Web (80)" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow

# PostgreSQL 외부 접속 차단 (같은 서버의 HRMS는 영향 없음)
New-NetFirewallRule -DisplayName "Block PostgreSQL external (5432)" -Direction Inbound -Protocol TCP -LocalPort 5432 -Action Block
```

- 2026-10-02 테스트 서버를 밖에서 확인했을 때 **5432가 외부에 열려 있었다.** 반드시 막는다.
- Windows 방화벽은 차단 규칙이 허용 규칙보다 우선한다. PostgreSQL 설치 프로그램이 만든 허용 규칙이 있어도 위 차단 규칙이 이긴다. 같은 서버 안의 접속(`localhost`)은 방화벽을 거치지 않는다.
- 테스트 기간에 접속 IP를 제한하는 방법은 11.2.
- 확인: 다른 PC에서 `Test-NetConnection 서버IP -Port 80` → `True`, `-Port 5432` → `False`.

### 9.3 참고

- 서비스로 돌면 **자동으로 운영 모드(Production)**로 뜬다. `ASPNETCORE_ENVIRONMENT`를 따로 설정하지 않는다.
- HTTP로 서비스한다. 운영 환경(현장)이 폐쇄망이라 HTTPS는 쓰지 않기로 했다(2026-09-28). 인터넷에 노출되는 테스트 기간의 대비는 11.2.
- 서비스 제거: `Stop-Service HRMS; sc.exe delete HRMS`

## 10. 첫 로그인과 관리자 비밀번호

DB에 사용자가 한 명도 없으면 첫 기동 때 관리자 계정이 자동으로 만들어진다.

| 아이디 | 최초 비밀번호 |
|---|---|
| `admin` | `admin1234` |

**첫 로그인 직후 반드시 비밀번호를 바꾸고 기록해 둔다.** 프론트의 비밀번호 변경 기능 또는 API(`PUT /api/users/me/password`, api-manual 68번)로 바꾼다.

> 시스템관리자 계정은 조직관리 화면의 대상이 아니라서 **다른 사람이 비밀번호를 초기화해 줄 수 없다.** 잊어버리면 DB에서 직접 고쳐야 하므로 개발 쪽 지원이 필요하다.

그다음 조직관리 화면에서 사용자를 등록하고 담당 장비를 지정한다. **여기서 등록한 사용자와 담당 장비는 납품 때도 그대로 남는다**(12장). 결재·비상정지 권한은 담당업무(역할)로 정해진다.

| 역할 | 결재 | 비상정지 |
|---|---|---|
| 시스템관리자 | 하지 않음(결재 초기화만) | 가능 |
| 안전관리총괄자 | 3단계 | 가능 |
| 안전관리책임자 | 2단계 | 가능 |
| 안전관리원 | 1단계 | 불가 |
| 일반관리원 | 하지 않음 | 불가 |

---

# 2부. 테스트 서버 운영

## 11. 테스트 서버 운영 (회사 고정 IP)

발주처(현대)가 외부에서 `http://59.16.212.237/`로 접속해 기능을 테스트하는 단계다.

### 11.1 테스트 모드로 운영한다

테스트 서버에는 현장 TLC가 없다. 실제 모드(`TestMode=false`)로 두면 수집 대상 223대가 **전부 "끊김"**, 장비는 전부 "정지"가 되고 기동 30초 뒤 통신장애 경보가 200여 건 쌓인다. 그래서 테스트 서버는 **`TestMode=true`(모의값)**로 운영한다(사용자 결정 2026-10-02).

- 모의값은 시간에 따라 완만하게 변하는 값이라 실시간 현황·트렌드·경보·운전 판정·일지 자동 기록이 실제와 같은 흐름으로 돈다(19장).
- 비상정지는 TLC에 명령을 보내지 않고 화면 상태만 바뀐다. 실제 비상정지 시험은 현장에서 한다(12.5).
- 테스트 기간에 쌓인 모의 트렌드·경보·일지는 납품 때 초기화한다(12장).

### 11.2 인터넷 노출 대비

테스트 서버는 인터넷에 열려 있다. 운영(현장)은 폐쇄망이라 HTTPS 없이 가지만, **테스트 기간에는 아래로 대신한다.**

| 항목 | 할 일 |
|---|---|
| DB 포트 | 5432 차단(9.2) — **필수** |
| 접속 IP 제한(권장) | 80번 허용 규칙을 **현대 측과 회사의 IP로만** 좁힌다(아래). 인증서 없이 할 수 있는 가장 효과적인 대비다 |
| 비밀번호 | `admin`과 테스트 계정 비밀번호를 길게. 테스트 기간 비밀번호는 다른 곳에 쓰는 비밀번호와 다르게 |
| 알고 있을 것 | HTTP라서 로그인·비상정지 비밀번호와 로그인 토큰이 암호화 없이 오간다. 로그인 시도 횟수 제한도 없다 |

접속 IP 제한(현대 측 공인 IP를 받아서):

```powershell
Set-NetFirewallRule -DisplayName "HRMS Web (80)" -RemoteAddress "현대측IP1","현대측IP2","회사IP대역/24"
```

납품 때는 이 제한을 현장 기준으로 바꾸거나 푼다(12.1).

### 11.3 테스트 서버 확인

| # | 확인 | 정상 |
|---|---|---|
| 1 | 서버에서 `curl.exe http://localhost/health` | `200`, `"status":"정상"` |
| 2 | 외부 PC에서 `http://59.16.212.237/health` | 같은 JSON |
| 3 | 외부 PC에서 `http://59.16.212.237/` → 로그인 → 아무 화면에서 F5 | 로그인 성공, 새로고침해도 같은 화면 |
| 4 | `check_install.sql`의 **[기동 이벤트]** | `HRMS 백엔드 시작 (TestMode=True, …)` — 테스트 서버는 **True**가 맞다 |
| 5 | 실시간 현황 화면 | 운영 장비 149대가 "연결됨", 값이 3초마다 바뀜(모의값) |
| 6 | 서비스 재시작·서버 재부팅 | 자동으로 다시 뜨고 `/health` 200 |

테스트 기간에 새 버전이 나오면 15장 절차로 올린다.

---

# 3부. 납품

## 12. 납품 (현장 이전)

테스트를 마친 서버를 **초기화해서 그대로** 남양연구소로 옮긴다. 사용자 결정(2026-10-02): **사용자와 담당 장비만 남기고 나머지는 처음 상태로.**

| 남기는 것 | 지우는 것 | 처음 상태로 |
|---|---|---|
| 사용자, 담당 장비 지정, 사용자 선해임 신고서 첨부 | 트렌드, 이벤트 로그, 점검·운전·수리일지, 교육훈련 일지, 공지사항, 장비 검사이력, 장비 사진·검사이력·교육훈련·공지 첨부 | 장비·압축기·채널 설정(경보 기준값·운전전류 기준값 포함) |

> 테스트 기간에 현장용 경보 기준값을 미리 맞춰 두었다면, 이 초기화로 **기준값도 기본값으로 돌아간다.** 남기고 싶으면 초기화 전에 개발 쪽과 상의한다.

### 12.1 출고 전 (회사에서)

```powershell
# 1) 백업 — 초기화 전 상태를 반드시 남긴다 (14장)
$stamp = Get-Date -Format "yyyyMMdd_HHmm"
& "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe" -h localhost -U hrms_app -d hrms -F c -f "D:\HRMS_backup\before_delivery_$stamp.dump"

# 2) 서비스 중지 (수집이 돌면서 데이터를 쓰지 않게)
Stop-Service HRMS

# 3) 초기화 — 사용자·담당 장비만 남긴다
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\reset_for_delivery.sql
```

3)의 마지막 결과가 이렇게 나오면 정상이다(2026-10-02 리허설: 사용자 56명·담당 장비 971건 보존, 나머지 0).

```text
 users | user_equipments | attachments_kept | equipments | compressors
 (그대로) | (백업 개수와 같음) | (선해임 신고서 수) | 158 | 246

 events | trends | logs | notices | inspection_histories
      0 |      0 |    0 |       0 |                    0
```

이어서:

4. **첨부파일 정리**(12.2)
5. **`appsettings.json`**: `TestMode`를 **`false`**로. JWT 키도 새로 만들어 바꾼다(테스트 기간에 발급된 로그인 토큰이 전부 무효가 된다)
6. **테스트 계정 정리**: 테스트용으로 만든 계정은 조직관리에서 **비활성화**한다(삭제 기능은 없다). `admin` 비밀번호를 새로 바꾸고, 현장 담당자에게 따로 전달한다
7. **방화벽**: 11.2의 IP 제한을 풀거나 현장 내부망 대역으로 바꾼다(현장 IT 정책에 맞춘다). 5432 차단은 그대로 둔다
8. 서버 종료 → 이송

> 5)는 서비스를 켜기 전에 한다. `TestMode=true`인 채로 현장에서 켜면 현장 TLC와 통신하지 않고 모의값으로 화면이 **정상처럼** 돈다.

### 12.2 첨부파일·로그 정리

DB에서 지운 첨부의 파일을 디스크에서도 지운다. **`AppointmentReport`(선해임 신고서) 폴더는 남긴다.**

```powershell
$att = "C:\HRMS\App_Data\attachments"
Remove-Item "$att\EquipmentPhoto", "$att\EquipmentInspectionHistory", "$att\TrainingLog", "$att\Notice" -Recurse -Force -ErrorAction SilentlyContinue

# (선택) 테스트 기간 로그 파일 정리
Remove-Item C:\HRMS\App_Data\logs\*.log -ErrorAction SilentlyContinue
```

### 12.3 현장 설치 (남양연구소)

| 순서 | 할 일 | 비고 |
|---|---|---|
| 1 | 서버 IP 설정 | 연구소 IT가 정한 내부 IP로. HRMS 설정(`0.0.0.0:80`)과 프론트 설정은 IP와 무관해서 고칠 것이 없다 |
| 2 | **서버 시계** | 폐쇄망이라 인터넷 시간 동기화가 안 된다. 연구소 내부 시간 서버(NTP)가 있으면 지정하고, 없으면 정확히 맞춘다: `w32tm /config /manualpeerlist:"내부NTP주소" /syncfromflags:manual /update` |
| 3 | **TLC 네트워크 확인** | 아래 스크립트 — 서버에서 현장 TLC 182대의 TCP 5000에 닿는지 |
| 4 | 서비스 시작 | `Start-Service HRMS` (TestMode=false 확인 후) |
| 5 | 현장 테스트 | 12.4 |

TLC 네트워크 확인 — IP 182개에 1초씩 접속해 **안 되는 IP만** 출력한다(전부 안 되면 최대 약 3분).

```powershell
$ips = Import-Csv C:\HRMS_setup\Compressors.csv -Encoding UTF8 |
       Where-Object { $_.IpAddress } | Select-Object -ExpandProperty IpAddress -Unique
"TLC IP {0}개 확인 중..." -f $ips.Count
$ips | ForEach-Object {
    $c = New-Object Net.Sockets.TcpClient
    $ok = try { $c.ConnectAsync($_, 5000).Wait(1000) } catch { $false }
    $c.Close()
    if (-not $ok) { $_ }
}
```

출력이 없으면 전부 연결된다. 출력된 IP는 연구소 네트워크 담당자와 확인한다. CSV에는 미운영 장비 IP도 들어 있으므로 출력된 IP가 전부 문제인 것은 아니다.

### 12.4 현장 테스트

서비스를 시작하고 **2~3분 기다린 뒤** 확인한다. 1~6번은 반드시, 7~9번은 운영 전에, 10번은 현장 담당자 입회 하에.

| # | 확인 | 방법 | 정상 결과 |
|---|---|---|---|
| 1 | 서비스 기동 | `Get-Service HRMS` | `Running` |
| 2 | 상태 점검 | 서버에서 `curl.exe http://localhost/health` | `200`, `"status":"정상"` |
| 3 | **실제 모드로 떴는지** | `check_install.sql` **[기동 이벤트]** | `HRMS 백엔드 시작 (TestMode=False, 장비 158대, 압축기 246대)` — **True면 즉시 멈추고 12.1-5를 다시** |
| 4 | 내부망 PC에서 접속 | `http://서버IP/` → 로그인 → F5 | 로그인 성공, 새로고침해도 같은 화면 |
| 5 | **현장 TLC 통신** | `check_install.sql` **[통신 상태]** | `0`(연결됨)이 223에 가깝다. 끊긴 압축기는 바로 아래 목록을 네트워크 담당자에게 전달 |
| 6 | 값 갱신 | `check_install.sql` **[최근 10초 갱신]** | 연결된 압축기 수와 비슷. 실시간 현황 숫자가 3초마다 바뀜 |
| 7 | 트렌드 기록 | 2분 뒤 `check_install.sql` **[최근 1분 트렌드]** | 연결된 압축기 수만큼 |
| 8 | 운전·경보 판정 | 실시간 현황 화면 | 실제로 도는 장비가 "운전"으로 보이는지 현장과 대조. 경보는 임시 기준값 때문에 많을 수 있다 — 기준값을 맞춘 뒤 다시 본다 |
| 9 | 재기동 복구 | ① `Restart-Service HRMS` ② `HRMS.exe` 강제 종료 ③ 재부팅 | ① 즉시 ② 1분 안에 ③ 부팅 후 자동으로 `Running` |
| 10 | 비상정지 | 12.5 | |

**항상 "끊김"으로 보이는 압축기 13대가 있다** — IP가 아직 비어 있는 운영 장비 소속이라 수집 대상(223대)에 들어가지 않는다. IP가 정해지면 장비관리 화면(압축기 수정)에서 넣으면 다음 사이클부터 수집된다.

| 시설동 | 장비 | 압축기 |
|---|---|---|
| B지구 전기차환경시험동 | 환경챔버 #1, #2, #3 | 각 2대 |
| C지구 시스템내구동 | 강설챔버 | 1대(2대 중) |
| C지구 인증시험3동 | 저온챔버 #1·소크룸 #1, 저온챔버 #2·소크룸 #2 | 각 2대 |
| C지구 환경시험2동 | 제습용 냉동기 | 2대 |

연결 안 되는 압축기가 30초 넘게 이어지면 압축기마다 **통신장애 경보**가 한 건씩 쌓인다. 현장 네트워크가 덜 열린 상태라면 한꺼번에 쌓이는 것은 정상이다.

### 12.5 비상정지 테스트 — ⚠️ 현장 담당자 입회 필수

**비상정지는 실제 냉동기를 멈춘다.** 시험 중인 장비를 세우면 시험이 중단된다.

1. 현장 담당자와 **시험 대상 장비와 시각을 미리 정한다.** 시험이 걸려 있지 않은 장비를 고른다.
2. **LG 배터리 #1~#7은 고르지 않는다** — 7개 셀이 TLC 하나를 공유해서 한 셀을 멈추면 **7개 셀이 함께 멈춘다**.
3. 화면에서 비상정지를 누른다(책임자급 이상 계정, 비밀번호 재확인). 약 1초 뒤 성공 메시지가 나온다.
4. **현장에서 장비가 실제로 멈췄는지 확인한다.** 화면에도 비상정지로 표시되는지 본다.
5. 해제를 누르고, 장비가 다시 돌 수 있는 상태로 돌아왔는지 현장에서 확인한다.
6. (가능하면) 현장 패널의 **비상정지 버튼을 사람이 직접 눌러서** 화면에 비상정지로 뜨는지 확인한다. 이 시스템은 TLC의 D1805 값으로 상태를 판정하는데, 버튼 조작이 D1805에 반영되는지는 실제 냉동기가 물린 환경에서 아직 확인하지 못했다. 반영되지 않으면 개발 쪽에 알린다.

수행 기록은 이벤트 로그(`EmergencyStop` 카테고리)에 명령문·응답·상태 확인 결과까지 남는다.

---

# 4부. 운영·유지보수

## 13. 주의 사항

| # | 주의 | 이유 |
|---|---|---|
| 1 | **현장에서는 `TestMode=False`인지 꼭 확인한다**(12.4-3) | 테스트 모드로 뜨면 실제 장비와 통신하지 않고 모의값으로 화면이 **정상처럼** 돈다 |
| 2 | **`appsettings.Development.json`을 서버에 두지 않는다**(3.1) | 개발용 설정이 섞여 들어갈 여지를 없앤다 |
| 3 | **개발 PC의 DB를 복사해 가지 않는다**(1장) | 테스트 계정·샘플 결재·테스트 일지가 같이 넘어간다 |
| 4 | **`seed_from_csv.sql`을 단독으로 다시 실행하지 않는다**(7장) | 일지·트렌드·이력·담당 장비가 전부 지워진다. 납품 초기화는 `reset_for_delivery.sql` |
| 5 | **`admin` 비밀번호를 첫 로그인 직후, 그리고 납품 때 바꾼다**(10장, 12.1) | 최초 비밀번호가 문서에 공개되어 있고, 테스트 기간 비밀번호는 발주처와 공유됐다 |
| 6 | **비상정지 테스트는 입회 하에만**(12.5) | 실제 설비가 멈춘다. LG는 7개 셀이 함께 멈춘다 |
| 7 | **업데이트 때 `appsettings.json`과 `App_Data` 폴더를 덮어쓰지 않는다**(15장) | 설정이 초기화되고, `App_Data`에는 **첨부파일**과 로그가 들어 있다 |
| 8 | **백업 기능이 아직 없다**(14장) | 수동으로 정기 백업한다 |
| 9 | **트렌드 자동 정리 기능이 아직 없다** | 1년에 약 13.4GB씩 쌓인다. 대시보드의 저장소 사용량을 주기적으로 본다 |
| 10 | **JWT 키를 바꾸면 모든 사용자가 다시 로그인해야 한다** | 기존 로그인 토큰이 전부 무효가 된다 |
| 11 | **설정값 일괄 적용 API(api-manual 77·78번)는 쓰지 않는다** | 장비별로 맞춘 설정을 한 번에 덮어쓸 위험이 있어 화면 반영을 보류했다(2026-10-01) |
| 12 | **경로에 한글을 쓰지 않는다**(2장) | psql이 파일을 못 읽는 경우가 있다 |
| 13 | **DB 포트(5432)를 외부에 열지 않는다**(9.2) | 테스트 서버에서 실제로 열려 있었다 |

## 14. 백업

자동 백업 기능은 아직 없다(대시보드의 `databaseBackup`도 비어 있다). 그때까지는 **DB와 첨부파일 폴더를 함께** 수동으로 백업한다. 백업 위치는 가능하면 **다른 드라이브나 NAS**로 한다.

```powershell
$stamp = Get-Date -Format "yyyyMMdd_HHmm"
New-Item -ItemType Directory -Force D:\HRMS_backup | Out-Null

# DB
$env:PGPASSWORD = "hrms_app비밀번호"
& "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe" -h localhost -U hrms_app -d hrms -F c -f "D:\HRMS_backup\hrms_$stamp.dump"

# 첨부파일
Compress-Archive -Path C:\HRMS\App_Data\attachments -DestinationPath "D:\HRMS_backup\attachments_$stamp.zip"
```

복원:

```powershell
& "C:\Program Files\PostgreSQL\17\bin\pg_restore.exe" -h localhost -U hrms_app -d hrms --clean --if-exists "D:\HRMS_backup\hrms_YYYYMMDD_HHMM.dump"
```

**업데이트 직전(15장), 납품 초기화 직전(12.1), 장비 자료를 크게 바꾸기 전에는 반드시** 백업한다.

## 15. 새 버전으로 업데이트

**개발 PC**에서 3.1·3.2와 똑같이 게시 폴더와 스키마 SQL을 새로 만들고(`appsettings.Development.json` 삭제), 프론트가 바뀌었으면 새 빌드도 받아서 서버의 `C:\HRMS_setup`으로 가져간다.

**서버**:

```powershell
# 1) 백업 (14장)

# 2) 서비스 중지
Stop-Service HRMS

# 3) 백엔드 파일 복사 — appsettings.json과 App_Data 폴더는 건너뛴다 (/MIR 금지: wwwroot·App_Data가 지워진다)
robocopy C:\HRMS_setup\HRMS C:\HRMS /E /XF appsettings.json /XD App_Data

# 4) 스키마 변경 적용 (이미 적용된 부분은 건너뛰므로 변경이 없어도 실행해도 된다)
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\hrms_schema.sql

# 5) 프론트도 바뀌었으면 wwwroot를 통째로 교체 (/MIR: 옛 파일은 지운다)
robocopy C:\HRMS_setup\frontend C:\HRMS\wwwroot /MIR

# 6) 서비스 시작과 확인
Start-Service HRMS
curl.exe http://localhost/health
```

- **프론트만 바뀐 경우**에는 2)·5)·6)만 하면 된다.
- **`seed_from_csv.sql`은 실행하지 않는다**(13장 4번).
- 새 버전에 `appsettings.json` 항목이 추가됐다면 기존 파일에 **손으로 추가**한다(덮어쓰지 않는다).
- 업데이트 뒤 테스트 서버는 11.3, 현장은 12.4의 1~5번을 다시 확인한다.

## 16. 로그 위치

| 종류 | 위치 | 비고 |
|---|---|---|
| 파일 로그 | `C:\HRMS\App_Data\logs\hrms-yyyyMMdd.log` | 30일 보관(`Logging:File:RetentionDays`) |
| Windows 이벤트 로그 | 이벤트 뷰어 → Windows 로그 → 응용 프로그램 | Warning 이상 |
| 업무 이벤트 | DB `EventLogs` 테이블(화면의 이벤트·자료 조회) | 로그인·경보·통신장애·결재·비상정지·시스템 시작 |

API에서 오류가 나면 응답에 `traceId`가 실리고 같은 번호가 파일 로그에 남는다: `findstr "추적번호" C:\HRMS\App_Data\logs\hrms-*.log`

## 17. 문제 해결

| 증상 | 원인 / 조치 |
|---|---|
| **서버 자신에서도 접속이 안 됨** | ① `Get-Service HRMS`가 `Running`인가 ② `netstat -ano \| findstr ":80 "`에 `0.0.0.0:80 … LISTENING`이 있는가 ③ `curl.exe -i http://localhost/health`. ②가 비어 있으면 8.2 `Urls`가 빠졌거나(그러면 5000번으로 뜬다) 80번을 다른 프로그램이 쓰고 있다(8.3) |
| 서비스가 시작하자마자 `Stopped` | **콘솔로 직접 실행해서 오류를 본다**: `Stop-Service HRMS; cd C:\HRMS; .\HRMS.exe`(정상이면 `Now listening on: http://0.0.0.0:80` — 확인 후 `Ctrl+C` → `Start-Service HRMS`). "DB 연결 문자열이 비어 있습니다", "JWT 서명 키가 없습니다/너무 짧습니다"면 8.2. `address already in use`면 8.3 |
| 서버에서는 되는데 다른 PC에서 안 됨 | 8.2 `Urls`가 `0.0.0.0:80`인지, 9.2의 80번 허용 규칙(11.2의 IP 제한에 그 PC가 빠지지 않았는지). 서버 앞단(기관 방화벽·공유기)이 80번을 막을 수도 있다 — 네트워크 담당자 확인 |
| 외부에서 접속이 "시간 초과" | 서비스가 꺼져 있거나 방화벽이 막는 경우다. 서버에서 ①②③부터 확인 |
| `http://주소/`가 404, `/health`는 정상 | `C:\HRMS\wwwroot\index.html`이 없다(8.1). 한 단계 안쪽 폴더에 들어가 있지 않은지 확인. 넣은 뒤 `Restart-Service HRMS` |
| 화면은 뜨는데 로그인부터 실패 | 프론트의 API 주소 문제다. `C:\HRMS\wwwroot\common\js\config.js`를 확인하고(8.1), 브라우저 개발자 도구(F12) → 네트워크 탭에서 로그인 요청이 `http://서버주소/api/auth/login`으로 가는지 본다. 고친 뒤 Ctrl+F5 |
| 화면 일부가 깨지거나 스크립트 오류 | 프론트 파일 일부가 빠졌다. `robocopy … /MIR`로 `wwwroot`를 다시 복사한다 |
| `/health`가 `503` | `database`가 이상이면 PostgreSQL 서비스·연결 문자열. `polling`이 이상이면 수집이 30초 넘게 멈춘 것 — 파일 로그 확인 |
| (현장) 압축기가 전부 "끊김" | ① `TestMode=False`인지(테스트 모드는 전부 "연결됨"으로 보인다) ② 서버에서 TLC로 TCP 5000이 나가는지 — 12.3 스크립트 ③ 서버 방화벽의 아웃바운드 차단 |
| (현장) 일부 압축기만 "끊김" | `check_install.sql`의 목록을 네트워크 담당자에게 전달. 장비 전원·케이블·IP 변경 여부 확인. IP가 바뀌었으면 장비관리 화면에서 수정 |
| (테스트 서버) 전부 "끊김"·"정지" | `TestMode=false`로 돌고 있다. 테스트 서버는 `true`(11.1) |
| 로그인이 안 됨(`401`) | 아이디/비밀번호 확인. 일반 사용자는 시스템관리자가 조직관리에서 초기화한다. **`admin` 비밀번호를 잊으면 개발 쪽 지원이 필요하다**(10장) |
| `"equipments" 이름의 릴레이션이 없습니다`처럼 소문자로 바뀜 | Windows PowerShell 5.1이 `psql -c` 인자 안의 큰따옴표를 지운 것이다. 쿼리를 `.sql` 파일로 저장해 `-f`로 실행한다. psql 안에 들어가서(부록 B) 직접 입력하는 것은 괜찮다 |
| psql `"UTF8" 인코딩에서 사용할 수 없는 문자` | 명령줄 `-c` 쿼리에 한글이 들어갔다. `.sql` 파일로 저장해 `-f`로 실행한다(파일 안의 한글은 괜찮다) |
| psql `No such file or directory`(경로는 맞는데) | 경로에 한글이 있다. `C:\HRMS_setup`처럼 영문 경로로 옮긴다 |
| psql `password 인증에 실패` | `postgres`(4장)와 `hrms_app`(5장) 비밀번호를 구분한다 |
| `public 스키마 접근 권한 없음`(6장) | 5장의 `GRANT ALL ON SCHEMA public` / `ALTER SCHEMA public OWNER` 두 줄이 빠졌다 |

---

# 5부. 개발 PC 환경

## 18. 개발 DB 준비

PostgreSQL 설치와 DB·계정 생성은 4·5장과 같다. 개발 PC에서는 `hrms_app` 비밀번호를 `1234`로 쓰고 있다.

개발 PC에는 .NET 10 SDK와 EF Core 도구가 필요하다.

```powershell
dotnet tool install --global dotnet-ef     # 최초 1회
dotnet build
dotnet ef database update                   # 스키마 생성·갱신 (서버는 3.2의 SQL 방식)
```

- 연결 문자열·JWT 키·테스트 모드 등 개발 설정은 `appsettings.Development.json`에 있다.
- 2026-09-18에 개발 중 쌓인 마이그레이션 31개를 `InitialCreate` 하나로 합쳤다. 이후 `AddEventLogIndexes`(09-28), `AddEmergencyStopStatus`(09-29)가 추가됐다.
- 새 마이그레이션은 `dotnet ef migrations add 이름`으로 개발 PC에서만 만든다. 서버에는 3.2의 스키마 SQL로 반영한다.
- 장비 데이터는 7장의 `seed_from_csv.sql`을 같은 방식으로 실행한다.
- **서버 실행: `dotnet run --launch-profile http`**(`http://localhost:80`, 2026-10-02부터). 프론트(Live Server)의 `config.js`가 `localhost`로 열렸을 때 이 주소를 부른다. `https` 프로필(7253+5018)은 쓰지 않는다 — HTTPS 리다이렉트 때문에 로그인 토큰이 떨어져 나가 API가 401이 된다(api-manual 공통 사항).
- 개발 모드에서는 모든 origin에 CORS가 열려 있어 Live Server(예: `127.0.0.1:5500`)나 파일로 연 프론트에서도 호출된다.
- **저장소에 `wwwroot` 폴더를 만들지 않는다**(`.gitignore`). 프론트 빌드가 커밋될 뿐 아니라, 빌드가 이 폴더를 기억해서 지운 뒤 개발 모드 기동이 실패한다(2026-10-02 확인). 그렇게 됐다면 `bin\Debug\net10.0\HRMS.staticwebassets.*.json`과 `obj\Debug\net10.0\staticwebassets*`를 지우고 다시 빌드한다.

## 19. 테스트 모드

`Communication:TestMode=true`면 실제 TCP 통신 없이 **전 압축기가 정상 통신하는 것으로 가정하고 모의값**을 채운다. 시간에 따라 완만하게 변하는 사인파(6시간 주기, 64채널 중 1개만 경보 범위를 벗어남)라서 통신 상태·경보 판정·장비 상태 집계·트렌드까지 실제와 같은 흐름으로 확인할 수 있다([program-flow.md](program-flow.md) 6장). 개발 PC와 테스트 서버(11장)가 이 모드로 돈다.

- 켜고 끄는 건 설정만 바꾸고 **재시작**하면 된다.
- 비상정지도 TLC에 명령을 보내지 않고 상태만 바꾼다.

## 20. 실기기 통신 테스트

회사망에서 접근 가능한 샘플 TLC가 **1대**(`59.16.212.252`) 있다.

### 20.1 권장 — 테스트 모드를 켠 채로 샘플 TLC만 실제 통신

`Communication:RealDeviceIps`에 적힌 IP의 압축기만 실제로 통신하고 나머지는 모의값을 쓴다(`Modules/Communication/CommunicationMode.cs`). 통신장애 경보가 쌓이지 않는다. 비상정지 명령도 이 목록의 TLC에만 실제로 나간다.

```json
"Communication": {
    "TestMode": true,
    "RealDeviceIps": [ "59.16.212.252" ]
}
```

```sql
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;   -- 원래 값 10.90.21.233
```

**현재는 원복된 상태다**(2026-10-01). 2026-09-29~10-01 동안 장비 1번(A지구 차량장비동 BSR #1)의 1번 압축기를 샘플 TLC에 연결해 시험했고, 지금은 원래 주소로 되돌리고 `RealDeviceIps`를 비워 두었다. 운영 환경은 `TestMode=false`라 이 목록을 보지 않는다.

### 20.2 전체 실모드로 확인 (예전 방법)

> **30초 규칙**: 실모드에서는 닿지 않는 나머지 200여 대가 통신에 실패하고, **30초 넘게 이어지면 압축기마다 통신장애 경보가 쌓인다**. 확인은 30초 안에 끝내고, 넘겼으면 4)의 정리를 반드시 한다.

1) 원래 IP와 이벤트 기준점을 적어 두고 테스트 IP로 바꾼다.

```sql
SELECT "Id", "IpAddress" FROM "Compressors" WHERE "Id" = 1;
SELECT max("Id") FROM "EventLogs";
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;
```

2) `TestMode`를 `false`로 바꾸고 재시작한다.

3) 값이 들어오는지 본다. `CommunicationStatus = 0`이고 `MeasuredAt`이 두 번 조회 사이에 바뀌면 성공이다.

```sql
SELECT "ChannelNo", "Value", "MeasuredAt" FROM "CompressorSensorCurrents" WHERE "CompressorId" = 1 ORDER BY "ChannelNo";
```

4) 원복한다(반드시).

```sql
UPDATE "Compressors" SET "IpAddress" = '10.90.21.233' WHERE "Id" = 1;
DELETE FROM "EventLogs" WHERE "Category" = 2 AND "Id" > 기준_이벤트id;
DELETE FROM "CompressorMeasurements" WHERE "CompressorId" = 1 AND "MeasuredAt" >= '테스트 시작 시각';
```

### 20.3 장비 없이 프레임만 확인 — 가짜 TLC

로컬에 TCP 5000 포트로 응답만 돌려주는 프로그램(node 등)을 띄우고 압축기 IP를 `127.0.0.1`로 바꾸면 요청 프레임을 볼 수 있다. 응답은 `[STX]01RRD,OK,<4자리 16진수 × 개수><체크섬 2자리>[CR][LF]`(체크섬은 STX/CR/LF를 뺀 문자들의 ASCII 합 mod 256).

### 20.4 확인 기록

| 날짜 | 시험 | 결과 |
|---|---|---|
| 09-18 | 프레임 형식(가짜 TLC + LG 압축기) | 센서 없는 CH03·CH06 자리에 더미 `0099`, 그 두 채널은 저장 안 됨 |
| 09-18 | 실기기 단일 압축기 | 성공. 7채널이 3초마다 갱신 |
| 09-18 | 실기기 동시 접속 42개 | 사이클마다 40~41대 성공(실패 약 3~5%), 통신장애 경보 없음. **실제 LG TLC로 재확인 필요** |
| 09-29 | 비상정지 쓰기·되읽기 | `01WRD,001,1805,0001` 정상 수락. **D1801은 반응하지 않아 판정을 D1805로 확정.** TLC가 쓰기 반영을 0.4~0.8초 뒤에 해서 되읽기를 재시도하도록 함 |

## 21. 테스트용 샘플 데이터

일괄 결재 화면 테스트용(개발 전용, **서버에는 넣지 않는다**):

| 파일 | 역할 |
|---|---|
| `Infrastructure/Seed/sample_approvals.sql` | 단계별로 결재가 진행된 일지들을 만든다. 여러 번 실행해도 중복 생성되지 않는다 |
| `Infrastructure/Seed/sample_approvals_reset.sql` | 테스트로 결재한 뒤 처음 상태로 되돌린다 |

계정(개발 DB): `admin`/`1234`, `test1`/`1234`(책임자), 그 외 테스트 인원은 `test1234` — `safety19`(안전관리원), `manager01`(책임자), `chief01`(총괄자), `staff01`(일반관리원).

## 22. 장비 자료 갱신과 원본 대조 기록

### 22.1 파일 구성

| 파일 | 역할 |
|---|---|
| `Infrastructure/Equipments.csv` | 장비 158대 원본(UTF-8 BOM) |
| `Infrastructure/Compressors.csv` | 압축기 246대 원본. 소속 장비 id, IP, MAC |
| `Infrastructure/Seed/generate_seed_from_csv.js` | 두 CSV로 `seed_from_csv.sql`을 만든다 |
| `Infrastructure/Seed/seed_from_csv.sql` | 실제로 실행하는 SQL(생성물 — 직접 고치지 않는다) |
| `Infrastructure/Setup/check_install.sql` | 설치 확인(읽기 전용) |
| `Infrastructure/Setup/reset_for_delivery.sql` | 납품 초기화(12장) |

자료가 갱신되면 CSV를 교체하고 `node Infrastructure/Seed/generate_seed_from_csv.js`로 다시 만든다. 미운영 장비 목록과 운전전류 기준값은 스크립트 상단 상수(`NON_OPERATIONAL`, `RUNNING_CURRENT_THRESHOLD`)에 있다. **운영 중인 현장 DB에는 다시 실행하지 않는다**(13장 4번) — 운영 중 자료 변경은 장비관리 화면에서 개별로 고친다.

2026-10-02에 개발 DB의 장비·압축기·채널 설정이 이 시드 결과와 **컬럼 단위로 완전히 같음**을 확인했다(실시간 상태 컬럼 제외).

### 22.2 시드가 채우는 값

- 장비 상태: 실차환경챔버 #1(id 9)과 수소충전소 냉동기 8대(id 111~118)는 `미운영`, 나머지 149대는 `운영`
- 운전전류 기준값: 전 장비 raw `100`(10.0A). 비어 있으면 운전 판정이 항상 정지가 되고 경보도 뜨지 않는다
- 압축기 순번: 장비별로 CSV의 압축기 id 순서대로 1부터
- 채널 설정: CH01~03 ℃ 소수점 1자리, CH04~06 MPa 2자리, CH07 A 1자리. 상한 1000·하한 0, 지연 각 30초. LG 냉동기(배터리 #1~#7, 압축기 42대)는 CH03·CH06 "사용 안 함"
- id 시퀀스는 158, 246부터 발급되도록 맞춘다
- LG는 압축기 42대가 IP `10.90.87.217` 하나를 공유한다(TLC 한 대가 중계). 압축기별 레지스터 주소는 채널 설정(`RegisterAddress`)으로 관리한다

### 22.3 원본 엑셀과의 대조 (2026-09-18)

CSV는 `남양연구소 법정냉동기 현황_2026.09.01_삼원테크.xlsx`(시트 "남양 냉동기 보유현황")에서 만들었고, 한 줄씩 대조해서 아래를 바로잡았다.

| 수정 | 내용 |
|---|---|
| 상용설계동 7대 IP | `10.90.37.233~239` → `10.90.198.233~239`(종합내구시험동 IP와 겹쳐 있었다) |
| 기후재현시험실 COMP#1 MAC | `...00:23` → `...00:2D` |
| 배기1동 환경챔버 #1/#2 IP | `10.90.163.232/233` → `10.90.162.232/233` |
| 인증시험동 MAC 2건 | 소크룸 냉동기#2 `...00:06` → `...04:06`, 저온챔버#2 냉동기#1 빈칸 → `98:06:37:70:00:73` |
| 브라인 유무 | 의미가 반대로 들어가 있었다. 직팽식이면 0, 냉수/브라인/Booster/Intercooler면 1 |
| 실차환경챔버 #1(장비 9) | 압축기 2대로 정정(IP/MAC 미정) |
| 시설동 이름(2026-10-01) | `총합내구시험동` → `종합내구시험동`(원본 작업 오타) |

의도적으로 원본과 다르게 둔 것: 시스템내구동 강설챔버의 IP(`10.90.152.239` 유지, 사용자 지시), 수소충전소 8대를 장비로 등록하고 미운영 처리, 엑셀의 "중앙통제시스템 연결여부" 불능 26대·신규 14대는 미반영(처리 방법 미정).

## 23. 개발 팁

### 23.1 로그로 값 확인하기 (`ILogger`)

```csharp
logger.LogInformation("channels: {Channels} {CompressorId}", channels, c.Id);
```

- `{이름}` 자리표시자는 **뒤에 나열한 인자 순서대로** 채워진다.
- 문자열 보간(`$"..."`)으로 미리 합치지 않는다 — `"{이름}", 값` 형태를 지켜야 구조적으로 검색할 수 있다.
- 예외는 `logger.LogError(ex, "메시지")`처럼 첫 인자로 넘긴다.

`ILogger`는 콘솔과 파일 로그에만 남고 **DB에는 남지 않는다.** 화면의 이벤트로 남기려면 `EventLogger.LogAsync(db, category, message, username)`을 호출한다([Modules/Logging](../Modules/Logging/README.md)). 매 사이클 도는 코드에 넣으면 DB가 금방 차니 일회성 디버깅에는 `ILogger`만 쓴다.

### 23.2 EF Core 쿼리 로그 줄이기

`Logging:LogLevel`에 `"Microsoft.EntityFrameworkCore": "Warning"`을 둔다(개발·운영 설정 모두 이미 들어 있다).

---

## 부록 A. 설정 항목 (`appsettings.json`)

| 항목 | 기본값 | 설명 |
|---|---|---|
| `Urls` | (없음 → `http://localhost:5000`) | 서비스 주소. 서버는 `http://0.0.0.0:80`(8.2) |
| `ConnectionStrings:Default` | 빈 값 | **필수.** 비면 기동 안 함 |
| `Jwt:Key` / `Jwt:Issuer` | 빈 값 / `HRMS` | **Key 필수, 32바이트 이상** |
| `Cors:AllowedOrigins` | `[]` | 화면과 API가 같은 주소라 `[]`. 개발 모드는 전부 허용 |
| `Communication:TestMode` | `false` | `true`면 모의값. **테스트 서버 `true`, 현장 `false`** |
| `Communication:TimeoutMs` | `1500` | TLC 연결·응답 타임아웃(ms). 폴링 주기(3초)보다 짧아야 한다 |
| `Communication:RealDeviceIps` | (없음) | 테스트 모드에서도 실제로 통신할 IP 목록(20.1) |
| `FileStorage:RootPath` | `App_Data/attachments` | 첨부파일 저장 위치(실행 파일 기준 상대 경로 또는 절대 경로) |
| `SystemStatus:StorageDrive` | `C:\` | 대시보드 저장소 사용량을 볼 드라이브 = PostgreSQL 데이터 드라이브 |
| `Logging:File:Directory` / `RetentionDays` | `App_Data/logs` / `30` | 파일 로그 위치·보관 일수 |
| (폴더) `wwwroot` | 없음 | 프론트 빌드 결과(8.1). 있으면 `/`에서 화면을 내보낸다. **개발 PC 저장소에는 두지 않는다** |

## 부록 B. psql 기본 명령

```powershell
$env:PGPASSWORD = "hrms_app비밀번호"
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U hrms_app -d hrms
```

| 명령 | 뜻 |
|---|---|
| `\dt` | 테이블 목록 |
| `\d "Equipments"` | 테이블 구조 |
| `SELECT * FROM "Equipments" LIMIT 5;` | 데이터 조회(테이블·컬럼 이름은 **큰따옴표**로 감싼다) |
| `\q` | 나가기 |
