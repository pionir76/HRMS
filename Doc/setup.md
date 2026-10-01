# DB 설치 및 초기 설정 가이드

현장 서버에서 처음부터 PostgreSQL을 설치하고 HRMS 백엔드가 연결되도록 설정하는 절차. 아래 순서대로 그대로 따라 하면 된다.

## 0. 사전 준비물

- Windows Server (또는 Windows 10/11)
- .NET 10 SDK 설치되어 있어야 함 (PowerShell에서 `dotnet --version` 실행 시 `10.x.x`가 나오면 정상)
- HRMS 프로젝트 소스 (이 저장소) 서버에 복사되어 있어야 함

## 1. PostgreSQL 17 설치

1. https://www.postgresql.org/download/windows/ 에서 Windows용 설치 파일(EDB installer)을 받는다. **버전 17.x**를 사용한다 (18은 너무 최신이라 EF Core용 Npgsql 드라이버 호환성이 아직 덜 검증됨).
2. 설치 마법사 진행:
    - **Installation Directory**: 기본값 유지 (`C:\Program Files\PostgreSQL\17`)
    - **Select Components**: `PostgreSQL Server`, `pgAdmin 4`, `Command Line Tools` 체크. `Stack Builder`는 체크 해제.
    - **Data Directory**: 기본값 유지
    - **Password**: `postgres` superuser 비밀번호 설정 — **반드시 기록해둘 것**
    - **Port**: 기본값 `5432` 유지
    - **Locale**: 기본값(`[Default locale]`) 유지
    - 설치 완료 후 Stack Builder 실행 여부를 묻는 화면은 체크 해제하고 닫는다.
3. 설치 확인 (PowerShell):

    ```powershell
    Get-Service -Name postgresql*
    ```

    `Running` 상태로 나오면 정상.

## 2. HRMS 전용 DB / 계정 생성

`postgres` superuser로 앱을 직접 연결하지 않고, 전용 계정을 만들어 사용한다. PowerShell에서 실행 (아래 `postgres`는 1단계에서 설정한 실제 superuser 비밀번호로 바꿔서 사용):

```powershell
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$env:PGPASSWORD = "postgres여기에실제비밀번호"

& $psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE hrms;"
& $psql -h localhost -p 5432 -U postgres -c "CREATE USER hrms_app WITH PASSWORD '1234';"
& $psql -h localhost -p 5432 -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE hrms TO hrms_app;"

# PostgreSQL 15부터 public 스키마 기본 권한이 제한되어 있어 별도로 부여해야 함
& $psql -h localhost -p 5432 -U postgres -d hrms -c "GRANT ALL ON SCHEMA public TO hrms_app;"
& $psql -h localhost -p 5432 -U postgres -d hrms -c "ALTER SCHEMA public OWNER TO hrms_app;"

Remove-Item Env:\PGPASSWORD
```

`hrms_app` 계정 비밀번호(`1234`)는 운영 환경에서는 더 안전한 값으로 바꾸는 것을 권장한다. 바꾸는 경우 3단계 연결 문자열의 `Password` 값도 동일하게 맞춰야 한다.

### 연결 확인

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c "SELECT current_database(), current_user;"
Remove-Item Env:\PGPASSWORD
```

`hrms | hrms_app`이 출력되면 정상.

## 3. 연결 문자열 설정

프로젝트 루트의 `appsettings.json`(운영 환경 공통 설정)에 연결 문자열을 추가한다. 개발 PC에서는 `appsettings.Development.json`에 넣고, 현장 운영 서버에서는 `appsettings.json`(또는 `appsettings.Production.json`)에 넣는다.

```json
{
    "ConnectionStrings": {
        "Default": "Host=localhost;Port=5432;Database=hrms;Username=hrms_app;Password=1234"
    }
}
```

- `Host`: DB가 앱과 같은 서버에 있다면 `localhost` 유지. 별도 DB 서버를 쓴다면 그 서버의 IP/호스트명으로 변경.
- **운영 서버에서는 `appsettings.json`의 `ConnectionStrings:Default`가 빈 값으로 들어 있다.** 채우지 않으면 앱이 기동하지 않고 "DB 연결 문자열이 비어 있습니다"로 즉시 종료된다(의도된 동작 — 10.1 참고). 환경변수 `ConnectionStrings__Default`로 줘도 된다.
- `Password`: 2단계에서 설정한 `hrms_app` 비밀번호와 동일해야 함.

## 4. .NET 프로젝트 빌드 및 EF Core 도구 설치

```powershell
cd "프로젝트 루트 경로"

