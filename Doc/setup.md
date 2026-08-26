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

## 6. 초기 장비(Equipment) 데이터 시드

`Infrastructure/Seed/equipment_seed.sql`에 냉동장비 105개의 기본 데이터(지역/시설동명/장비명칭/운영상태만 채워진 상태, 나머지 필드는 비어있음)가 들어있다. 아래 명령으로 1회 실행한다.

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -f "Infrastructure\Seed\equipment_seed.sql"
Remove-Item Env:\PGPASSWORD
```

`INSERT 0 105`가 출력되면 정상. 이미 데이터가 있는 상태에서 다시 실행하면 중복 삽입되므로, 재실행 전에는 `Equipments` 테이블을 비우거나 건너뛴다.

### 확인

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c 'SELECT COUNT(*) FROM "Equipments";'
Remove-Item Env:\PGPASSWORD
```

`105`가 나오면 완료.

## 7. 초기 압축기(Compressor) 데이터 시드

`Infrastructure/Seed/compressor_seed.sql`에 압축기 244대의 기본 데이터(IP/MAC, 통신상태=끊김, 경보상태=정상)가 들어있다. `Equipments` 테이블을 시설동명+장비명칭으로 조인해서 `EquipmentId`를 채우는 방식이라, **6단계(장비 시드)가 먼저 끝나 있어야 한다.**

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -f "Infrastructure\Seed\compressor_seed.sql"
Remove-Item Env:\PGPASSWORD
```

`INSERT 0 244`가 출력되면 정상.

### 확인

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c 'SELECT COUNT(*) FROM "Compressors";'
Remove-Item Env:\PGPASSWORD
```

`244`가 나오면 완료. 이 중 52대는 원본 자료에 IP가 없어 `IpAddress`가 NULL인 상태다 (배터리시험셀2~7 대부분, 전기차 시험챔버, 인증시험3동 저온챔버 1/2호기 등).

## 8. 압축기 채널 설정(CompressorChannelSetting) 시드

압축기 1대당 CH01~CH07 7개 채널 설정 행이 필요하다. `Infrastructure/Seed/compressor_channel_setting_seed.sql`은 `Compressors` 테이블에 있는 모든 압축기에 대해 7행씩 기본값(`Enabled=true`, `AlarmEnabled=true`, 나머지는 NULL)으로 생성한다. **7단계(압축기 시드)가 먼저 끝나 있어야 한다.**

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -f "Infrastructure\Seed\compressor_channel_setting_seed.sql"
Remove-Item Env:\PGPASSWORD
```

`INSERT 0 1708`(압축기 244대 × 7채널)이 출력되면 정상.

### 확인

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -c 'SELECT COUNT(*) FROM "CompressorChannelSettings";'
Remove-Item Env:\PGPASSWORD
```

`1708`이 나오면 완료.

## 9. 경보/운전 판정 기본값 적용

`CompressorChannelSetting`의 경보 상/하한과 `Equipment`의 운전전류 임계값은 시드 직후엔 비어 있어서 판정이 되지 않는다. `Infrastructure/Seed/apply_alarm_defaults.sql`을 실행하면 전체 채널에 상한 1000·하한 0·표시소수점 1자리를 채우고, 전체 장비에 운전전류 임계값 10을 채운다. **8단계가 먼저 끝나 있어야 한다.**

```powershell
$env:PGPASSWORD = "1234"
$psql = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
& $psql -h localhost -p 5432 -U hrms_app -d hrms -f "Infrastructure\Seed\apply_alarm_defaults.sql"
Remove-Item Env:\PGPASSWORD
```

`UPDATE 1708`, `UPDATE 105`가 출력되면 정상. 실제 값이 정해지면 이 기본값은 웹 화면(또는 직접 SQL)에서 장비/채널별로 다시 조정하면 된다.

## 10. 테스트 모드 (실제 장비 네트워크 없이 전체 흐름 확인)

`appsettings.Development.json`의 `"Communication": { "TestMode": true }`가 켜져 있으면 실제 TCP 통신 없이 전 압축기가 정상 통신하는 것으로 가정하고 랜덤값을 채운다. 통신상태·경보판정·장비상태 집계까지 전부 실제로 동작하는 걸 확인할 수 있다 (자세한 동작은 [program-flow.md](program-flow.md) 6장 참고).

- 켜기/끄기는 설정값만 바꾸고 **앱 재시작**하면 된다.
- 운영용 `appsettings.json`은 기본 `false` — 실제 현장 배포 시에는 반드시 꺼진 상태인지 확인한다.

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

## 임시 테스트 상태 (현장 배포 전 원복 필요)

Communication 모듈(PC-Link 통신) 개발 중, 실제 압축기 네트워크에 접근할 수 없어서 **압축기 1번(`인증환경챔버&쇼크룸`, PT배기환경시험동)의 IP를 테스트용 IP로 임시 변경**해서 폴링 서비스(`CompressorPollingService`) 동작 검증에 사용하고 있다.

| 구분 | 값 |
|---|---|
| 압축기 | Id=1, `인증환경챔버&쇼크룸` (PT배기환경시험동) |
| 현재 IP (테스트용) | `59.16.212.252` |
| **원래 IP (현장 배포 전 복구 필요)** | `10.90.190.235` |

현장 배포 전에 반드시 아래 SQL로 원래 IP로 되돌려야 한다.

```sql
UPDATE "Compressors" SET "IpAddress" = '10.90.190.235', "CommunicationStatus" = 1 WHERE "Id" = 1;
```

(`CommunicationStatus = 1`은 `끊김` — 실제 폴링이 시작되기 전 기본 상태로 초기화하는 값이다.)

## 폴링 로그 조용히 하기 (선택)

`CompressorPollingService`가 1초마다 계속 도는데, 기본 로깅 설정상 EF Core가 실행하는 모든 SQL(SELECT/UPDATE)이 콘솔에 그대로 찍혀서 화면이 계속 스크롤된다. 에러는 아니고 정상 동작이지만, 개발 중 콘솔이 시끄러우면 `appsettings.Development.json`의 `Logging.LogLevel`에 아래 한 줄을 추가하면 EF Core 쿼리 로그가 사라지고 경고/에러만 남는다.

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
