-- 납품 직전 초기화 (Doc/setup.md 12장)
--
-- 테스트 서버(회사 고정 IP)에서 쌓인 데이터를 지우고 현장 운영을 시작할 상태로 만든다.
-- 사용자 결정(2026-10-02): **사용자와 담당 장비만 남기고** 나머지는 처음 상태로 되돌린다.
--
--   남기는 것   : 사용자(Users), 담당 장비 지정(UserEquipments), 사용자 선해임 신고서 첨부
--   지우는 것   : 트렌드, 이벤트 로그, 점검·운전·수리일지, 교육훈련 일지, 공지사항,
--                 장비 검사이력, 장비 사진·검사이력·교육훈련·공지 첨부
--   처음 상태로 : 장비·압축기·채널 설정(경보 기준값·운전전류 기준값 포함) — seed_from_csv.sql
--
-- 실행 전 반드시: (1) pg_dump 백업 (2) Stop-Service HRMS
-- 실행:  & $psql -h localhost -U hrms_app -d hrms -f C:\HRMS_setup\reset_for_delivery.sql
--        (seed_from_csv.sql이 이 파일과 같은 폴더에 있어야 한다)
-- 실행 후: App_Data\attachments의 첨부 파일 정리(setup.md 12.2)

\set ON_ERROR_STOP on
\pset pager off

\echo ===== 1/4 담당 장비 백업 =====
-- 임시 테이블이 아니라 실제 테이블로 둔다. 중간에 실패해서 psql이 끝나도 백업이 남아 있어야
-- 손으로 복원할 수 있다. 정상 완료되면 마지막 단계에서 지운다.
DROP TABLE IF EXISTS "_delivery_backup_user_equipments";
CREATE TABLE "_delivery_backup_user_equipments" AS SELECT * FROM "UserEquipments";
SELECT COUNT(*) AS backed_up_user_equipments FROM "_delivery_backup_user_equipments";

\echo ===== 2/4 장비 시드 (장비 설정 초기화 + 장비에 딸린 기록 삭제) =====
-- 시드는 "UPDATE 1"을 수백 줄 출력해서 결과를 가린다. 일반 출력만 숨긴다(오류는 그대로 보인다).
\o NUL
\ir seed_from_csv.sql
\o

\echo ===== 3/4 시드가 건드리지 않는 기록 삭제 + 담당 장비 복원 =====
BEGIN;
DELETE FROM "Attachments" WHERE "OwnerType" IN (3, 4);      -- TrainingLog, Notice (5 = 선해임 신고서는 남김)
TRUNCATE TABLE "TrainingLogs", "Notices" RESTART IDENTITY;
INSERT INTO "UserEquipments" SELECT * FROM "_delivery_backup_user_equipments";
COMMIT;

DROP TABLE "_delivery_backup_user_equipments";

\echo ===== 4/4 결과 확인 =====
SELECT (SELECT COUNT(*) FROM "Users") AS users,
       (SELECT COUNT(*) FROM "UserEquipments") AS user_equipments,
       (SELECT COUNT(*) FROM "Attachments") AS attachments_kept,
       (SELECT COUNT(*) FROM "Equipments") AS equipments,
       (SELECT COUNT(*) FROM "Compressors") AS compressors;

SELECT (SELECT COUNT(*) FROM "EventLogs") AS events,
       (SELECT COUNT(*) FROM "CompressorMeasurements") AS trends,
       (SELECT COUNT(*) FROM "InspectionLogs") + (SELECT COUNT(*) FROM "OperationLogs")
         + (SELECT COUNT(*) FROM "RepairLogs") + (SELECT COUNT(*) FROM "TrainingLogs") AS logs,
       (SELECT COUNT(*) FROM "Notices") AS notices,
       (SELECT COUNT(*) FROM "EquipmentInspectionHistories") AS inspection_histories;

\echo 위 두 번째 결과가 전부 0이고, users / user_equipments가 백업 개수와 같으면 정상.