# EF Core 마이그레이션 CLI 도구 (최초 1회만 설치하면 됨)
dotnet tool install --global dotnet-ef

# 패키지 복원 및 빌드 확인
dotnet build
```

`dotnet ef --version`을 실행했을 때 버전이 출력되면 도구 설치가 정상이다.

## 5. DB 스키마 생성 (마이그레이션 적용)

프로젝트에는 이미 `Migrations/` 폴더에 마이그레이션 코드가 포함되어 있다. 아래 명령으로 실제 DB에 테이블을 생성한다.

2026-09-18에 개발 중 쌓인 마이그레이션 31개를 **`InitialCreate` 하나로 합쳤다**(운영 서버 배포 전이라 이력을 보존할 이유가 없었고, 파일이 63개/946KB까지 늘어 있었다). 그래서 지금은 마이그레이션이 1개뿐이고, 새 서버에서는 아래 한 번으로 스키마 전체가 만들어진다. 합치기 전 이력은 git 히스토리에 남아 있다.

```powershell
dotnet ef database update
```

정상 완료되면 `Equipments`, `__EFMigrationsHistory` 테이블이 생성된다. 확인:

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c "\dt"
Remove-Item Env:\PGPASSWORD
```

> 향후 엔티티가 추가/변경되어 새 마이그레이션이 생기면, 서버에서는 `dotnet ef database update`만 다시 실행하면 된다 (`dotnet ef migrations add`는 개발 중에만 필요, 운영 서버에서는 실행하지 않음).

## 6. 장비/압축기 데이터 시드

장비·압축기·채널 설정을 한 번에 채운다. 예전에는 5개 스크립트를 순서대로 돌렸지만(장비 → 압축기 → 채널 설정 → 경보 기본값 → 채널명/순번), 2026-09-18 장비 자료가 최신본으로 교체되면서 **CSV에서 생성한 스크립트 하나**로 합쳤다.

| 파일 | 역할 |
|---|---|
| `Infrastructure/Equipments.csv` | 장비 158대 원본 자료(UTF-8 BOM). 지역/건물/장비명/관리번호/압축기수량/냉각수·브라인·전압 유무 |
| `Infrastructure/Compressors.csv` | 압축기 246대 원본 자료. 소속 장비 id, IP, MAC |
| `Infrastructure/Seed/generate_seed_from_csv.js` | 위 두 CSV를 읽어 아래 SQL을 생성하는 스크립트 |
| `Infrastructure/Seed/seed_from_csv.sql` | 실제로 실행하는 SQL(생성물) |

