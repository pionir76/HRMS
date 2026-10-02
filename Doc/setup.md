# HRMS 설치·배포 가이드

HRMS 백엔드를 **현장 서버에 설치하는 절차(1부)**와 **개발 PC 환경(2부)**을 정리한 문서다. 현장 설치는 1부를 위에서부터 순서대로 따라 하면 된다.

> 1부의 절차는 2026-10-02 개발 PC에서 **그대로 리허설해서 확인했다** — 게시 → 스키마 SQL → 장비 시드 → 운영 모드 기동 → `/health`·관리자 로그인·기동 이벤트 확인까지. 문서에 적힌 숫자(장비 158대, 수집 대상 223대 등)는 그때의 실제 결과다.

**목차**

- 1부. 현장 서버 설치
  - [1. 한눈에 보기](#1-한눈에-보기) · [2. 사전 확인](#2-사전-확인) · [3. 개발 PC에서 배포 파일 만들기](#3-개발-pc에서-배포-파일-만들기)
  - [4. PostgreSQL 설치](#4-postgresql-설치) · [5. DB·계정 만들기](#5-db계정-만들기) · [6. DB 스키마 만들기](#6-db-스키마-만들기) · [7. 장비 데이터 넣기](#7-장비-데이터-넣기)
  - [8. 프로그램 배치와 설정](#8-프로그램-배치와-설정) · [9. Windows 서비스 등록](#9-windows-서비스-등록) · [10. 첫 로그인과 관리자 비밀번호](#10-첫-로그인과-관리자-비밀번호)
  - [11. 설치 후 테스트](#11-설치-후-테스트) · [12. 주의 사항](#12-주의-사항) · [13. 백업](#13-백업) · [14. 새 버전으로 업데이트](#14-새-버전으로-업데이트) · [15. 로그 위치](#15-로그-위치) · [16. 문제 해결](#16-문제-해결)
- 2부. 개발 PC 환경
  - [17. 개발 DB 준비](#17-개발-db-준비) · [18. 테스트 모드](#18-테스트-모드) · [19. 실기기 통신 테스트](#19-실기기-통신-테스트) · [20. 테스트용 샘플 데이터](#20-테스트용-샘플-데이터) · [21. 장비 자료 갱신](#21-장비-자료-갱신과-원본-대조-기록) · [22. 개발 팁](#22-개발-팁)
- [부록 A. 설정 항목](#부록-a-설정-항목-appsettingsjson) · [부록 B. psql 기본 명령](#부록-b-psql-기본-명령)

---

# 1부. 현장 서버 설치

## 1. 한눈에 보기

현장 서버는 폐쇄망이라 인터넷에서 패키지를 받을 수 없다고 보고, **빌드는 개발 PC에서 끝내고 결과물만 가져가는** 방식으로 설치한다. 서버에는 .NET SDK도, `dotnet ef` 도구도 필요 없다.

```text
[개발 PC]                                    [현장 서버]
 3. 실행 파일 게시(.NET 포함)  ─┐             4. PostgreSQL 17 설치
    DB 스키마 SQL 생성          ├─ USB 등 ─▶   5. DB·계정 만들기
    장비 시드 SQL               │              6. 스키마 SQL 실행
    PostgreSQL 설치 파일       ─┘              7. 장비 시드 SQL 실행
                                               8. 프로그램 배치 + appsettings.json 설정
                                               9. Windows 서비스 등록
                                              10. 첫 로그인 → 관리자 비밀번호 변경
                                              11. 설치 후 테스트
```

**새 DB로 시작한다.** 개발 PC의 DB를 복사해 가지 않는다 — 테스트 계정 51명, 결재 샘플 데이터, 테스트로 만든 일지까지 같이 넘어가기 때문이다. 장비·압축기·채널 설정은 시드 SQL로 넣고, 사용자는 설치 후 조직관리에서 실제 인원을 등록한다.

## 2. 사전 확인

### 2.1 서버 조건

| 항목 | 기준 | 비고 |
|---|---|---|
| OS | Windows Server 또는 Windows 10/11 (64비트) | |
| 디스크 | DB 드라이브에 **여유 50GB 이상** 권장 | 트렌드 기록이 하루 약 37MB, 1년 약 13.4GB 쌓인다. 아직 자동 정리 기능이 없다(12장) |
| 시계 | **시간이 정확해야 한다**(NTP 동기화) | 매일 자정의 운전일지·점검일지 자동 기록, 매분 트렌드 기록이 서버 시계를 기준으로 돈다. 시간대 설정과 무관하게 한국시간으로 계산한다 |
| 전원·절전 | **절전 모드 끄기** | 서버가 잠들면 수집이 멈춘다 |
| 권한 | 관리자 권한 PowerShell | 서비스 등록·방화벽·환경 설정에 필요 |
| 설치 폴더 | **경로에 한글을 쓰지 않는다**(예: `C:\HRMS`, `C:\HRMS_setup`) | psql이 한글 경로의 파일을 읽지 못하는 경우가 있다(리허설에서 SQL 안의 `\i` 경로로 실제 발생) |

### 2.2 네트워크 확인 (선택이지만 권장)

서버가 현장 TLC들의 **TCP 5000 포트**에 닿아야 한다. 설치 전에 확인해 두면, 설치 후 "통신 끊김"이 나왔을 때 프로그램 문제인지 네트워크 문제인지 바로 가를 수 있다.

`Compressors.csv`(3.3에서 가져감)의 IP 182개에 1초씩 접속해 보고, **안 되는 IP만** 출력한다. 전부 안 되는 경우 최대 약 3분 걸린다.

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

출력이 없으면 전부 연결된다는 뜻이다. 출력된 IP는 현장 네트워크(방화벽·VLAN·장비 전원) 담당자와 확인한다. **CSV에는 미운영 장비 IP도 들어 있으므로** 출력된 IP가 전부 문제인 것은 아니다.

## 3. 개발 PC에서 배포 파일 만들기

개발 PC의 저장소 루트(`HRMS` 폴더)에서 진행한다. 결과물을 모을 폴더는 `C:\HRMS_setup`으로 가정한다.

### 3.1 실행 파일 게시

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o C:\HRMS_setup\HRMS
```

- `--self-contained`라서 **.NET 런타임이 함께 들어간다.** 서버에 .NET을 따로 설치할 필요가 없다(약 114MB, 파일 약 350개).
- 결과물에 **`appsettings.Development.json`이 같이 딸려 나온다. 반드시 지운다.** 개발 PC용 설정(테스트 모드 켜짐, 개발 DB 비밀번호, 개발용 JWT 키)이 들어 있어서, 실수로 개발 모드로 뜨면 실제 장비와 통신하지 않고 모의값으로 화면이 정상처럼 돈다.

```powershell
Remove-Item C:\HRMS_setup\HRMS\appsettings.Development.json
```

### 3.2 DB 스키마 SQL 만들기

```powershell
dotnet ef migrations script --idempotent -o C:\HRMS_setup\hrms_schema.sql
```

- 테이블을 만드는 SQL이다(약 22KB). `--idempotent`라서 **이미 적용된 부분은 건너뛴다** — 같은 파일을 두 번 실행해도 안전하고, 나중에 업데이트할 때도 같은 방식으로 만든 파일을 그냥 실행하면 된다(14장).
- 개발 PC에 `dotnet ef` 도구가 있어야 한다(17장).

### 3.3 나머지 파일 복사

| 가져갈 것 | 어디서 |
|---|---|
| `Infrastructure\Seed\seed_from_csv.sql` | 저장소 — 장비·압축기·채널 설정 시드 |
| `Infrastructure\Compressors.csv` | 저장소 — 2.2 네트워크 확인용 |
| `Infrastructure\Setup\check_install.sql` | 저장소 — 설치 확인 스크립트(6·7·11장) |
| PostgreSQL 17 Windows 설치 파일 | https://www.postgresql.org/download/windows/ (EDB installer, **17.x**) |

최종적으로 `C:\HRMS_setup`에 아래가 있으면 된다. 이 폴더째로 서버의 같은 경로에 복사한다.

```text
C:\HRMS_setup\
 ├─ HRMS\                   (게시 결과 — appsettings.Development.json은 지운 상태)
 ├─ hrms_schema.sql
 ├─ seed_from_csv.sql
 ├─ check_install.sql
 ├─ Compressors.csv
 └─ postgresql-17.x-windows-x64.exe
```

## 4. PostgreSQL 설치

1. 설치 파일을 실행한다. **버전 17.x**를 쓴다(18은 아직 Npgsql 드라이버 호환 검증이 덜 됐다).
2. 설치 마법사:
    - **Installation Directory**: 기본값(`C:\Program Files\PostgreSQL\17`)
    - **Select Components**: `PostgreSQL Server`, `pgAdmin 4`, `Command Line Tools` 체크. `Stack Builder`는 해제
    - **Data Directory**: 기본값. 데이터 전용 드라이브가 있으면 그쪽(예: `D:\PostgreSQL\data`)으로 지정한다 — 8장 `StorageDrive`와 맞춘다
    - **Password**: `postgres` 관리자 비밀번호 — **반드시 기록**
    - **Port**: `5432` / **Locale**: 기본값
    - 마지막의 Stack Builder 실행은 해제하고 닫는다.
3. 확인:

    ```powershell
    Get-Service -Name postgresql*      # Running이면 정상
    ```

## 5. DB·계정 만들기

앱은 `postgres` 관리자 계정이 아니라 **전용 계정 `hrms_app`**으로 접속한다. 아래에서 `postgres비밀번호`와 `hrms_app비밀번호`를 실제 값으로 바꿔 실행한다. `hrms_app` 비밀번호는 **개발 PC의 `1234`를 쓰지 말고 새로 정한다.**

```powershell
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$env:PGPASSWORD = "postgres비밀번호"

& $psql -h localhost -U postgres -c "CREATE DATABASE hrms;"
& $psql -h localhost -U postgres -c "CREATE USER hrms_app WITH PASSWORD 'hrms_app비밀번호';"
& $psql -h localhost -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE hrms TO hrms_app;"

# PostgreSQL 15부터 public 스키마 기본 권한이 제한되어 따로 준다
& $psql -h localhost -U postgres -d hrms -c "GRANT ALL ON SCHEMA public TO hrms_app;"
& $psql -h localhost -U postgres -d hrms -c "ALTER SCHEMA public OWNER TO hrms_app;"

Remove-Item Env:\PGPASSWORD
```

확인 — `hrms | hrms_app`이 나오면 정상:

```powershell
$env:PGPASSWORD = "hrms_app비밀번호"
& $psql -h localhost -U hrms_app -d hrms -c "SELECT current_database(), current_user;"
```

> 이후 6·7·11장의 psql 명령은 이 `$psql`·`$env:PGPASSWORD`(hrms_app)가 설정된 같은 창에서 실행한다고 가정한다.

> **확인 쿼리는 `check_install.sql` 파일로 실행한다.** Windows PowerShell 5.1은 `psql -c '...'`로 넘기는 쿼리 안의 큰따옴표를 지워 버려서 `"Equipments"` 같은 테이블 이름이 소문자로 바뀌고 실패한다(2026-10-02 확인). 파일로 실행하면 이 문제가 없다. 읽기만 하는 스크립트라 몇 번을 실행해도 되고, 아직 진행하지 않은 단계의 항목은 0으로 나온다.
>
> ```powershell
> & $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\check_install.sql
> ```

## 6. DB 스키마 만들기

```powershell
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\hrms_schema.sql
```

확인 — `check_install.sql`을 실행해 **[6장]** 항목에 마이그레이션 3개(`InitialCreate`, `AddEventLogIndexes`, `AddEmergencyStopStatus`)가 나오면 정상.

**앱은 스키마를 자동으로 만들지 않는다.** 이 단계를 건너뛰고 기동하면 첫 쿼리에서 실패한다.

## 7. 장비 데이터 넣기

```powershell
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\seed_from_csv.sql
```

확인 — `check_install.sql`의 **[7장]** 항목이 `158 / 246 / 1722`, 운영 장비 `149` / 수집 대상 `223`이면 정상.

> **⚠️ 이 SQL은 처음 설치할 때 한 번만 실행한다.** 기존 장비·압축기를 **지우고 다시 넣는** 스크립트라, 운영 중에 돌리면 트렌드 기록·이벤트 로그·점검/운전/수리일지·장비 검사이력·담당 장비 지정·장비 사진이 **전부 삭제된다**. 전체가 한 트랜잭션이라 중간에 실패하면 아무것도 바뀌지 않는다.

들어가는 값(요약):

| 항목 | 값 |
|---|---|
| 장비 | 158대. 이 중 **운영 149대**, 미운영 9대(A지구 PDI 1동 실차환경챔버 #1, C지구 수소충전소 냉동기 8대) |
| 압축기 | 246대(장비당 1~6대). 이 중 운영 장비 소속이면서 IP가 있는 **223대가 수집 대상**이다 |
| 채널 설정 | 압축기마다 CH01~07. 상한 1000·하한 0(raw), 경보 발생/해제 지연 30초. LG 냉동기 42대는 CH03·CH06이 "사용 안 함" |
| 운전전류 기준값 | 전 장비 raw `100`(= 10.0A) |

경보 기준값(상·하한)은 **임시 기본값**이다. 실제 운영값은 설치 후 장비관리 화면에서 장비별로 맞춘다.

## 8. 프로그램 배치와 설정

### 8.1 배치

`C:\HRMS_setup\HRMS` 폴더를 **`C:\HRMS`**로 복사한다. 실행 파일은 `C:\HRMS\HRMS.exe`가 된다.

### 8.2 `appsettings.json` 설정

`C:\HRMS\appsettings.json`을 메모장으로 열어 아래 항목을 채운다. **나머지 항목은 그대로 둔다.**

```json
{
  "Urls": "http://0.0.0.0:5000",

  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=hrms;Username=hrms_app;Password=hrms_app비밀번호"
  },

  "Jwt": {
    "Key": "아래 명령으로 만든 무작위 문자열",
    "Issuer": "HRMS"
  },

  "Cors": {
    "AllowedOrigins": [ "http://프론트주소:포트" ]
  },

  "Communication": {
    "TestMode": false,
    "TimeoutMs": 1500
  },

  "SystemStatus": {
    "StorageDrive": "C:\\"
  }
}
```

| 항목 | 넣을 값 | 비고 |
|---|---|---|
| `Urls` | `http://0.0.0.0:5000` | **새로 추가하는 줄**이다. 없으면 서버 자신(`localhost`)에서만 접속되고 다른 PC에서는 접속이 안 된다. 포트를 바꾸려면 9.2 방화벽도 같이 바꾼다 |
| `ConnectionStrings:Default` | 5장의 `hrms_app` 비밀번호 | **비어 있으면 서비스가 기동하지 않는다**(의도된 동작) |
| `Jwt:Key` | 무작위 문자열 32바이트 이상 | **비어 있거나 짧으면 기동하지 않는다.** 개발 PC의 키를 쓰지 않는다 |
| `Cors:AllowedOrigins` | 프론트 웹 주소 | 프론트를 이 서버의 **같은 주소·포트**에서 서비스하면 `[]` 그대로 둔다. 다른 포트·다른 PC면 그 주소를 넣는다 — 포트만 달라도 브라우저가 막는다 |
| `Communication:TestMode` | **`false`** | 기본값이 `false`다. `true`면 실제 장비와 통신하지 않는다 |
| `SystemStatus:StorageDrive` | **PostgreSQL 데이터 폴더가 있는 드라이브** | 대시보드의 저장소 사용량이 이 드라이브를 가리킨다. 4장에서 데이터 폴더를 바꿨으면 그 드라이브로 |

JWT 키 만들기:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

> 비밀번호와 키를 파일에 적지 않고 시스템 환경변수로 줄 수도 있다(이름의 구분자는 **밑줄 두 개**: `ConnectionStrings__Default`, `Jwt__Key`). 폐쇄망이라 파일에 적어도 무방하다는 판단이다(사용자 확인 2026-09-28).

## 9. Windows 서비스 등록

### 9.1 서비스 등록·시작

```powershell
New-Service -Name HRMS -BinaryPathName "C:\HRMS\HRMS.exe" -DisplayName "HRMS 냉동기 모니터링" -StartupType Automatic

# 비정상 종료 시 1분 뒤 자동 재시작
sc.exe failure HRMS reset= 86400 actions= restart/60000/restart/60000/restart/60000

Start-Service HRMS
Get-Service HRMS        # Status가 Running이면 정상
```

서비스가 **바로 Stopped로 바뀌면** 설정 문제다. 이벤트 뷰어 → Windows 로그 → 응용 프로그램에서 한국어 오류 메시지(예: "DB 연결 문자열이 비어 있습니다")를 확인한다(16장).

### 9.2 방화벽

```powershell
New-NetFirewallRule -DisplayName "HRMS API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

### 9.3 참고

- 서비스로 돌면 **자동으로 운영 모드(Production)**로 뜬다. `ASPNETCORE_ENVIRONMENT`를 따로 설정하지 않는다.
- HTTPS는 쓰지 않는다(폐쇄망). HTTPS 리다이렉트는 개발 모드에서만 걸리므로 HTTP로 그대로 서비스된다.
- 서비스 제거: `Stop-Service HRMS; sc.exe delete HRMS`

## 10. 첫 로그인과 관리자 비밀번호

DB에 사용자가 한 명도 없으면 첫 기동 때 관리자 계정이 자동으로 만들어진다.

| 아이디 | 최초 비밀번호 |
|---|---|
| `admin` | `admin1234` |

**첫 로그인 직후 반드시 비밀번호를 바꾸고, 바꾼 비밀번호를 안전한 곳에 기록해 둔다.** 프론트의 비밀번호 변경 기능 또는 API(`PUT /api/users/me/password`, api-manual 68번)로 바꾼다.

> 시스템관리자 계정은 조직관리 화면의 대상이 아니라서 **다른 사람이 비밀번호를 초기화해 줄 수 없다.** 잊어버리면 DB에서 직접 고쳐야 하므로 개발 쪽 지원이 필요하다.

그다음 조직관리 화면에서 실제 사용자를 등록하고 담당 장비를 지정한다. 결재 권한과 비상정지 권한은 사용자의 **담당업무(역할)**로 정해진다.

| 역할 | 결재 | 비상정지 |
|---|---|---|
| 시스템관리자 | 하지 않음(결재 초기화만) | 가능 |
| 안전관리총괄자 | 3단계 | 가능 |
| 안전관리책임자 | 2단계 | 가능 |
| 안전관리원 | 1단계 | 불가 |
| 일반관리원 | 하지 않음 | 불가 |

## 11. 설치 후 테스트

서비스를 시작하고 **2~3분 기다린 뒤** 위에서부터 차례로 확인한다. 1~6번은 반드시, 7~9번은 운영 전에, 10번은 현장 담당자 입회 하에 한다.

### 11.1 테스트 목록

| # | 확인 | 방법 | 정상 결과 |
|---|---|---|---|
| 1 | 서비스 기동 | `Get-Service HRMS` | `Running` |
| 2 | 상태 점검 | 서버에서 `curl.exe http://localhost:5000/health` | `200`, `"status":"정상"`. DB 연결 이상이나 수집 정지면 `503` |
| 3 | **운영 모드로 떴는지** | 11.2의 기동 이벤트 쿼리 | `HRMS 백엔드 시작 (TestMode=False, 장비 158대, 압축기 246대)` — **`TestMode=True`면 즉시 중지하고 8.2를 다시 본다** |
| 4 | 로그인 | 다른 PC 브라우저에서 프론트 접속 → `admin` 로그인 | 로그인 성공. 실패하면 9.2 방화벽, 8.2 `Urls`·`Cors` 확인 |
| 5 | **현장 TLC 통신** | 11.2의 통신 상태 쿼리 | 수집 대상 223대 중 대부분 `연결됨`. 끊긴 압축기는 11.2의 목록으로 뽑아 네트워크 담당자와 확인 |
| 6 | 값 갱신 | 11.2의 갱신 쿼리를 몇 초 간격으로 두 번 | 최근 10초 안에 갱신된 압축기 수가 연결된 수와 비슷. 실시간 현황 화면 숫자가 3초마다 바뀜 |
| 7 | 트렌드 기록 | 2분 뒤 11.2의 트렌드 쿼리 | 최근 1분 기록이 연결된 압축기 수만큼 |
| 8 | 운전·경보 판정 | 실시간 현황 화면 | 실제로 돌고 있는 장비가 "운전"으로 보이는지 현장과 몇 대 대조. 경보는 7장의 임시 기준값 때문에 많이 뜰 수 있다 — 기준값을 맞춘 뒤 다시 본다 |
| 9 | 재기동 복구 | ① `Restart-Service HRMS` ② 작업 관리자에서 `HRMS.exe` 강제 종료 ③ 서버 재부팅 | ① 즉시 ② 1분 안에 ③ 부팅 후 자동으로 다시 `Running`, `/health` 200 |
| 10 | 비상정지 | 11.3 | 11.3 참고 |

**항상 "끊김"으로 보이는 압축기 13대가 있다** — IP가 아직 비어 있는 운영 장비 소속이라 수집 대상(223대)에 들어가지 않는다. 정상이다.

| 시설동 | 장비 | 압축기 |
|---|---|---|
| B지구 전기차환경시험동 | 환경챔버 #1, #2, #3 | 각 2대 |
| C지구 시스템내구동 | 강설챔버 | 1대(2대 중) |
| C지구 인증시험3동 | 저온챔버 #1·소크룸 #1, 저온챔버 #2·소크룸 #2 | 각 2대 |
| C지구 환경시험2동 | 제습용 냉동기 | 2대 |

IP가 정해지면 장비관리 화면(압축기 수정)에서 넣으면 다음 사이클부터 수집된다.

### 11.2 확인용 쿼리

`check_install.sql`을 실행하면 11.1의 3·5·6·7번 결과가 한 번에 나온다(5장의 psql 창에서).

```powershell
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\check_install.sql
```

| 스크립트 항목 | 정상 |
|---|---|
| **[11장-3] 기동 이벤트** | 맨 위 줄이 `HRMS 백엔드 시작 (TestMode=False, 장비 158대, 압축기 246대)` |
| **[11장-5] 통신 상태** | `0`(연결됨)이 223에 가깝다. `1`(끊김)·`2`(재접속중)가 있으면 바로 아래 **연결 안 된 압축기 목록**을 네트워크 담당자에게 전달한다 |
| **[11장-6] 최근 10초 갱신** | 연결된 압축기 수와 비슷하다(조회 시점과 3초 주기가 어긋나면 조금 적게 나올 수 있다 — 몇 초 뒤 다시 실행) |
| **[11장-7] 최근 1분 트렌드** | 기동 2분 뒤부터 연결된 압축기 수만큼 |
| **[11장] 통신장애 경보** | 연결 안 되는 압축기가 30초 넘게 이어지면 압축기마다 한 건씩 쌓인다. 설치 직후 네트워크가 덜 열린 상태라면 한꺼번에 쌓이는 것은 정상이다 |
| **[참고] IP 없는 압축기** | 위 표의 13대 |

### 11.3 비상정지 테스트 — ⚠️ 현장 담당자 입회 필수

**비상정지는 실제 냉동기를 멈춘다.** 시험 중인 장비를 세우면 시험이 중단된다. 아래 조건을 지킨다.

1. 현장 담당자와 **시험 대상 장비와 시각을 미리 정한다.** 시험이 걸려 있지 않은 장비를 고른다.
2. **LG 배터리 #1~#7은 고르지 않는다** — 7개 셀이 TLC 하나를 공유해서 한 셀을 멈추면 **7개 셀이 함께 멈춘다**.
3. 화면에서 비상정지를 누른다(책임자급 이상 계정, 비밀번호 재확인). 약 1초 뒤 성공 메시지가 나온다.
4. **현장에서 장비가 실제로 멈췄는지 확인한다.** 화면에도 비상정지로 표시되는지 본다.
5. 해제를 누르고, 장비가 다시 돌 수 있는 상태로 돌아왔는지 현장에서 확인한다.
6. (가능하면) 현장 패널의 **비상정지 버튼을 사람이 직접 눌러서** 화면에 비상정지로 뜨는지 확인한다. 이 시스템은 TLC의 D1805 값으로 상태를 판정하는데, 버튼 조작이 D1805에 반영되는지는 실제 냉동기가 물린 환경에서 아직 확인하지 못했다. 반영되지 않으면 개발 쪽에 알린다.

수행 기록은 이벤트 로그(`EmergencyStop` 카테고리)에 명령문·응답·상태 확인 결과까지 남는다.

## 12. 주의 사항

| # | 주의 | 이유 |
|---|---|---|
| 1 | **`TestMode=False`인지 11.1-3으로 꼭 확인한다** | 테스트 모드로 뜨면 실제 장비와 통신하지 않고 모의값으로 화면이 **정상처럼** 돈다. 겉보기로는 구분되지 않는다 |
| 2 | **`appsettings.Development.json`을 서버에 두지 않는다**(3.1) | 개발용 설정이 섞여 들어갈 여지를 없앤다 |
| 3 | **개발 PC의 DB를 복사해 가지 않는다**(1장) | 테스트 계정·샘플 결재·테스트 일지가 같이 넘어간다 |
| 4 | **`seed_from_csv.sql`은 최초 설치 때 한 번만** 실행한다(7장) | 운영 중에 돌리면 일지·트렌드·이력이 전부 지워진다 |
| 5 | **`admin` 비밀번호를 첫 로그인 직후 바꾼다**(10장) | 최초 비밀번호가 문서에 공개되어 있다 |
| 6 | **비상정지 테스트는 입회 하에만**(11.3) | 실제 설비가 멈춘다. LG는 7개 셀이 함께 멈춘다 |
| 7 | **업데이트 때 `appsettings.json`과 `App_Data` 폴더를 덮어쓰지 않는다**(14장) | 설정이 초기화되고, `App_Data`에는 **첨부파일**(장비 사진·검사이력·교육훈련·공지·선해임 신고서)과 로그가 들어 있다 |
| 8 | **백업 기능이 아직 없다**(13장) | 수동으로 정기 백업한다 |
| 9 | **트렌드 자동 정리 기능이 아직 없다** | 1년에 약 13.4GB씩 쌓인다. 디스크 여유를 주기적으로 본다(대시보드의 저장소 사용량) |
| 10 | **JWT 키를 바꾸면 모든 사용자가 다시 로그인해야 한다** | 기존 로그인 토큰이 전부 무효가 된다 |
| 11 | **설정값 일괄 적용 API(api-manual 77·78번)는 쓰지 않는다** | 현장에서 장비별로 맞춘 설정을 한 번에 덮어쓸 위험이 있어 화면 반영을 보류했다(2026-10-01) |
| 12 | **경로에 한글을 쓰지 않는다**(2.1) | psql이 파일을 못 읽는 경우가 있다 |

## 13. 백업

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

복원(새 DB 기준):

```powershell
& "C:\Program Files\PostgreSQL\17\bin\pg_restore.exe" -h localhost -U hrms_app -d hrms --clean --if-exists "D:\HRMS_backup\hrms_YYYYMMDD_HHMM.dump"
```

**업데이트 직전(14장)과 장비 자료를 크게 바꾸기 전에는 반드시** 백업한다.

## 14. 새 버전으로 업데이트

**개발 PC**에서 3.1·3.2와 똑같이 게시 폴더와 스키마 SQL을 새로 만들고, `appsettings.Development.json`을 지운 뒤 서버의 `C:\HRMS_setup`으로 가져간다.

**현장 서버**:

```powershell
# 1) 백업 (13장)

# 2) 서비스 중지
Stop-Service HRMS

# 3) 새 파일 복사 — appsettings.json과 App_Data 폴더는 건너뛴다
robocopy C:\HRMS_setup\HRMS C:\HRMS /E /XF appsettings.json /XD App_Data

# 4) 스키마 변경 적용 (이미 적용된 부분은 건너뛰므로 변경이 없어도 실행해도 된다)
& $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\hrms_schema.sql

# 5) 서비스 시작과 확인
Start-Service HRMS
curl.exe http://localhost:5000/health
```

- **`seed_from_csv.sql`은 실행하지 않는다**(12장 4번).
- 새 버전에 `appsettings.json` 항목이 추가됐다면 릴리스 내용을 보고 기존 파일에 **손으로 추가**한다(덮어쓰지 않는다).
- 업데이트 뒤 11.1의 1·2·3·5번을 다시 확인한다.

## 15. 로그 위치

| 종류 | 위치 | 비고 |
|---|---|---|
| 파일 로그 | `C:\HRMS\App_Data\logs\hrms-yyyyMMdd.log` | 30일 보관(`Logging:File:RetentionDays`) |
| Windows 이벤트 로그 | 이벤트 뷰어 → Windows 로그 → 응용 프로그램 | Warning 이상. **기동 실패 원인은 여기서 본다** |
| 업무 이벤트 | DB `EventLogs` 테이블(화면의 이벤트·자료 조회) | 로그인·경보·통신장애·결재·비상정지·시스템 시작 |

API에서 오류가 나면 응답에 `traceId`가 실리고 같은 번호가 파일 로그에 남는다. 화면의 번호로 찾는다:

```powershell
findstr "추적번호" C:\HRMS\App_Data\logs\hrms-*.log
```

## 16. 문제 해결

| 증상 | 원인 / 조치 |
|---|---|
| 서비스가 시작하자마자 `Stopped` | 설정 문제. 이벤트 뷰어 → 응용 프로그램의 메시지를 본다 — "DB 연결 문자열이 비어 있습니다", "JWT 서명 키가 없습니다/너무 짧습니다"면 8.2. DB 비밀번호가 틀려도 여기서 멈춘다 |
| `/health`가 `503` | `database`가 이상이면 PostgreSQL 서비스·연결 문자열. `polling`이 이상이면 수집이 30초 넘게 멈춘 것 — 파일 로그 확인 |
| 서버에서는 되는데 다른 PC에서 접속 안 됨 | 8.2 `Urls`가 `http://0.0.0.0:5000`인지, 9.2 방화벽 규칙이 있는지 |
| 브라우저 개발자 도구에 CORS 오류 | 프론트 주소가 `Cors:AllowedOrigins`에 없다(8.2). 바꾼 뒤 `Restart-Service HRMS` |
| 압축기가 전부 "끊김" | ① 11.1-3에서 `TestMode=False`인지(테스트 모드는 전부 "연결됨"으로 보이므로 이 증상과는 반대) ② 서버에서 TLC로 TCP 5000이 나가는지 — 2.2 스크립트로 확인 ③ 서버 자체 방화벽의 아웃바운드 차단 |
| 일부 압축기만 "끊김" | 11.2의 목록을 네트워크 담당자에게 전달. 장비 전원·케이블·IP 변경 여부 확인. IP가 바뀌었으면 장비관리 화면에서 수정 |
| 로그인이 안 됨(`401`) | 아이디/비밀번호 확인. 일반 사용자는 시스템관리자가 조직관리에서 비밀번호를 초기화한다. **`admin`(시스템관리자) 비밀번호를 잊으면 개발 쪽 지원이 필요하다**(10장) |
| psql `No such file or directory`(경로는 맞는데) | 경로에 한글이 있다. `C:\HRMS_setup`처럼 영문 경로로 옮긴다 |
| psql에서 `"UTF8" 인코딩에서 사용할 수 없는 문자` | 명령줄 `-c` 쿼리에 한글이 들어갔다. `.sql` 파일로 저장해 `-f`로 실행한다(파일 안의 한글은 괜찮다) |
| psql `password 인증에 실패` | 비밀번호가 다르다. `postgres`(4장)와 `hrms_app`(5장)을 구분한다 |
| `public 스키마 접근 권한 없음`(6장) | 5장의 `GRANT ALL ON SCHEMA public` / `ALTER SCHEMA public OWNER` 두 줄이 빠졌다 |
| `"equipments" 이름의 릴레이션이 없습니다`처럼 테이블 이름이 소문자로 바뀜 | Windows PowerShell 5.1이 `psql -c` 인자 안의 큰따옴표를 지운 것이다. 쿼리를 `.sql` 파일로 저장해 `-f`로 실행한다(`check_install.sql`처럼). psql 안에 들어가서(부록 B) 직접 입력하는 것은 괜찮다 |

---

# 2부. 개발 PC 환경

## 17. 개발 DB 준비

PostgreSQL 설치와 DB·계정 생성은 4·5장과 같다. 개발 PC에서는 `hrms_app` 비밀번호를 `1234`로 쓰고 있다.

개발 PC에는 .NET 10 SDK와 EF Core 도구가 필요하다.

```powershell
dotnet tool install --global dotnet-ef     # 최초 1회
dotnet build
dotnet ef database update                   # 스키마 생성·갱신 (현장은 6장의 SQL 방식)
```

- 연결 문자열·JWT 키·테스트 모드 등 개발 설정은 `appsettings.Development.json`에 있다.
- 2026-09-18에 개발 중 쌓인 마이그레이션 31개를 `InitialCreate` 하나로 합쳤다. 이후 `AddEventLogIndexes`(09-28), `AddEmergencyStopStatus`(09-29)가 추가됐다. 합치기 전 이력은 git 히스토리에 있다.
- 새 마이그레이션은 `dotnet ef migrations add 이름`으로 개발 PC에서만 만든다. 현장에는 3.2의 스키마 SQL로 반영한다.
- 장비 데이터는 7장의 `seed_from_csv.sql`을 같은 방식으로 실행한다.
- 서버 실행: `dotnet run --launch-profile https`(https://localhost:7253 + http://localhost:5018. 두 포트를 같이 열면 5018은 7253으로 리다이렉트된다) 또는 `--launch-profile http`(5018만).

## 18. 테스트 모드

`appsettings.Development.json`의 `"Communication": { "TestMode": true }`면 실제 TCP 통신 없이 **전 압축기가 정상 통신하는 것으로 가정하고 모의값**을 채운다. 시간에 따라 완만하게 변하는 사인파(6시간 주기, 64채널 중 1개만 경보 범위를 벗어남)라서 통신 상태·경보 판정·장비 상태 집계·트렌드까지 실제와 같은 흐름으로 확인할 수 있다([program-flow.md](program-flow.md) 6장).

- 켜고 끄는 건 설정만 바꾸고 **재시작**하면 된다.
- 테스트 모드에서는 비상정지도 TLC에 명령을 보내지 않고 상태만 바꾼다.

## 19. 실기기 통신 테스트

개발 PC에서 접근 가능한 샘플 TLC가 **1대**(`59.16.212.252`) 있다.

### 19.1 권장 — 테스트 모드를 켠 채로 샘플 TLC만 실제 통신

`Communication:RealDeviceIps`에 적힌 IP의 압축기만 실제로 통신하고 나머지는 모의값을 쓴다(`Modules/Communication/CommunicationMode.cs`). **통신장애 경보가 쌓이지 않고 프론트가 계속 테스트할 수 있다.** 비상정지 명령도 이 목록의 TLC에만 실제로 나간다.

```json
"Communication": {
    "TestMode": true,
    "RealDeviceIps": [ "59.16.212.252" ]
}
```

```sql
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;   -- 원래 값 10.90.21.233
```

**현재는 원복된 상태다**(2026-10-01). 2026-09-29~10-01 동안 장비 1번(A지구 차량장비동 BSR #1)의 1번 압축기를 샘플 TLC에 연결해 프론트가 실제 센서값·비상정지를 시험했고, 지금은 원래 주소로 되돌리고 `RealDeviceIps`를 비워 두었다. 다시 붙일 때는 위 두 가지를 하고 재시작한다. 운영 환경은 `TestMode=false`라 이 목록을 보지 않는다.

### 19.2 전체 실모드로 확인 (예전 방법)

전체 실모드 동작을 봐야 할 때만 쓴다.

> **30초 규칙**: 실모드에서는 개발 PC가 닿지 않는 나머지 200여 대가 통신에 실패하고, **30초 넘게 이어지면 압축기마다 통신장애 경보가 쌓인다**(`CompressorPollingService.CommunicationFailureAlarmDelay`). 확인은 30초 안에 끝내고, 넘겼으면 4)의 정리를 반드시 한다.

1) 원래 IP와 이벤트 기준점을 적어 두고 테스트 IP로 바꾼다.

```sql
SELECT "Id", "IpAddress" FROM "Compressors" WHERE "Id" = 1;
SELECT max("Id") FROM "EventLogs";
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;
```

표준 주소(360/361/362/364/365/366/367)를 쓰는 압축기를 고르면 값까지 의미 있게 확인된다. LG 냉동기 압축기를 고르면 그 TLC에 없는 주소를 읽으므로 통신 성공 여부만 의미가 있다.

2) `TestMode`를 `false`로 바꾸고 재시작한다.

3) 값이 들어오는지 본다(3초 주기). `CommunicationStatus = 0`이고 `MeasuredAt`이 두 번 조회 사이에 바뀌면 성공이다.

```sql
SELECT "ChannelNo", "Value", "MeasuredAt" FROM "CompressorSensorCurrents" WHERE "CompressorId" = 1 ORDER BY "ChannelNo";
SELECT "Id", "CommunicationStatus", "HasCommunicationAlarm" FROM "Compressors" WHERE "Id" = 1;
```

4) 원복한다(반드시).

```sql
UPDATE "Compressors" SET "IpAddress" = '10.90.21.233' WHERE "Id" = 1;
DELETE FROM "EventLogs" WHERE "Category" = 2 AND "Id" > 기준_이벤트id;     -- 통신장애 경보 정리
DELETE FROM "CompressorMeasurements" WHERE "CompressorId" = 1 AND "MeasuredAt" >= '테스트 시작 시각';
```

`TestMode`를 `true`로 되돌리고 재시작한다.

### 19.3 장비 없이 프레임만 확인 — 가짜 TLC

로컬에 TCP 5000 포트로 응답만 돌려주는 프로그램(node 등)을 띄우고 압축기 IP를 `127.0.0.1`로 바꾸면 요청 프레임을 그대로 볼 수 있다. 응답은 `[STX]01RRD,OK,<4자리 16진수 × 개수><체크섬 2자리>[CR][LF]` 형식(체크섬은 STX/CR/LF를 뺀 문자들의 ASCII 합 mod 256).

### 19.4 확인 기록

| 날짜 | 시험 | 결과 |
|---|---|---|
| 09-18 | 프레임 형식(가짜 TLC + LG 압축기) | 센서 없는 CH03·CH06 자리에 더미 `0099`가 들어가고 그 두 채널은 저장 안 됨 |
| 09-18 | 실기기 단일 압축기 | 성공. 7채널이 3초마다 갱신(미세 변동 = 실제 센서값) |
| 09-18 | 실기기 동시 접속 42개 | 사이클마다 40~41대 성공, 1~2대 실패(약 3~5%). 연속 실패가 아니라 통신장애 경보는 안 뜸. **실제 LG TLC로 재확인 필요** — 현장에서 실패율이 높으면 IP별 동시 접속 수 제한을 검토한다 |
| 09-29 | 비상정지 쓰기·되읽기 | `01WRD,001,1805,0001` 정상 수락. **D1801은 반응하지 않아 판정을 D1805로 확정.** TLC가 쓰기 반영을 0.4~0.8초 뒤에 해서 되읽기를 재시도하도록 함(`Modules/EmergencyStop/README.md`) |

## 20. 테스트용 샘플 데이터

일괄 결재 화면 테스트용(개발 전용, **현장 서버에는 넣지 않는다**):

| 파일 | 역할 |
|---|---|
| `Infrastructure/Seed/sample_approvals.sql` | 단계별로 결재가 진행된 점검·운전·수리일지, 교육훈련 일지를 만든다. 여러 번 실행해도 중복 생성되지 않는다 |
| `Infrastructure/Seed/sample_approvals_reset.sql` | 테스트로 결재한 뒤 처음 상태로 되돌린다(문서 ID 유지) |

계정(개발 DB): `admin`/`1234`, `test1`/`1234`(책임자), 그 외 테스트 인원은 `test1234` — `safety19`(안전관리원), `manager01`(책임자), `chief01`(총괄자), `staff01`(일반관리원).

## 21. 장비 자료 갱신과 원본 대조 기록

### 21.1 파일 구성

| 파일 | 역할 |
|---|---|
| `Infrastructure/Equipments.csv` | 장비 158대 원본(UTF-8 BOM). 지역/건물/장비명/관리번호/압축기 수량/냉각수·브라인·전압 유무 |
| `Infrastructure/Compressors.csv` | 압축기 246대 원본. 소속 장비 id, IP, MAC |
| `Infrastructure/Seed/generate_seed_from_csv.js` | 위 두 CSV로 `seed_from_csv.sql`을 만든다 |
| `Infrastructure/Seed/seed_from_csv.sql` | 실제로 실행하는 SQL(생성물 — 직접 고치지 않는다) |

자료가 갱신되면 CSV를 교체하고 다시 만든다. 미운영 장비 목록과 운전전류 기준값은 스크립트 상단 상수(`NON_OPERATIONAL`, `RUNNING_CURRENT_THRESHOLD`)에 있다.

```powershell
node Infrastructure/Seed/generate_seed_from_csv.js
```

**운영 중인 현장 DB에는 다시 실행하지 않는다**(12장 4번). 운영 중에 장비 자료가 바뀌면 장비관리 화면에서 개별로 고친다.

### 21.2 시드가 채우는 값

- 장비 상태: 실차환경챔버 #1(id 9)과 수소충전소 냉동기 8대(id 111~118)는 `미운영`, 나머지 149대는 `운영`(2026-09-18)
- 운전전류 기준값: 전 장비 raw `100`(CH07 소수점 1자리 → 10.0A). 비어 있으면 운전 판정이 항상 정지가 되고 경보도 뜨지 않는다
- 압축기 순번: 장비별로 CSV의 압축기 id 순서대로 1부터(장비당 최대 6대)
- 채널 설정: CH01~03 ℃ 소수점 1자리, CH04~06 MPa 2자리, CH07 A 1자리. 상한 1000·하한 0, 지연 각 30초. LG 냉동기(배터리 #1~#7, 압축기 42대)는 CH03·CH06이 "사용 안 함"
- 통신·경보 상태는 `끊김`·`정상`으로 시작하고, 기동하면 3초 안에 실제 값으로 바뀐다
- id 시퀀스는 158, 246부터 발급되도록 맞춘다
- IP 하나를 여러 압축기가 공유하는 장비가 있다(TLC 한 대가 여러 압축기를 중계 — LG는 42대가 `10.90.87.217` 하나). 압축기별 레지스터 주소는 채널 설정(`RegisterAddress`)으로 관리한다

### 21.3 원본 엑셀과의 대조 (2026-09-18)

CSV는 `남양연구소 법정냉동기 현황_2026.09.01_삼원테크.xlsx`(시트 "남양 냉동기 보유현황")에서 만들었고, 한 줄씩 대조해서 아래를 바로잡았다.

| 수정 | 내용 |
|---|---|
| 상용설계동 7대 IP | `10.90.37.233~239` → `10.90.198.233~239`. 잘못된 값이 종합내구시험동 IP와 겹쳐 있었다 |
| 기후재현시험실 COMP#1 MAC | `...00:23` → `...00:2D` |
| 배기1동 환경챔버 #1/#2 IP | `10.90.163.232/233` → `10.90.162.232/233` |
| 인증시험동 MAC 2건 | 소크룸 냉동기#2 `...00:06` → `...04:06`, 저온챔버#2 냉동기#1 빈칸 → `98:06:37:70:00:73` |
| 브라인 유무 | 의미가 반대로 들어가 있었다. 직팽식이면 0, 냉수/브라인/Booster/Intercooler면 1 |
| 실차환경챔버 #1(장비 9) | 압축기 2대로 정정(IP/MAC 미정) |
| 시설동 이름(2026-10-01) | `총합내구시험동` → `종합내구시험동`(원본 작업 오타) |

의도적으로 원본과 다르게 둔 것: 시스템내구동 강설챔버의 IP(`10.90.152.239` 유지, 사용자 지시), 수소충전소 8대를 장비로 등록하고 미운영 처리, 엑셀의 "중앙통제시스템 연결여부" 불능 26대·신규 14대는 미반영(처리 방법 미정).

## 22. 개발 팁

### 22.1 로그로 값 확인하기 (`ILogger`)

```csharp
logger.LogInformation("channels: {Channels} {CompressorId}", channels, c.Id);
```

- `{이름}` 자리표시자는 이름이 아니라 **뒤에 나열한 인자 순서대로** 채워진다.
- 문자열 보간(`$"..."`)으로 미리 합치지 않는다 — 로그를 구조적으로 검색할 수 있게 `"{이름}", 값` 형태를 지킨다.
- 레벨: `Trace < Debug < Information < Warning < Error < Critical`. 개발 설정이 `Information`이라 그 이상만 보인다.
- 예외는 `logger.LogError(ex, "메시지")`처럼 첫 인자로 넘긴다.

`ILogger`는 콘솔과 파일 로그에만 남고 **DB에는 남지 않는다.** 화면의 이벤트로 남기려면 `EventLogger.LogAsync(db, category, message, username)`을 명시적으로 호출한다([Modules/Logging](../Modules/Logging/README.md)). 폴링처럼 매 사이클 도는 코드에 넣으면 DB가 금방 차니 일회성 디버깅에는 `ILogger`만 쓴다.

### 22.2 EF Core 쿼리 로그 줄이기

폴링이 3초마다 돌면서 SQL 로그가 콘솔을 가득 채우면 `Logging:LogLevel`에 `"Microsoft.EntityFrameworkCore": "Warning"`을 둔다(개발·운영 설정 모두 이미 들어 있다).

---

## 부록 A. 설정 항목 (`appsettings.json`)

| 항목 | 기본값 | 설명 |
|---|---|---|
| `Urls` | (없음 → `http://localhost:5000`) | 서비스 주소. 현장은 `http://0.0.0.0:5000`(8.2) |
| `ConnectionStrings:Default` | 빈 값 | **필수.** 비면 기동 안 함 |
| `Jwt:Key` / `Jwt:Issuer` | 빈 값 / `HRMS` | **Key 필수, 32바이트 이상** |
| `Cors:AllowedOrigins` | `[]` | 프론트가 다른 주소·포트면 그 주소. 개발 모드는 전부 허용 |
| `Communication:TestMode` | `false` | `true`면 모의값(현장은 반드시 `false`) |
| `Communication:TimeoutMs` | `1500` | TLC 연결·응답 타임아웃(ms). 폴링 주기(3초)보다 짧아야 한다 |
| `Communication:RealDeviceIps` | (없음) | 테스트 모드에서도 실제로 통신할 IP 목록. 개발 전용(19.1) |
| `FileStorage:RootPath` | `App_Data/attachments` | 첨부파일 저장 위치(실행 파일 기준 상대 경로 또는 절대 경로) |
| `SystemStatus:StorageDrive` | `C:\` | 대시보드 저장소 사용량을 볼 드라이브 = PostgreSQL 데이터 드라이브 |
| `Logging:File:Directory` / `RetentionDays` | `App_Data/logs` / `30` | 파일 로그 위치·보관 일수 |
| `Logging:LogLevel:*` | `Information`(EF Core·AspNetCore는 `Warning`) | 로그 레벨 |

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
