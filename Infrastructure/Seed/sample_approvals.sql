-- 일괄 결재(api-manual 72~74번) 프론트 테스트용 샘플 데이터 (2026-09-28)
--
-- **개발/테스트 전용이다.** 운영 DB에는 넣지 않는다 — seed_from_csv.sql(장비·압축기 실데이터)와
-- 달리 이 파일은 화면 확인용 가짜 문서를 만든다.
--
-- 실행:  psql -h localhost -U hrms_app -d hrms -v ON_ERROR_STOP=1 -f Infrastructure/Seed/sample_approvals.sql
-- 여러 번 실행해도 같은 문서가 중복 생성되지 않는다(전부 NOT EXISTS로 막아둔다).
-- 테스트로 결재를 진행한 뒤 처음 상태로 되돌리려면 sample_approvals_reset.sql을 실행한다.
--
-- 결재선: safety19(윤준우, 안전관리원=1단계) -> manager01(홍예준, 책임자=2단계)
--          -> chief01(안윤서, 총괄자=3단계)
-- 데모 장비 6대: 4, 5, 7, 23, 24, 32  (manager01 담당 장비 중 6대)
--
-- 각 문서유형마다 세 묶음을 만든다:
--   (A) 미결재          -> 1단계(safety19)에게 보임
--   (B) 1단계 완료       -> 2단계(manager01)에게 보임
--   (C) 1+2단계 완료     -> 3단계(chief01)에게 보임
-- 수리일지/교육훈련은 1단계가 "해당없음"이라 (B)=미결재, (C)=2단계 완료다.

BEGIN;

-- 0. safety19가 데모 장비 6대를 모두 담당하도록 보강(원래 5번만 담당)
INSERT INTO "UserEquipments" ("UserId", "EquipmentId")
SELECT 31, e FROM unnest(ARRAY[4,5,7,23,24,32]) AS e
WHERE NOT EXISTS (SELECT 1 FROM "UserEquipments" u WHERE u."UserId" = 31 AND u."EquipmentId" = e);

--------------------------------------------------------------------------------
-- 1. 점검일지 — 3주 × 6대 = 18건
--------------------------------------------------------------------------------
INSERT INTO "InspectionLogs"
    ("EquipmentId","WeekStartDate","Opinion",
     "Level1ApproverId","Level1ApproverName","Level1ApprovedAt",
     "Level2ApproverId","Level2ApproverName","Level2ApprovedAt",
     "CreatedAt","UpdatedAt")
SELECT e, w.start, w.opinion,
       w.l1_id, w.l1_name, w.l1_at,
       w.l2_id, w.l2_name, w.l2_at,
       w.created, w.created
FROM unnest(ARRAY[4,5,7,23,24,32]) AS e
CROSS JOIN (VALUES
    -- (A) 미결재 — safety19가 결재할 주
    (DATE '2026-09-20', '특이사항 없음', NULL::int, NULL::text, NULL::timestamptz,
                                        NULL::int, NULL::text, NULL::timestamptz,
                                        TIMESTAMPTZ '2026-09-26 09:00:00+09'),
    -- (B) 1단계 완료 — manager01이 결재할 주
    (DATE '2026-09-13', '응축기 청소 실시', 31, '윤준우', TIMESTAMPTZ '2026-09-19 10:20:00+09',
                                        NULL, NULL, NULL,
                                        TIMESTAMPTZ '2026-09-19 09:00:00+09'),
    -- (C) 1+2단계 완료 — chief01이 결재할 주
    (DATE '2026-09-06', '냉매 누설 점검 완료', 31, '윤준우', TIMESTAMPTZ '2026-09-12 10:10:00+09',
                                        3, '홍예준', TIMESTAMPTZ '2026-09-12 14:30:00+09',
                                        TIMESTAMPTZ '2026-09-12 09:00:00+09')
) AS w(start, opinion, l1_id, l1_name, l1_at, l2_id, l2_name, l2_at, created)
WHERE NOT EXISTS (
    SELECT 1 FROM "InspectionLogs" i WHERE i."EquipmentId" = e AND i."WeekStartDate" = w.start);