### 실행

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -f "Infrastructure\Seed\seed_from_csv.sql"
Remove-Item Env:\PGPASSWORD
```

`INSERT 0 158`(장비), `INSERT 0 246`(압축기), `INSERT 0 1722`(채널 설정 = 246 × 7)이 차례로 나오면 정상이다.

> **주의: 이 스크립트는 기존 데이터를 지우고 다시 넣는다.** 장비·압축기와 함께 트렌드 기록, 이벤트 로그, 점검/운전/수리일지, 장비 검사이력, 담당장비 지정, 장비 사진 첨부가 모두 삭제된다(장비 id 기준으로 묶여 있어 남길 수가 없다). 공지사항·교육훈련 일지·사용자 계정은 장비와 무관해서 남는다. 운영 중인 DB에 다시 돌릴 일이 생기면 **반드시 먼저 `pg_dump`로 백업**할 것. 전체가 하나의 트랜잭션이라 중간에 실패하면 아무것도 바뀌지 않는다.

### 시드가 채우는 값

- **장비 상태**: A지구 PDI 1동 `실차환경챔버 #1`(id 9)과 수소충전소 냉동기 8대(id 111~118)는 `미운영`, 나머지 149대는 `운영`(사용자 결정 2026-09-18). 운영이 아닌 장비는 수집도 노출도 되지 않는다(overview.md 4.1).
- **운전전류 임계값**: 전 장비 raw `100`(CH07 소수점 1자리이므로 실제 10.0 A). 이 값이 비어 있으면 운전 판정이 항상 정지가 되고, 그러면 경보도 발생하지 않는다.
- **압축기 순번(`SequenceNo`)**: 장비별로 CSV의 압축기 id 순서대로 1부터 부여(장비당 최대 6대).
- **채널 설정**: 압축기마다 CH01~07 7행 — 저온/고온/오일온도(℃, 소수점 1자리), 저압/고압/오일압력(MPa, 2자리), 운전전류(A, 1자리), 상한 1000·하한 0, 경보 발생/해제 지연 각 30초, 사용·경보 모두 켬.
  - 예외: **LG 냉동기(환경차개발시험2동 배터리 #1~#7, 압축기 42대)는 오일온도(CH03)·오일압력(CH06) 센서가 없어 그 84행만 "사용 안 함"(`Enabled=false`)으로 넣는다** — 그러지 않으면 읽히지도 않는 채널에 경보가 뜬다(2026-09-18).
- **통신/경보 상태**: 통신 `끊김`, 경보 `정상`, 운전 `false`로 시작한다(아직 폴링 전). 서버를 띄우면 3초 안에 실제 값으로 바뀐다.
- **id 시퀀스**: 158, 246부터 발급되도록 맞춰서 이후 API 등록과 충돌하지 않는다.

IP가 없는 압축기 23대(수소충전소 8대, 전기차환경시험동 6대 등)는 실운영 모드에서 수집 대상에서 제외된다. 또 IP 하나를 여러 압축기가 공유하는 장비가 있는데(TLC 한 대가 압축기 여러 대를 중계), 압축기별 레지스터 주소는 채널 설정(`RegisterAddress`)으로 관리하며 시드가 채운다 — `Modules/Communication/README.md` 참고.

### 원본 엑셀과의 대조 (2026-09-18)

CSV는 `Infrastructure/남양연구소 법정냉동기 현황_2026.09.01_삼원테크.xlsx`(시트 "남양 냉동기 보유현황", 데이터 행 3~243)에서 만들었다. 원본과 한 줄씩 대조해서 아래를 바로잡았다.

| 수정 | 내용 |
|---|---|
| 상용설계동 7대 IP | `10.90.37.233~239` → `10.90.198.233~239`. 잘못된 값이 종합내구시험동 IP와 겹쳐 있었다(IP 중복 6건의 원인) |
| 기후재현시험실 COMP#1 MAC | `...00:23` → `...00:2D` |
| 배기1동 환경챔버 #1/#2 IP | `10.90.163.232/233` → `10.90.162.232/233` |
| 인증시험동 MAC 2건 | 소크룸 냉동기#2 `...00:06` → `...04:06`, 저온챔버#2 냉동기#1 빈칸 → `98:06:37:70:00:73` |
| 브라인유무 | 의미가 반대로 들어가 있었다. 원본 `브라인 유무`(T열)가 **직팽식이면 0**, **냉수/브라인/Booster/Intercooler면 1**이다(148건 수정, 현재 1이 37대) |
| 실차환경챔버 #1(장비 9) | 압축기 2대로 정정(IP/MAC 미정) |

의도적으로 원본과 다르게 둔 것:

- **시스템내구동 강설챔버**: 원본은 IP가 "신규"(미정)인데 CSV는 `10.90.152.239`를 유지한다(사용자 지시).
- **수소충전소 8대**: 원본에는 "26년도 증설 예정(총 8기, 4기분 추가)" 메모 한 줄뿐이지만 장비 8대로 등록하고 상태는 미운영으로 둔다.
- **배터리 #1~#7의 IP 공유**: 원본도 42행 전부 `10.90.87.217`이다. 오류가 아니라 TLC 한 대가 압축기 여러 대를 중계하는 구조다(`Modules/Communication/README.md`).
- **엑셀 `중앙통제시스템 연결여부`(U열)의 불능 26대·신규 14대**는 CSV/DB에 반영하지 않았다 — 어떻게 다룰지 미정.

대조에 쓴 스크립트는 저장소에 넣지 않았다(1회성). 자료가 또 갱신되면 같은 항목(IP 대역·MAC·브라인 방향)을 다시 확인할 것.

### 자료가 다시 갱신되면

CSV를 새 것으로 교체한 뒤 SQL을 다시 생성하고 실행한다.

```powershell
node Infrastructure/Seed/generate_seed_from_csv.js
```

미운영 장비 목록과 운전전류 임계값은 이 스크립트 상단 상수(`NON_OPERATIONAL`, `RUNNING_CURRENT_THRESHOLD`)에 있다. 생성된 SQL은 직접 고치지 말 것 — 다음 생성 때 덮어써진다.

### 확인

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c 'SELECT (SELECT COUNT(*) FROM "Equipments") eq, (SELECT COUNT(*) FROM "Compressors") comp, (SELECT COUNT(*) FROM "CompressorChannelSettings") ch;'
Remove-Item Env:\PGPASSWORD
```

`158 | 246 | 1722`가 나오면 완료.

## 7. 로그인 인증 설정 (JWT 서명 키)

로그인 토큰(JWT) 서명에 쓰는 비밀 키를 `appsettings.json`(운영) 또는 `appsettings.Development.json`(개발)에 넣어야 한다. 개발 환경에는 이미 키가 들어있지만, **운영 서버에는 별도로 새 키를 생성해서 넣어야 한다** (개발용 키를 그대로 쓰면 안 됨).

```json
{
  "Jwt": {
    "Key": "여기에 32자 이상의 무작위 문자열",
    "Issuer": "HRMS"
  }
}
```

**운영 서버에서는 이 값이 비어 있으면 앱이 기동하지 않는다** — "JWT 서명 키가 없습니다"로 즉시 종료된다. 32바이트 미만이면 "너무 짧습니다"로 역시 기동하지 않는다(그냥 두면 앱은 뜨고 로그인만 전부 실패하는 형태로 뒤늦게 드러나기 때문이다).

키 생성 예시 (PowerShell):

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

앱을 처음 실행하면(마이그레이션 적용 후) `Users` 테이블이 비어있을 경우 관리자 계정이 자동으로 하나 생성된다.

| 아이디 | 임시 비밀번호 |
|---|---|
| `admin` | `admin1234` |

**최초 로그인 후 반드시 비밀번호를 바꿀 것** (현재는 비밀번호 변경 API가 없으므로, DB에서 직접 새 해시 값으로 갱신하거나 추후 추가될 사용자 관리 기능을 사용).

## 8. 테스트 모드 (실제 장비 네트워크 없이 전체 흐름 확인)

`appsettings.Development.json`의 `"Communication": { "TestMode": true }`가 켜져 있으면 실제 TCP 통신 없이 전 압축기가 정상 통신하는 것으로 가정하고, 시간에 따라 완만하게 변하는 모의값(사인파 6시간 주기, 64채널 중 1개만 경보 범위 이탈)을 채운다. 통신상태·경보판정·장비상태 집계까지 전부 실제로 동작하는 걸 확인할 수 있다 (자세한 동작은 [program-flow.md](program-flow.md) 6장 참고).

- 켜기/끄기는 설정값만 바꾸고 **앱 재시작**하면 된다.
- 운영용 `appsettings.json`은 기본 `false` — 실제 현장 배포 시에는 반드시 꺼진 상태인지 확인한다.

## 9. 실기기 통신 테스트 (TLC 1대로 확인)

테스트 모드는 실제 통신을 하지 않으므로, 프레임이 실제 장비에 먹히는지는 실기기로 확인해야 한다. 현재 개발 PC에서 접근 가능한 TLC가 **1대**(`59.16.212.252`) 있다.

### 9.0 권장 — 테스트 모드를 켠 채로 샘플 TLC만 실제 통신 (2026-09-29)

`Communication:RealDeviceIps`에 적힌 IP의 압축기만 실제로 통신하고, 나머지는 모의값을 그대로 쓴다. **통신장애 경보가 쌓이지 않아 30초 규칙이 필요 없고, 프론트가 계속 테스트할 수 있다.** 비상정지 명령도 이 목록에 있는 TLC에만 실제로 나간다.

```json
"Communication": {
    "TestMode": true,
    "RealDeviceIps": [ "59.16.212.252" ]
}
```

```sql
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;   -- 원래 값 10.90.21.233
```

**현재는 원복된 상태다**(2026-10-01). 2026-09-29~10-01 동안 장비 1번 **A 지구 · 차량장비동 · BSR #1**의 1번 압축기(압축기 ID 1)를 샘플 TLC에 연결해 프론트가 실제 센서값·비상정지를 시험했고, 지금은 원래 주소(`10.90.21.233`)로 되돌리고 `RealDeviceIps`를 비워 두었다. 다시 붙일 때는 위 두 가지를 반대로 하면 된다.

원복할 때는 IP를 `10.90.21.233`으로 되돌리고 `RealDeviceIps`를 비운다. 운영 환경은 `TestMode`가 `false`라 이 목록을 보지 않는다.

아래 1)~4)는 **테스트 모드를 통째로 끄는** 예전 방법이다. 전체 실모드 동작을 봐야 할 때만 쓴다.

> **30초 규칙**: 실운영 모드에서는 나머지 200여 대가 통신에 실패한다. 실패가 **30초 넘게 지속되면 압축기마다 통신 장애 경보 이벤트가 쌓인다**(`CompressorPollingService.CommunicationFailureAlarmDelay`). 확인은 30초 안에 끝내고, 넘겼으면 4)의 정리를 반드시 한다.

### 1) 원래 IP와 이벤트 기준점을 기록하고 테스트 IP로 바꾼다

```sql
SELECT "Id", "IpAddress" FROM "Compressors" WHERE "Id" = 1;   -- 원래 값을 적어둔다
SELECT max("Id") AS 기준_이벤트id FROM "EventLogs";            -- 정리 기준점
UPDATE "Compressors" SET "IpAddress" = '59.16.212.252' WHERE "Id" = 1;
```

표준 주소(360/361/362/364/365/366/367)를 쓰는 압축기를 고르면 값까지 의미 있게 확인된다. LG 냉동기 압축기를 고르면 그 TLC에 없는 주소를 읽으므로 **통신 성공 여부만** 의미가 있다.

### 2) 테스트 모드를 끄고 재시작한다

`appsettings.Development.json`의 `"Communication": { "TestMode": false }`로 바꾸고 앱을 재시작한다.

### 3) 값이 들어오는지 확인한다 (3초 주기)

```sql
SELECT "ChannelNo", "Value", "MeasuredAt" FROM "CompressorSensorCurrents" WHERE "CompressorId" = 1 ORDER BY "ChannelNo";
SELECT "Id", "CommunicationStatus", "HasCommunicationAlarm" FROM "Compressors" WHERE "Id" = 1;
```

`CommunicationStatus = 0`(연결됨)이고 `MeasuredAt`이 계속 갱신되면 성공이다. 몇 초 간격으로 두 번 조회해서 값이 미세하게 움직이면 실제 센서값임이 확실해진다. **조회 시점과 폴링 주기가 어긋나면 "최근 3초 내 갱신"이 0으로 보일 수 있으니**, 갱신 여부는 `MeasuredAt` 값 자체를 두 번 비교해서 판단한다.

### 4) 원복 (반드시)

```sql
UPDATE "Compressors" SET "IpAddress" = '10.90.21.233' WHERE "Id" = 1;   -- 1)에서 적어둔 원래 값
DELETE FROM "EventLogs" WHERE "Category" = 2 AND "Id" > 기준_이벤트id;   -- 통신 장애 경보(Category 2) 정리
DELETE FROM "CompressorMeasurements"                                     -- 엉뚱한 장비로 기록된 트렌드 정리
WHERE "CompressorId" = 1 AND "MeasuredAt" >= '테스트 시작 시각';
```

`TestMode`를 `true`로 되돌리고 재시작한다. 통신 상태와 `HasCommunicationAlarm`은 다음 사이클에 자동 정상화된다.

### 실제 장비 없이 프레임만 확인하려면 — 가짜 TLC

로컬에 TCP 5000 포트로 응답만 돌려주는 프로그램(node 등)을 띄우고 압축기 IP를 `127.0.0.1`로 바꾸면 **요청 프레임을 그대로 눈으로 볼 수 있다** — 레지스터 주소·개수·체크섬 확인용이다. 응답은 `[STX]01RRD,OK,<4자리 16진수 × 개수><체크섬2자리>[CR][LF]` 형식으로 만들어 주면 된다(체크섬은 STX/CR/LF를 뺀 문자들의 ASCII 합 mod 256).

### 확인 기록 (2026-09-18)

| 시험 | 방법 | 결과 |
|---|---|---|
| 프레임 형식 | 가짜 TLC + LG 압축기 1대 | `01RRD,008,0360,0361,0099,0362,0363,0099,0384,1801` + 체크섬 — 센서 없는 CH03·CH06 자리에 더미 `0099`가 들어가고, 그 두 채널은 값이 저장되지 않음(5채널만 기록) |
| 실기기 단일 | 압축기 1번(표준 주소)을 `59.16.212.252`로 변경 | **성공**. 연결됨 + 7채널이 3초마다 갱신(150↔151, 628↔629처럼 미세 변동 = 실제 센서값) |
| 실기기 동시 접속 42 | LG 42대를 모두 `59.16.212.252`로 변경 | **대부분 성공**. 사이클마다 40~41대 연결됨 / 1~2대 실패. 값 저장·통신 상태 갱신 정상, 통신 장애 경보는 뜨지 않음(실패가 연속되지 않음) |

**동시 접속 42개에 대한 판단**: TLC가 동시 접속을 적게 제한하지는 않지만, 사이클마다 1~2대는 실패한다(약 3~5%). 실패가 30초 이상 연속되지 않으면 경보는 뜨지 않으므로 현재 구조(압축기 개별 통신)로 운영 가능하다. 다만 값이 3초마다 한 번씩 빠지는 압축기가 생기므로, 현장에서 실패율이 더 높아지면 **IP별 동시 실행 수 제한**(예: IP당 4~8개)을 넣는 것을 검토한다. 이 시험은 LG 실기기가 아니라 다른 TLC 1대에 42개를 몰아넣은 것이므로, 실제 LG TLC로 재확인이 필요하다.

## 10. 운영 서버 배포 (Windows 서비스)

개발 PC에서는 `dotnet bin/Debug/net10.0/HRMS.dll`로 띄우지만, 현장 서버에서는 게시(publish)한 결과물을 Windows 서비스로 등록한다.

> **폐쇄망 전제**: 이 시스템은 남양연구소 내부망에서만 접근되며 외부 공격은 고려 대상이 아니다(사용자 확인 2026-09-28). 그래서 HTTPS·브루트포스 방어·시크릿 관리보다 **기동 실패와 설정 실수**를 막는 쪽에 무게를 둔다.

### 10.1 필수 설정 — 없으면 기동하지 않는다

앱은 시작할 때 아래 두 값을 검사하고, 없거나 잘못되면 **원인을 한국어로 밝히고 즉시 종료**한다(`Program.cs`).

| 설정 | 없을 때 메시지 | 비고 |
|---|---|---|
| `ConnectionStrings:Default` | "DB 연결 문자열이 없습니다 …" | |
| `Jwt:Key` | "JWT 서명 키가 없습니다 …" | **32바이트 이상**이어야 한다. 짧으면 "JWT 서명 키가 너무 짧습니다(현재 N바이트)" |

`appsettings.json`에는 두 항목이 **빈 값으로** 들어 있다. 운영 서버에서 채우는 방법은 두 가지다.

**(A) 파일에 직접 적기** — 간단하지만 값이 저장소에 커밋될 수 있으니 주의한다.

```json
{
  "ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=hrms;Username=hrms_app;Password=실제비밀번호" },
  "Jwt": { "Key": "32바이트 이상 무작위 문자열", "Issuer": "HRMS" }
}
```

**(B) 환경변수로 주입(권장)** — 구분자가 **밑줄 두 개**다.

```powershell
[Environment]::SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;...", "Machine")
[Environment]::SetEnvironmentVariable("Jwt__Key", "32바이트 이상 무작위 문자열", "Machine")
```

키 생성:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### 10.2 게시

```powershell
cd "프로젝트 루트"
dotnet publish -c Release -o C:\HRMS
```

`C:\HRMS\HRMS.exe`가 생성된다. `appsettings.json`도 함께 복사되므로 (A) 방식이면 이 파일을 수정한다.

### 10.3 서비스 등록

```powershell
New-Service -Name HRMS -BinaryPathName "C:\HRMS\HRMS.exe" -DisplayName "HRMS 냉동기 모니터링" -StartupType Automatic
Start-Service HRMS
Get-Service HRMS
```

장애 시 자동 재시작(1·2회 실패는 1분 뒤 재시작, 이후 재시작):

```powershell
sc.exe failure HRMS reset= 86400 actions= restart/60000/restart/60000/restart/60000
```

제거는 `Stop-Service HRMS; sc.exe delete HRMS`.

### 10.4 포트와 주소

`ASPNETCORE_URLS`로 정한다(기본 5000). 예: 5000 포트로 서비스하려면

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://0.0.0.0:5000", "Machine")
New-NetFirewallRule -DisplayName "HRMS API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

HTTPS는 쓰지 않는다. HTTPS 리다이렉트는 개발 환경에서만 걸리도록 되어 있어(`Program.cs`), 운영에서 HTTP로 서비스해도 리다이렉트로 요청이 깨지지 않는다.

### 10.5 프론트가 다른 포트에 있으면 CORS를 열어야 한다

브라우저는 **포트만 달라도 다른 origin**으로 보기 때문에, 폐쇄망이어도 CORS가 없으면 프론트의 API 호출이 전부 차단된다.

| 배포 형태 | 설정 |
|---|---|
| 프론트를 API와 같은 주소·포트에서 서빙 | 설정 불필요(`Cors:AllowedOrigins`를 비워 둔다) |
| 프론트가 다른 포트·다른 호스트 | 그 주소를 `Cors:AllowedOrigins`에 넣는다 |

```json
{ "Cors": { "AllowedOrigins": [ "http://10.90.x.x:8080" ] } }
```

개발 환경(`Development`)에서는 모든 origin이 허용되므로 지금처럼 한 PC에서 서버와 웹을 같이 띄워 테스트하는 동안은 설정할 것이 없다. **배포 시점에 프론트 주소가 정해지면 그때 채우면 된다.**

### 10.6 배포 전 체크리스트

| 확인 | 왜 |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Production`인가 | **가장 중요.** Development로 뜨면 `TestMode=true`라 실제 장비와 통신하지 않고 **사인파 모의값**으로 화면이 정상처럼 돈다. 겉보기로는 구분이 안 된다 |
| `appsettings.json`의 `Communication:TestMode`가 `false`인가 | 위와 같은 이유 |
| 연결 문자열·JWT 키를 넣었는가 | 없으면 기동하지 않는다(10.1) |
| `SystemStatus:StorageDrive`가 실제 DB 드라이브인가 | 대시보드 저장소 사용량이 엉뚱한 디스크를 가리킨다 |
| 마이그레이션을 적용했는가(`dotnet ef database update`) | 앱은 자동 적용하지 않는다. 안 하면 첫 쿼리에서 "column does not exist" |
| 시드를 실행했는가(6단계) | 장비·압축기·채널 설정이 비어 있으면 수집할 대상이 없다 |
| 프론트 주소를 `Cors:AllowedOrigins`에 넣었는가 | 다른 포트면 필수(10.5) |
| `admin` 비밀번호를 바꿨는가 | 최초 기동 시 `admin`/`admin1234`로 생성된다 |

