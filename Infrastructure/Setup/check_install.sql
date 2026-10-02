-- HRMS 설치 확인 스크립트 (Doc/setup.md 6·7·11장)
--
-- 실행:  & $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\check_install.sql
--
-- 확인용 쿼리를 파일로 둔 이유: Windows PowerShell 5.1은 psql -c로 넘기는 인자 안의
-- 큰따옴표를 지워서 "Equipments" 같은 테이블 이름이 소문자로 바뀌어 실패한다(2026-10-02 확인).
-- 읽기만 하는 쿼리라 언제 몇 번을 실행해도 된다. 설치 단계가 아직 덜 진행됐으면 해당
-- 항목이 0으로 나올 뿐이다.

\pset pager off

\echo
\echo ===== [6장] 스키마: 마이그레이션 3개가 나와야 한다 =====
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;

\echo ===== [7장] 장비 데이터: 158 / 246 / 1722 =====
SELECT (SELECT COUNT(*) FROM "Equipments") AS equipments,
       (SELECT COUNT(*) FROM "Compressors") AS compressors,
       (SELECT COUNT(*) FROM "CompressorChannelSettings") AS channel_settings;

\echo ===== [7장] 운영 장비 149 / 수집 대상 압축기 223 =====
SELECT (SELECT COUNT(*) FROM "Equipments" WHERE "Status" = 0) AS operating_equipments,
       (SELECT COUNT(*) FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
         WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL) AS collection_targets;

\echo ===== [11장-3] 기동 이벤트: TestMode=False 여야 한다 =====
SELECT "CreatedAt", "Message" FROM "EventLogs" WHERE "Category" = 4 ORDER BY "Id" DESC LIMIT 3;

\echo ===== [11장-5] 수집 대상 통신 상태 (0=연결됨 1=끊김 2=재접속중) =====
SELECT c."CommunicationStatus" AS status, COUNT(*) AS compressors
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL
GROUP BY 1 ORDER BY 1;

\echo ===== [11장-5] 연결 안 된 압축기 목록 (네트워크 담당자에게 전달) =====
SELECT e."Region", e."BuildingName", e."Name", c."SequenceNo", c."IpAddress"
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL AND c."CommunicationStatus" <> 0
ORDER BY 1, 2, 3, 4;

\echo ===== [11장-6] 최근 10초 안에 값이 갱신된 압축기 수 =====
SELECT COUNT(DISTINCT "CompressorId") AS updated_last_10s
FROM "CompressorSensorCurrents" WHERE "MeasuredAt" > now() - interval '10 seconds';

\echo ===== [11장-7] 최근 1분 트렌드 기록 수 =====
SELECT COUNT(*) AS trend_rows_last_1min
FROM "CompressorMeasurements" WHERE "MeasuredAt" > now() - interval '1 minute';

\echo ===== [11장] 통신장애 경보 이벤트 수 (최근 1시간) =====
SELECT COUNT(*) AS communication_alarms_last_1h
FROM "EventLogs" WHERE "Category" = 2 AND "CreatedAt" > now() - interval '1 hour';

\echo ===== [참고] IP가 없어 항상 끊김으로 보이는 운영 장비 압축기 (13대) =====
SELECT e."Region", e."BuildingName", e."Name", COUNT(*) AS no_ip_compressors
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NULL
GROUP BY 1, 2, 3 ORDER BY 1, 2, 3;
