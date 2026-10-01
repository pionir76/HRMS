-- 일괄 결재 샘플 데이터를 "결재 전" 상태로 되돌린다 (2026-09-28)
--
-- **개발/테스트 전용.** 화면에서 일괄 결재를 눌러 목록이 비면 이 파일을 실행해서 다시 채운다.
-- 문서를 지웠다 다시 만들지 않고 결재 칸(Level1~3)만 원래 단계로 되돌리므로 문서 ID는 그대로다.
-- sample_approvals.sql을 먼저 한 번 실행해 둔 상태를 전제로 한다.
--
-- 실행:  psql -h localhost -U hrms_app -d hrms -v ON_ERROR_STOP=1 -f Infrastructure/Seed/sample_approvals_reset.sql
--
-- 되돌린 뒤 상태:
--   safety19  (1단계) 점검일지 6, 운전일지 6 + 담당 장비의 오늘치
--   manager01 (2단계) 점검일지 6, 운전일지 6, 수리일지 3, 교육훈련 2
--   chief01   (3단계) 점검일지 6, 운전일지 6, 수리일지 3, 교육훈련 2
--   test1     (2단계) 점검일지 21, 운전일지 21, 수리일지 5, 교육훈련 2

BEGIN;

--------------------------------------------------------------------------------
-- 1. 점검일지
--------------------------------------------------------------------------------
-- (가) safety19 시나리오: 장비 4·5·7·23·24·32, 주차별로 0/1/2단계
UPDATE "InspectionLogs" SET
    "Level1ApproverId"   = CASE "WeekStartDate" WHEN DATE '2026-09-20' THEN NULL ELSE 31 END,
    "Level1ApproverName" = CASE "WeekStartDate" WHEN DATE '2026-09-20' THEN NULL ELSE '윤준우' END,
    "Level1ApprovedAt"   = CASE "WeekStartDate"
                               WHEN DATE '2026-09-20' THEN NULL
                               WHEN DATE '2026-09-13' THEN TIMESTAMPTZ '2026-09-19 10:20:00+09'
                               ELSE TIMESTAMPTZ '2026-09-12 10:10:00+09' END,
    "Level2ApproverId"   = CASE "WeekStartDate" WHEN DATE '2026-09-06' THEN 3 ELSE NULL END,
    "Level2ApproverName" = CASE "WeekStartDate" WHEN DATE '2026-09-06' THEN '홍예준' ELSE NULL END,
    "Level2ApprovedAt"   = CASE "WeekStartDate" WHEN DATE '2026-09-06' THEN TIMESTAMPTZ '2026-09-12 14:30:00+09' ELSE NULL END,
    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
WHERE "EquipmentId" = ANY (ARRAY[4,5,7,23,24,32])
  AND "WeekStartDate" IN (DATE '2026-09-20', DATE '2026-09-13', DATE '2026-09-06');

-- (나) test1 시나리오: 장비 11~19, 두 주 모두 1단계까지만 완료
UPDATE "InspectionLogs" SET
    "Level1ApproverId" = 31, "Level1ApproverName" = '윤준우',
    "Level1ApprovedAt" = CASE "WeekStartDate"
                             WHEN DATE '2026-09-20' THEN TIMESTAMPTZ '2026-09-26 10:30:00+09'
                             ELSE TIMESTAMPTZ '2026-09-19 10:30:00+09' END,
    "Level2ApproverId" = NULL, "Level2ApproverName" = NULL, "Level2ApprovedAt" = NULL,
    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
WHERE "EquipmentId" BETWEEN 11 AND 19
  AND "WeekStartDate" IN (DATE '2026-09-20', DATE '2026-09-13');

--------------------------------------------------------------------------------
-- 2. 운전일지
--------------------------------------------------------------------------------
-- (가) 먼저 전부 미결재로 — 오늘 자동 생성분과 테스트로 결재한 것들을 한 번에 지운다
UPDATE "OperationLogs" SET
    "Level1ApproverId" = NULL, "Level1ApproverName" = NULL, "Level1ApprovedAt" = NULL,
    "Level2ApproverId" = NULL, "Level2ApproverName" = NULL, "Level2ApprovedAt" = NULL,
    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
WHERE "Level1ApprovedAt" IS NOT NULL OR "Level2ApprovedAt" IS NOT NULL OR "Level3ApprovedAt" IS NOT NULL;