### 10.7 로그 위치

| 종류 | 위치 | 비고 |
|---|---|---|
| 파일 로그 | `{설치 폴더}\App_Data\logs\hrms-yyyyMMdd.log` | 기본 30일 보관(`Logging:File:RetentionDays`). 레벨은 `Logging:LogLevel` 설정을 따른다 |
| Windows 이벤트 로그 | 이벤트 뷰어 → Windows 로그 → 응용 프로그램 | `UseWindowsService()`가 자동 등록한다. Warning 이상만 남는다 |
| DB 이벤트 | `EventLogs` 테이블 | 로그인/경보/통신장애 등 업무 이벤트(화면에서 조회) |

API에서 예외가 나면 응답에 `traceId`가 실리고 같은 번호가 파일 로그에 남는다. 사용자가 화면의 번호를 알려주면 `findstr "번호" App_Data\logs\hrms-*.log`로 바로 찾을 수 있다.

### 10.8 기동 확인

```powershell
Get-Service HRMS                      # Running 인지
curl http://localhost:5000/health     # 인증 없이 상태 확인 (정상 200, DB/수집 이상이면 503)
curl http://localhost:5000/api/auth/login -Method Post -Body '{"username":"admin","password":"..."}' -ContentType "application/json"
```

서비스가 바로 중지된다면 설정 문제일 가능성이 높다. **이벤트 뷰어 → Windows 로그 → 응용 프로그램**에서 위 한국어 오류 메시지를 확인한다. DB에 남는 "시스템 시작" 이벤트는 기동이 끝난 뒤에 기록되므로, 기동 실패 시에는 DB에 아무 흔적이 없다.