--------------------------------------------------------------------------------
-- 2. 운전일지 — 3일 × 6대 = 18건 (오늘치 158건은 이미 있고 전부 미결재다)
--------------------------------------------------------------------------------
INSERT INTO "OperationLogs"
    ("EquipmentId","Date",
     "Level1ApproverId","Level1ApproverName","Level1ApprovedAt",
     "Level2ApproverId","Level2ApproverName","Level2ApprovedAt",
     "CreatedAt","UpdatedAt")
SELECT e, d.day,
       d.l1_id, d.l1_name, d.l1_at,
       d.l2_id, d.l2_name, d.l2_at,
       d.created, d.created
FROM unnest(ARRAY[4,5,7,23,24,32]) AS e
CROSS JOIN (VALUES
    (DATE '2026-09-27', NULL::int, NULL::text, NULL::timestamptz,
                        NULL::int, NULL::text, NULL::timestamptz,
                        TIMESTAMPTZ '2026-09-27 00:05:00+09'),
    (DATE '2026-09-26', 31, '윤준우', TIMESTAMPTZ '2026-09-27 09:15:00+09',
                        NULL, NULL, NULL,
                        TIMESTAMPTZ '2026-09-26 00:05:00+09'),
    (DATE '2026-09-25', 31, '윤준우', TIMESTAMPTZ '2026-09-26 09:15:00+09',
                        3, '홍예준', TIMESTAMPTZ '2026-09-26 16:40:00+09',
                        TIMESTAMPTZ '2026-09-25 00:05:00+09')
) AS d(day, l1_id, l1_name, l1_at, l2_id, l2_name, l2_at, created)
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationLogs" o WHERE o."EquipmentId" = e AND o."Date" = d.day);

--------------------------------------------------------------------------------
-- 3. 수리일지 — 6건. 1단계가 "해당없음"이라 manager01(2단계)부터 결재한다.
--    장비 23번에 같은 날 2건을 넣어 subtitle의 제목 구분이 보이게 한다.
--------------------------------------------------------------------------------
INSERT INTO "RepairLogs"
    ("EquipmentId","Title","PerformedAt","Location","PerformedBy","Target",
     "StateBefore","StateAfter","Result","FailureCause","PreventiveAction","Opinion",
     "CreatedByUserId","CreatedByUserName","CreatedAt",
     "Level2ApproverId","Level2ApproverName","Level2ApprovedAt")