-- (나) safety19 시나리오: 장비 4·5·7·23·24·32, 날짜별로 0/1/2단계
UPDATE "OperationLogs" SET
    "Level1ApproverId"   = CASE "Date" WHEN DATE '2026-09-27' THEN NULL ELSE 31 END,
    "Level1ApproverName" = CASE "Date" WHEN DATE '2026-09-27' THEN NULL ELSE '윤준우' END,
    "Level1ApprovedAt"   = CASE "Date"
                               WHEN DATE '2026-09-27' THEN NULL
                               WHEN DATE '2026-09-26' THEN TIMESTAMPTZ '2026-09-27 09:15:00+09'
                               ELSE TIMESTAMPTZ '2026-09-26 09:15:00+09' END,
    "Level2ApproverId"   = CASE "Date" WHEN DATE '2026-09-25' THEN 3 ELSE NULL END,
    "Level2ApproverName" = CASE "Date" WHEN DATE '2026-09-25' THEN '홍예준' ELSE NULL END,
    "Level2ApprovedAt"   = CASE "Date" WHEN DATE '2026-09-25' THEN TIMESTAMPTZ '2026-09-26 16:40:00+09' ELSE NULL END
WHERE "EquipmentId" = ANY (ARRAY[4,5,7,23,24,32])
  AND "Date" IN (DATE '2026-09-27', DATE '2026-09-26', DATE '2026-09-25');

-- (다) test1 시나리오: 장비 11~19, 두 날짜 모두 1단계까지만 완료
UPDATE "OperationLogs" SET
    "Level1ApproverId" = 31, "Level1ApproverName" = '윤준우',
    "Level1ApprovedAt" = CASE "Date"
                             WHEN DATE '2026-09-27' THEN TIMESTAMPTZ '2026-09-28 09:10:00+09'
                             ELSE TIMESTAMPTZ '2026-09-27 09:10:00+09' END
WHERE "EquipmentId" BETWEEN 11 AND 19
  AND "Date" IN (DATE '2026-09-27', DATE '2026-09-26');

--------------------------------------------------------------------------------
-- 3. 수리일지 — 제목 기준. 1단계는 "해당없음"이라 항상 비어 있다.
--------------------------------------------------------------------------------
UPDATE "RepairLogs" SET
    "Level2ApproverId"   = CASE WHEN "Title" IN ('고압 차단 스위치 교체','냉매 충전','제어반 릴레이 교체') THEN 3 ELSE NULL END,
    "Level2ApproverName" = CASE WHEN "Title" IN ('고압 차단 스위치 교체','냉매 충전','제어반 릴레이 교체') THEN '홍예준' ELSE NULL END,
    "Level2ApprovedAt"   = CASE "Title"
                               WHEN '고압 차단 스위치 교체' THEN TIMESTAMPTZ '2026-09-19 11:00:00+09'
                               WHEN '냉매 충전'            THEN TIMESTAMPTZ '2026-09-20 10:00:00+09'
                               WHEN '제어반 릴레이 교체'    THEN TIMESTAMPTZ '2026-09-22 09:30:00+09'
                               ELSE NULL END,
    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
WHERE "Title" IN ('압축기 오일 교체','냉각수 배관 보수','응축기 팬모터 교체',
                  '고압 차단 스위치 교체','냉매 충전','제어반 릴레이 교체',
                  '팽창밸브 교체','냉각탑 충전재 교체','냉각탑 벨트 장력 조정','유분리기 오일 보충');

--------------------------------------------------------------------------------
-- 4. 교육훈련 — 제목 기준
--------------------------------------------------------------------------------
UPDATE "TrainingLogs" SET
    "Level2ApproverId"   = CASE WHEN "Title" IN ('３분기 정기 안전교육','소방시설 사용법 교육') THEN 3 ELSE NULL END,
    "Level2ApproverName" = CASE WHEN "Title" IN ('３분기 정기 안전교육','소방시설 사용법 교육') THEN '홍예준' ELSE NULL END,
    "Level2ApprovedAt"   = CASE "Title"
                               WHEN '３분기 정기 안전교육'  THEN TIMESTAMPTZ '2026-09-12 10:00:00+09'
                               WHEN '소방시설 사용법 교육'  THEN TIMESTAMPTZ '2026-09-05 09:00:00+09'
                               ELSE NULL END,
    "Level3ApproverId" = NULL, "Level3ApproverName" = NULL, "Level3ApprovedAt" = NULL
WHERE "Title" IN ('냉동기 취급 안전교육','냉매 누설 대응 훈련','３분기 정기 안전교육','소방시설 사용법 교육');

--------------------------------------------------------------------------------
-- 5. 테스트로 쌓인 결재 이벤트 로그 정리(일괄 결재분만)
--------------------------------------------------------------------------------
DELETE FROM "EventLogs" WHERE "Category" = 5 AND "Message" LIKE '%일괄 결재%';

COMMIT;