## 문제 해결

| 증상                                                                                     | 원인 / 조치                                                                                                                                                                         |
| ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `password 인증에 실패했습니다`                                                           | 비밀번호가 실제 설정값과 다름. 1단계에서 설정한 `postgres` 비밀번호를 다시 확인.                                                                                                    |
| `public 스키마(schema) 접근 권한 없음` (마이그레이션 적용 시)                            | PostgreSQL 15부터 `public` 스키마 기본 권한이 제한됨. 2단계의 `GRANT ALL ON SCHEMA public` / `ALTER SCHEMA public OWNER TO hrms_app` 명령이 빠졌을 가능성 — 다시 실행.              |
| `dotnet ef` 명령을 찾을 수 없음                                                          | `dotnet tool install --global dotnet-ef` 미실행 또는 PATH 미반영. 새 터미널을 열어 다시 시도.                                                                                       |
| `Did not find any relations` (`\dt` 결과 비어있음)                                       | 5단계 마이그레이션 적용이 안 된 상태. `dotnet ef database update` 재실행.                                                                                                           |
| 테이블명을 넣은 쿼리인데 `relation "equipments" does not exist`처럼 소문자로 바뀌어 나옴 | PowerShell에서 `-c` 뒤 SQL을 큰따옴표로 감싸면 내부의 `"Equipments"` 큰따옴표가 깨진다. 이 문서의 예시처럼 `-c '...'` 형태로 **작은따옴표로 감싸고 내부는 큰따옴표**를 그대로 쓴다. |