SELECT * FROM (VALUES
    -- (B) 미결재 — manager01 대상
    (4,  '압축기 오일 교체',      TIMESTAMPTZ '2026-09-24 10:00:00+09', '기계실', '한국냉동(주)', '1호기 압축기',
     '오일 오염', '오일 교체 완료', '정상 가동', '사용 시간 초과', '6개월 주기 교체', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-24 17:00:00+09', NULL, NULL, NULL),
    (23, '냉각수 배관 보수',      TIMESTAMPTZ '2026-09-26 09:30:00+09', '옥상', '대성설비', '냉각수 배관',
     '배관 누수', '용접 보수', '누수 해소', '용접부 부식', '방청 도장 주기 단축', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-26 18:00:00+09', NULL, NULL, NULL),
    (23, '응축기 팬모터 교체',    TIMESTAMPTZ '2026-09-26 15:00:00+09', '옥상', '대성설비', '응축기 팬모터',
     '베어링 소음', '모터 교체', '소음 해소', '베어링 마모', '분기 점검 추가', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-26 18:10:00+09', NULL, NULL, NULL),
    -- (C) 2단계 완료 — chief01 대상
    (5,  '고압 차단 스위치 교체',  TIMESTAMPTZ '2026-09-18 13:00:00+09', '기계실', '한국냉동(주)', 'HPC',
     '접점 불량', '스위치 교체', '정상 작동', '접점 소손', '연 1회 교체', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-18 17:30:00+09', 3, '홍예준', TIMESTAMPTZ '2026-09-19 11:00:00+09'),
    (7,  '냉매 충전',              TIMESTAMPTZ '2026-09-19 11:00:00+09', '기계실', '한국냉동(주)', '냉매 배관',
     '냉매 부족', '충전 완료', '정상 냉각', '미세 누설', '누설 검사 강화', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-19 16:00:00+09', 3, '홍예준', TIMESTAMPTZ '2026-09-20 10:00:00+09'),
    (32, '제어반 릴레이 교체',    TIMESTAMPTZ '2026-09-21 14:00:00+09', '제어실', '사내 정비팀', '제어반 릴레이',
     '동작 불안정', '릴레이 교체', '정상 제어', '경년 열화', '5년 주기 교체', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-21 18:00:00+09', 3, '홍예준', TIMESTAMPTZ '2026-09-22 09:30:00+09')
) AS v(eq, title, performed, loc, by, target, before, after, res, cause, prevent, opinion,
       cb_id, cb_name, created, l2_id, l2_name, l2_at)
WHERE NOT EXISTS (
    SELECT 1 FROM "RepairLogs" r WHERE r."EquipmentId" = v.eq AND r."Title" = v.title);

--------------------------------------------------------------------------------
-- 4. 교육훈련 — 4건. 장비와 무관한 전사 문서라 담당 장비를 보지 않는다
--    (= 모든 책임자/총괄자에게 보인다).
--------------------------------------------------------------------------------
INSERT INTO "TrainingLogs"
    ("Title","PerformedAt","Location","Instructor","Content","Attendees",
     "CreatedByUserId","CreatedByUserName","CreatedAt",
     "Level2ApproverId","Level2ApproverName","Level2ApprovedAt")
SELECT * FROM (VALUES
    -- (B) 미결재 — manager01 대상
    ('냉동기 취급 안전교육',   TIMESTAMPTZ '2026-09-24 14:00:00+09', '연구소 대회의실', '한국가스안전공사 김강사',
     '고압가스 취급 시 준수사항, 누설 시 대응 절차', '윤준우, 홍예준 외 12명',
     31, '윤준우', TIMESTAMPTZ '2026-09-24 17:00:00+09', NULL, NULL, NULL),
    ('냉매 누설 대응 훈련',    TIMESTAMPTZ '2026-09-26 10:00:00+09', '환경시험1동', '사내 안전팀',
     '누설 감지 후 비상정지 및 대피 절차 실습', '안전관리원 전원',
     31, '윤준우', TIMESTAMPTZ '2026-09-26 16:00:00+09', NULL, NULL, NULL),
    -- (C) 2단계 완료 — chief01 대상
    ('３분기 정기 안전교육',   TIMESTAMPTZ '2026-09-11 14:00:00+09', '연구소 대회의실', '사내 안전팀',
     '분기 사고 사례 공유 및 재발 방지 대책', '전 관리자',
     31, '윤준우', TIMESTAMPTZ '2026-09-11 17:00:00+09', 3, '홍예준', TIMESTAMPTZ '2026-09-12 10:00:00+09'),
    ('소방시설 사용법 교육',   TIMESTAMPTZ '2026-09-04 15:00:00+09', '연구소 야외교육장', '남양소방서',
     '소화기·옥내소화전 사용 실습', '전 관리자',
     31, '윤준우', TIMESTAMPTZ '2026-09-04 17:30:00+09', 3, '홍예준', TIMESTAMPTZ '2026-09-05 09:00:00+09')
) AS v(title, performed, loc, instructor, content, attendees,
       cb_id, cb_name, created, l2_id, l2_name, l2_at)
WHERE NOT EXISTS (
    SELECT 1 FROM "TrainingLogs" t WHERE t."Title" = v.title);

COMMIT;

-- 확인
SELECT '점검일지' AS 문서, count(*) FROM "InspectionLogs"
UNION ALL SELECT '운전일지', count(*) FROM "OperationLogs"
UNION ALL SELECT '수리일지', count(*) FROM "RepairLogs"
UNION ALL SELECT '교육훈련', count(*) FROM "TrainingLogs";

--------------------------------------------------------------------------------
-- 5. test1 계정(안전관리책임자 = 2단계) 전용 결재 대상
--    담당 장비 11~19번(PT 배기환경시험동 / PT 환경선행연구동 9대)을 쓴다 —
--    위 safety19 시나리오(장비 4·5·7·23·24·32)와 겹치지 않아 서로 영향이 없다.
--    2단계가 보려면 1단계가 끝나 있어야 하므로 점검·운전일지는 Level1을 채워둔다.
--    수리일지는 1단계가 "해당없음"이라 미결재 상태 그대로 2단계 대상이 된다.
--------------------------------------------------------------------------------
BEGIN;

-- 점검일지: 9대 × 2주 = 18건 (1단계 완료)
INSERT INTO "InspectionLogs"
    ("EquipmentId","WeekStartDate","Opinion",
     "Level1ApproverId","Level1ApproverName","Level1ApprovedAt",
     "CreatedAt","UpdatedAt")
SELECT e, w.start, w.opinion, 31, '윤준우', w.l1_at, w.created, w.created
FROM unnest(ARRAY[11,12,13,14,15,16,17,18,19]) AS e
CROSS JOIN (VALUES
    (DATE '2026-09-20', '특이사항 없음',
     TIMESTAMPTZ '2026-09-26 10:30:00+09', TIMESTAMPTZ '2026-09-26 09:00:00+09'),
    (DATE '2026-09-13', '압축기 진동 확인, 이상 없음',
     TIMESTAMPTZ '2026-09-19 10:30:00+09', TIMESTAMPTZ '2026-09-19 09:00:00+09')
) AS w(start, opinion, l1_at, created)
WHERE NOT EXISTS (
    SELECT 1 FROM "InspectionLogs" i WHERE i."EquipmentId" = e AND i."WeekStartDate" = w.start);

-- 운전일지: 9대 × 2일 = 18건 (1단계 완료)
INSERT INTO "OperationLogs"
    ("EquipmentId","Date",
     "Level1ApproverId","Level1ApproverName","Level1ApprovedAt",
     "CreatedAt","UpdatedAt")
SELECT e, d.day, 31, '윤준우', d.l1_at, d.created, d.created
FROM unnest(ARRAY[11,12,13,14,15,16,17,18,19]) AS e
CROSS JOIN (VALUES
    (DATE '2026-09-27', TIMESTAMPTZ '2026-09-28 09:10:00+09', TIMESTAMPTZ '2026-09-27 00:05:00+09'),
    (DATE '2026-09-26', TIMESTAMPTZ '2026-09-27 09:10:00+09', TIMESTAMPTZ '2026-09-26 00:05:00+09')
) AS d(day, l1_at, created)
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationLogs" o WHERE o."EquipmentId" = e AND o."Date" = d.day);

-- 수리일지: 4건 (미결재 = 2단계 대상). 장비 14번에 같은 날 2건을 넣어 제목 구분을 본다.
INSERT INTO "RepairLogs"
    ("EquipmentId","Title","PerformedAt","Location","PerformedBy","Target",
     "StateBefore","StateAfter","Result","FailureCause","PreventiveAction","Opinion",
     "CreatedByUserId","CreatedByUserName","CreatedAt")
SELECT * FROM (VALUES
    (11, '팽창밸브 교체',        TIMESTAMPTZ '2026-09-22 09:00:00+09', '기계실', '한국냉동(주)', '팽창밸브',
     '과열도 불안정', '밸브 교체', '정상 제어', '밸브 고착', '연 1회 점검', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-22 17:00:00+09'),
    (14, '냉각탑 충전재 교체',   TIMESTAMPTZ '2026-09-25 09:00:00+09', '옥상', '대성설비', '냉각탑 충전재',
     '스케일 고착', '충전재 교체', '냉각 성능 회복', '수질 관리 미흡', '수처리 주기 단축', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-25 18:00:00+09'),
    (14, '냉각탑 벨트 장력 조정', TIMESTAMPTZ '2026-09-25 14:00:00+09', '옥상', '사내 정비팀', '냉각탑 송풍기 벨트',
     '벨트 슬립', '장력 조정', '소음 감소', '벨트 이완', '월 1회 점검', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-25 18:20:00+09'),
    (18, '유분리기 오일 보충',   TIMESTAMPTZ '2026-09-27 10:00:00+09', '기계실', '한국냉동(주)', '유분리기',
     '오일 레벨 저하', '오일 보충', '정상 가동', '미세 누유', '누유부 점검 강화', NULL,
     31, '윤준우', TIMESTAMPTZ '2026-09-27 16:00:00+09')
) AS v(eq, title, performed, loc, by, target, before, after, res, cause, prevent, opinion,
       cb_id, cb_name, created)
WHERE NOT EXISTS (
    SELECT 1 FROM "RepairLogs" r WHERE r."EquipmentId" = v.eq AND r."Title" = v.title);

COMMIT;
