-- HRMS 설치 확인 스크립트 (Doc/setup.md 6·7·11·12장)
--
-- 실행:  & $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\check_install.sql
--
-- 확인용 쿼리를 파일로 둔 이유: Windows PowerShell 5.1은 psql -c로 넘기는 인자 안의
-- 큰따옴표를 지워서 "Equipments" 같은 테이블 이름이 소문자로 바뀌어 실패한다(2026-10-02 확인).
-- 읽기만 하는 쿼리라 언제 몇 번을 실행해도 된다. 설치 단계가 아직 덜 진행됐으면 해당
-- 항목이 0으로 나올 뿐이다. [ ] 안의 이름은 setup.md에서 가리키는 항목 이름이다.

\pset pager off

\echo
\echo ===== [스키마] 마이그레이션 6개가 나와야 한다 (마지막: ResetRepairLogApprovals) =====
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;

\echo ===== [장비 데이터] 158 / 246 / 1722 =====
SELECT (SELECT COUNT(*) FROM "Equipments") AS equipments,
       (SELECT COUNT(*) FROM "Compressors") AS compressors,
       (SELECT COUNT(*) FROM "CompressorChannelSettings") AS channel_settings;

\echo ===== [채널 이름] 흡입온도/토출온도/오일온도/흡입압력/토출압력/오일압력/운전전류 각 246 =====
\echo (저온/고온/저압/고압이 보이면 예전 seed_from_csv.sql로 넣은 것이다 — setup.md 12.0)
SELECT "ChannelNo", "ChannelName", COUNT(*) AS compressors
FROM "CompressorChannelSettings" GROUP BY 1, 2 ORDER BY 1, 2;

\echo ===== [장비 데이터] 운영 장비 149 / 수집 대상 압축기 223 =====
SELECT (SELECT COUNT(*) FROM "Equipments" WHERE "Status" = 0) AS operating_equipments,
       (SELECT COUNT(*) FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
         WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL) AS collection_targets;

\echo ===== [기동 이벤트] 테스트 서버는 TestMode=True, 현장은 TestMode=False =====
SELECT "CreatedAt", "Message" FROM "EventLogs" WHERE "Category" = 4 ORDER BY "Id" DESC LIMIT 3;

\echo ===== [통신 상태] 수집 대상 압축기 (0=연결됨 1=끊김 2=재접속중) =====
SELECT c."CommunicationStatus" AS status, COUNT(*) AS compressors
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL
GROUP BY 1 ORDER BY 1;

\echo ===== [통신 상태] 연결 안 된 압축기 목록 (현장: 네트워크 담당자에게 전달) =====
SELECT e."Region", e."BuildingName", e."Name", c."SequenceNo", c."IpAddress"
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NOT NULL AND c."CommunicationStatus" <> 0
ORDER BY 1, 2, 3, 4;

\echo ===== [최근 10초 갱신] 값이 갱신된 압축기 수 =====
SELECT COUNT(DISTINCT "CompressorId") AS updated_last_10s
FROM "CompressorSensorCurrents" WHERE "MeasuredAt" > now() - interval '10 seconds';

\echo ===== [최근 1분 트렌드] 기록 수 =====
SELECT COUNT(*) AS trend_rows_last_1min
FROM "CompressorMeasurements" WHERE "MeasuredAt" > now() - interval '1 minute';

\echo ===== [통신장애 경보] 최근 1시간 이벤트 수 =====
SELECT COUNT(*) AS communication_alarms_last_1h
FROM "EventLogs" WHERE "Category" = 2 AND "CreatedAt" > now() - interval '1 hour';

\echo ===== [참고] IP가 없어 수집하지 못하는 운영 장비 압축기 (13대 — Doc/ip-assignment-list.md, setup.md 12.6) =====
SELECT e."Region", e."BuildingName", e."Name", COUNT(*) AS no_ip_compressors
FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"
WHERE e."Status" = 0 AND c."IpAddress" IS NULL
GROUP BY 1, 2, 3 ORDER BY 1, 2, 3;