## 기타 참고 항목

````DB 접속 및 기본 명령어
$env:PGPASSWORD = "1234"
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -p 5432 -U hrms_app -d hrms

```테이블 목록보기
\dt

```특정 테이블 구조 보기
\d "Equipments"

```실제 데이터 조회
SELECT * FROM "Equipments" LIMIT 5;

```나가기
\q
````

## 실제 설치 시 변경 필요 항목

| 항목 | 위치 | 현재값(개발 PC) | 설치 시 할 일 |
|---|---|---|---|
| 시스템 정보 저장소 대상 드라이브 | `appsettings.json` → `SystemStatus:StorageDrive` | `C:\` | 운영 서버의 **PostgreSQL 데이터 폴더가 있는 드라이브**로 변경. 확인: `Get-CimInstance Win32_Service -Filter "Name like 'postgres%'"`의 `PathName`에서 `-D` 뒤 경로 |
| 수집 서비스 `주의` 판정 기준 | `Modules/SystemStatus/Controllers/SystemStatusController.cs` → `CollectionWarningAge` | 10초 | 실제 장비에서 응답 없는 압축기가 있으면 사이클이 약 9~10초까지 늘어 `주의`가 잦을 수 있다. 확인 후 필요하면 늘린다 |
| DB 백업 | — | 기능 없음(`databaseBackup: null`) | 백업 정책 결정 후 구현 |

자세한 배경은 `Modules/SystemStatus/README.md` 참고.

## 폴링 로그 조용히 하기 (선택)

`CompressorPollingService`가 3초마다 계속 도는데, 기본 로깅 설정상 EF Core가 실행하는 모든 SQL(SELECT/UPDATE)이 콘솔에 그대로 찍혀서 화면이 계속 스크롤된다. 에러는 아니고 정상 동작이지만, 개발 중 콘솔이 시끄러우면 `appsettings.Development.json`의 `Logging.LogLevel`에 아래 한 줄을 추가하면 EF Core 쿼리 로그가 사라지고 경고/에러만 남는다.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

운영 환경(`appsettings.json`)에도 동일하게 적용하면 실제 배포 후에도 조용해진다.

## 개발 중 값 확인하기 (`ILogger`)

`dotnet run`으로 띄운 터미널에서 특정 값을 그때그때 확인하고 싶을 때는 `logger.LogInformation(...)`을 쓴다. `CompressorPollingService`, `TrendRecordingService` 등 대부분의 `BackgroundService`가 생성자로 `ILogger<T> logger`를 이미 받고 있으니, 그 안에서 바로 호출하면 된다.

```csharp
logger.LogInformation("channels: {Channels} {CompressorId}", channels, c.Id);
```

- `{이름}` 자리표시자는 이름이 아니라 **뒤에 나열한 인자 순서대로** 채워진다. 위 예시는 `{Channels}` ← `channels`, `{CompressorId}` ← `c.Id` 순서다.
- 문자열 보간(`$"channels: {channels}"`)으로 미리 합쳐서 넘기지 않는다. `"{이름}", 값` 형태를 지켜야 나중에 로그를 구조적으로 검색/필터링할 수 있다.
- 로그 레벨은 `LogTrace < LogDebug < LogInformation < LogWarning < LogError < LogCritical` 순이다. `appsettings.Development.json`의 `Logging:LogLevel:Default`가 `Information`이라 `LogInformation`부터는 바로 보이고, `LogDebug`/`LogTrace`는 설정을 더 낮춰야 보인다.
- 예외를 같이 남기고 싶으면 `logger.LogError(ex, "메시지")`처럼 예외 객체를 첫 인자로 넘긴다(스택트레이스까지 같이 찍힘).

**DB에는 기록되지 않는다.** `ILogger`는 기본적으로 콘솔에만 출력되고, 터미널을 닫으면 사라진다. DB `EventLogs` 테이블에 실제로 남기려면 [Modules/Logging](../Modules/Logging/README.md)의 `EventLogger.LogAsync(db, category, message, username)`를 명시적으로 호출해야 한다 — 이 둘은 완전히 별개의 경로다(현재 `EventLogger`는 로그인/로그아웃 두 곳에서만 호출됨). 폴링처럼 매 사이클 도는 코드에 `EventLogger`를 함부로 넣으면 DB에 로그가 급격히 쌓이니, 일회성 디버깅에는 `ILogger`만 쓰는 게 맞다.
