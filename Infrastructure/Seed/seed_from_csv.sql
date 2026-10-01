-- 장비/압축기 전면 교체 (Infrastructure/Equipments.csv, Compressors.csv 기준, 2026-09-18)
-- 생성: Infrastructure/Seed/generate_seed_from_csv.js — SQL을 직접 편집하지 말고 스크립트로 다시 생성할 것
\set ON_ERROR_STOP on
BEGIN;

-- 1) 장비/압축기에 딸린 기존 데이터 전부 삭제 (사용자 결정: 현재 기준으로 리셋)
DELETE FROM "Attachments" WHERE "OwnerType" IN (1, 2); -- EquipmentPhoto, EquipmentInspectionHistory
TRUNCATE TABLE "CompressorMeasurements", "CompressorSensorCurrents", "CompressorChannelSettings",
    "OperationItemValues", "OperationReferenceValues", "OperationLogs",
    "InspectionResults", "InspectionLogs", "RepairLogs", "EquipmentInspectionHistories",
    "EventLogs", "UserEquipments", "Compressors", "Equipments" RESTART IDENTITY CASCADE;

-- 2) 장비 157대
INSERT INTO "Equipments" ("Id", "Region", "BuildingNumber", "BuildingName", "Location", "Name", "Status", "ManagementNumber", "CompressorCount", "HasCoolingWater", "CoolingWaterInletMin", "CoolingWaterInletMax", "CoolingWaterOutletMin", "CoolingWaterOutletMax", "HasBrine", "BrineInletMin", "BrineInletMax", "BrineOutletMin", "BrineOutletMax", "HasVoltage", "VoltageMin", "VoltageMax", "RunningCurrentThreshold", "AlarmStatus", "CommunicationStatus", "IsRunning") VALUES
  (1, 'A 지구', 'A-4', '차량장비동', '1F', 'BSR #1', 0, '00072686-089', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (2, 'A 지구', 'A-4', '차량장비동', '1F', 'BSR #3', 0, '00072686-221', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (3, 'A 지구', 'A-6', '차량시험동', 'B1F', '저온챔버 #1', 0, '00072686-081', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (4, 'A 지구', 'A-6', '차량시험동', 'B1F', '저온챔버 #2', 0, '00072686-081', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (5, 'A 지구', 'A-6', '차량시험동', '2F (옥외)', '수밀챔버 #1', 0, '00072686-036', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (6, 'A 지구', 'A-6', '차량시험동', '2F (옥외)', '수밀챔버 #2', 0, '00072686-036', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (7, 'A 지구', 'A-6', '차량시험동', '2F', '시트컴포트(저온)&HOT챔버', 0, '00072686-122', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (8, 'A 지구', 'A-6', '차량시험동', '1F', 'BSR #2', 0, '00072686-121', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (9, 'A 지구', 'A1-1', 'PDI 1동', '1F', '실차환경챔버 #1', 1, NULL, 2, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (10, 'A 지구', 'A1-1', 'PDI 1동', '1F', '실차환경챔버 #2', 0, '00072686-156', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (11, 'A 지구', 'A1-5', 'PT 배기환경시험동', '2F', '인증환경챔버 #1', 0, '00072686-124', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (12, 'A 지구', 'A1-5', 'PT 배기환경시험동', '2F', '인증환경챔버 #2', 0, '00072686-124', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (13, 'A 지구', 'A1-5', 'PT 배기환경시험동', '2F', '인증환경챔버 #3', 0, '00072686-124', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (14, 'A 지구', 'A1-5', 'PT 배기환경시험동', '2F', '인증환경챔버 #4', 0, '00072686-124', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (15, 'A 지구', 'A1-5', 'PT 배기환경시험동', '2F', '인증환경챔버 #5', 0, '00072686-124', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (16, 'A 지구', 'A1-17', 'PT 환경선행연구동', 'B1F', '냉각칠러 #1', 0, '00072686-171', 2, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (17, 'A 지구', 'A1-17', 'PT 환경선행연구동', 'B1F', '냉각칠러 #2', 0, '00072686-172', 2, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (18, 'A 지구', 'A1-17', 'PT 환경선행연구동', '2F', '고지챔버 #1', 0, '00072686-115', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (19, 'A 지구', 'A1-17', 'PT 환경선행연구동', '2F', '고지챔버 #2', 0, '00072686-116', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (20, 'B 지구', 'B1-1', '제동시험동', '옥상', '고저온 소음 #3', 0, '00072686-179', 2, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (21, 'B 지구', 'B1-1', '제동시험동', '옥상', '고저온 소음 #4', 0, '00072686-180', 2, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (22, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #1~2 냉동기  #1', 0, '00072686-176', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (23, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #1~2 냉동기  #2', 0, '00072686-176', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (24, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #1~2 냉동기  #3', 0, '00072686-176', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (25, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #5~6 냉동기  #1', 0, '00072686-120', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (26, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #5~6 냉동기  #2', 0, '00072686-120', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (27, 'B 지구', 'B1-1', '제동시험동', '2F', '고저온 #5~6 냉동기  #3', 0, '00072686-120', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (28, 'B 지구', 'B1-2', '환경차개발시험1동', '옥상', '배기시험 #1', 0, '00072686-098', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (29, 'B 지구', 'B1-2', '환경차개발시험1동', '옥상', '배기시험 #2', 0, '00072686-101', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (30, 'B 지구', 'B1-2', '환경차개발시험1동', '옥상', '사무공조 #1', 0, '00072686-099', 2, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (31, 'B 지구', 'B1-2', '환경차개발시험1동', '옥상', '사무공조 #2', 0, '00072686-100', 2, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (32, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리개발실 #1', 0, '00072686-159', 3, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (33, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리개발실 #2', 0, '00072686-160', 3, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (34, 'B 지구', 'B1-6', '환경차개발시험2동', '1F', '고저온챔버, 쇼크챔버', 0, '00072686-096', 2, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (35, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #1', 0, '00072686-191', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (36, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #2', 0, '00072686-192', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (37, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #3', 0, '00072686-193', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (38, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #4', 0, '00072686-194', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (39, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #5', 0, '00072686-195', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (40, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #6', 0, '00072686-196', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (41, 'B 지구', 'B1-6', '환경차개발시험2동', '옥상', '배터리 #7', 0, '00072686-197', 6, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (42, 'B 지구', 'B1-7', '환경시험1동', '2F', '저온풍동 #1', 0, '00072686-145', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (43, 'B 지구', 'B1-7', '환경시험1동', '2F', '저온풍동 #2', 0, '00072686-145', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (44, 'B 지구', 'B1-7', '환경시험1동', '2F', '저온풍동 #3', 0, '00072686-145', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (45, 'B 지구', 'B1-7', '환경시험1동', '2F', '저온풍동 #4', 0, '00072686-145', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (46, 'B 지구', 'B1-7', '환경시험1동', '2F', 'MAU/공조기', 0, '00072686-146', 3, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (47, 'B 지구', 'B1-7', '환경시험1동', '2F', '소크룸 #1', 0, '00072686-144', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (48, 'B 지구', 'B1-7', '강설시험동', '1F', '강설강우풍동 #1', 0, '00072686-051', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (49, 'B 지구', 'B1-7', '강설시험동', '1F', '강설강우풍동 #2', 0, '00072686-051', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (50, 'B 지구', 'B1-7', '강설시험동', '2F', 'MAU/공조기', 0, '00072686-054', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (51, 'B 지구', 'B1-7', '강설시험동', '2F', '소크룸#2', 0, '00072686-055', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (52, 'B 지구', 'B1-8', '무향1동', '옥상', '디젤1실', 0, '00072686-022', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (53, 'B 지구', 'B1-10', '배기1동', '2F 기계실', '소크룸 #1', 0, '00072686-260', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (54, 'B 지구', 'B1-10', '배기1동', '2F 기계실', '소크룸 #2', 0, '00072686-260', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (55, 'B 지구', 'B1-10', '배기1동', '2F 기계실', '환경챔버 #1', 0, '00072686-259', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (56, 'B 지구', 'B1-10', '배기1동', '2F 기계실', '환경챔버 #2', 0, '00072686-259', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (57, 'B 지구', 'B1-12', '배기2동', '2F', '환경챔버 #1', 0, '00072686-039', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (58, 'B 지구', 'B1-12', '배기2동', '2F', '환경챔버 #2', 0, '00072686-040', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (59, 'B 지구', 'B1-12', '배기2동', '2F', '소크챔버', 0, '00072686-041', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (60, 'B 지구', 'B1-14', '전기차환경시험동', '3F 기계실', '환경챔버 #1', 0, '00072686-266', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (61, 'B 지구', 'B1-14', '전기차환경시험동', '3F 기계실', '환경챔버 #2', 0, '00072686-266', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (62, 'B 지구', 'B1-14', '전기차환경시험동', '3F 기계실', '환경챔버 #3', 0, '00072686-266', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (63, 'C 지구', 'C1-2', 'PDI 2동', '1F', '도장 AHU', 0, '00072686-074', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (64, 'C 지구', 'C1-2', 'PDI 2동', '2F', 'CAB냉동기', 0, '00072686-073', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (65, 'C 지구', 'C1-2', 'PDI 2동', '1F', '전착냉동기 #1', 0, '00072686-261', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (66, 'C 지구', 'C1-2', 'PDI 2동', '1F', '전착냉동기 #2', 0, '00072686-187', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (67, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #1', 0, '00072686-066', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (68, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #2', 0, '00072686-067', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (69, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #3', 0, '00072686-068', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (70, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #4', 0, '00072686-069', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (71, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #5', 0, '00072686-070', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (72, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #6', 0, '00072686-071', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (73, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #7', 0, '00072686-072', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (74, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #8', 0, '00072686-153', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (75, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #9', 0, '00072686-154', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (76, 'C 지구', 'C1-4', '배기3동', '옥상', '시험실 #10', 0, '00072686-155', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (77, 'C 지구', 'C1-6', '자동내구2동', '2F', '환경챔버 MACD #1', 0, '00072686-151', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (78, 'C 지구', 'C1-6', '자동내구2동', '2F', '환경챔버 MACD #2', 0, '00072686-151', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (79, 'C 지구', 'C1-6', '자동내구2동', '2F', '환경챔버 MACD #3', 0, '00072686-151', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (80, 'C 지구', 'C1-6', '자동내구2동', '2F', '환경챔버 MACD #4', 0, '00072686-151', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (81, 'C 지구', 'C1-6', '자동내구2동', '2F', '환경챔버 MACD #5', 0, '00072686-152', 4, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (82, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #1', 0, '00072686-162', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (83, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #2', 0, '00072686-163', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (84, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #3', 0, '00072686-164', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (85, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #4', 0, '00072686-165', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (86, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #5', 0, '00072686-166', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (87, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #6', 0, '00072686-167', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (88, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #7', 0, '00072686-168', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (89, 'C 지구', 'C1-8', '제어연비동', '옥상', '셀공조기 #8', 0, '00072686-169', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (90, 'C 지구', 'C1-8', '제어연비동', '2F', '고저온챔버, 소크룸', 0, '00072686-092', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (91, 'C 지구', 'C2-1', '환경차개발시험3동', '2F', '환경챔버 #1, 소크룸 #1', 0, '00072686-173', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (92, 'C 지구', 'C2-1', '환경차개발시험3동', '2F', '환경챔버 #1, 소크룸 #2', 0, '00072686-173', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (93, 'C 지구', 'C2-1', '환경차개발시험3동', '2F', '환경챔버 #2, 소크룸 #1', 0, '00072686-182', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (94, 'C 지구', 'C2-1', '환경차개발시험3동', '2F', '환경챔버 #2, 소크룸 #2', 0, '00072686-182', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (95, 'C 지구', 'C2-7', '무향3동', '2F 가변외기온시험실', '무향실', 0, '00072686-113', 5, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (96, 'C 지구', 'C2-9', 'NVH시험3동', '3F', '고저온챔버', 0, '00072686-112', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (97, 'C 지구', 'C2-11', '부식내구1동', '1F', '항온항습챔버#3 저온챔버 #1', 0, '00072686-110', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (98, 'C 지구', 'C2-11', '부식내구1동', '1F', '항온항습챔버#3 저온챔버 #2', 0, '00072686-110', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (99, 'C 지구', 'C2-11', '부식내구1동', '1F', '항온항습챔버#3 저온챔버 #3', 0, '00072686-110', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (100, 'C 지구', 'C2-11', '부식내구1동', '1F (외부)', '태양광챔버 #1', 0, '00072686-111', 2, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (101, 'C 지구', 'C2-11', '부식내구1동', '1F (외부)', '태양광챔버 #2', 0, '00072686-111', 2, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (102, 'C 지구', 'C3-1', '차량내구동', '2F', '저온챔버 #1', 0, '00072686-091', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (103, 'C 지구', 'C3-1', '차량내구동', '2F', '저온챔버 #2', 0, '00072686-091', 1, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (104, 'C 지구', 'C3-4', '재료개발동', '1F', '실차복합IR챔버', 0, '00072686-109', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (105, 'C 지구', 'C3-6', '전자연구2동', '1F', '실차환경챔버#3', 0, '00072686-097', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (106, 'C 지구', 'C3-7', '시스템내구동', '3F', '실차환경챔버 #1', 0, '00072686-147', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (107, 'C 지구', 'C3-7', '시스템내구동', '3F', '실차환경챔버 #2', 0, '00072686-227', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (108, 'C 지구', 'C3-7', '시스템내구동', '3F', '강설챔버', 0, '00072686-272', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (109, 'C 지구', 'C3-13', '상용엔진시험동', '1F (외부)', '환경챔버 #1', 0, '00072686-174', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (110, 'C 지구', 'C3-13', '상용엔진시험동', '1F (외부)', '환경챔버 #2', 0, '00072686-174', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (111, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #1', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (112, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #2', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (113, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #3', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (114, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #4', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (115, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #5', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (116, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #6', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (117, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #7', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (118, 'C 지구', NULL, '수소충전소', '1F', '냉동기 #8', 1, NULL, 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (119, 'C 지구', 'C4-1', '인증시험동', '2F', '복합챔버 #1', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (120, 'C 지구', 'C4-1', '인증시험동', '2F', '복합챔버 #2', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (121, 'C 지구', 'C4-1', '인증시험동', '2F', '저온챔버 #1 냉동기 #1', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (122, 'C 지구', 'C4-1', '인증시험동', '2F', '저온챔버 #1 냉동기 #2', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (123, 'C 지구', 'C4-1', '인증시험동', '2F', '소크룸 #1', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (124, 'C 지구', 'C4-1', '인증시험동', '2F', '소크룸 #2', 0, '00072686-149', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (125, 'C 지구', 'C4-1', '인증시험동', '2F', '저온챔버 #2 냉동기 #1', 0, '00072686-214', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (126, 'C 지구', 'C4-1', '인증시험동', '2F', '저온챔버 #2 냉동기 #2', 0, '00072686-214', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (127, 'C 지구', 'C4-3', '인증시험3동', '2F', '저온챔버 #1, 소크룸 #1', 0, '00072686-273', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (128, 'C 지구', 'C4-3', '인증시험3동', '2F', '저온챔버 #2, 소크룸 #2', 0, '00072686-273', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (129, 'C 지구', 'C4-4', '환경시험2동', '1F', '저온풍동 #1 #2', 0, '00072686-118', 2, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (130, 'C 지구', 'C4-4', '환경시험2동', '1F', '저온풍동 #3 #4', 0, '00072686-118', 2, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (131, 'C 지구', 'C4-4', '환경시험2동', '1F', '고온풍동 #1', 0, '00072686-119', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (132, 'C 지구', 'C4-4', '환경시험2동', '3F', '고온/저온 소크룸 #1', 0, '00072686-117', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (133, 'C 지구', 'C4-4', '환경시험2동', '3F', '고온/저온 소크룸 #2', 0, '00072686-117', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (134, 'C 지구', 'C4-4', '환경시험2동', '3F', '무향챔버 #1', 0, '00072686-126', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (135, 'C 지구', 'C4-4', '환경시험2동', '3F', '무향챔버 #2', 0, '00072686-126', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (136, 'C 지구', 'C4-4', '환경시험2동', '2F', '제습용 냉동기', 0, '00072686-274', 2, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (137, 'C 지구', 'C4-6', '고지환경시험동', '3F', '실차고지챔버 #1', 0, '00072686-130', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (138, 'C 지구', 'C4-6', '고지환경시험동', '3F', '실차고지챔버 #2', 0, '00072686-130', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (139, 'C 지구', 'C4-8', '인증시험2동', '2F', '복합챔버 #1', 0, '00072686-232', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (140, 'C 지구', 'C4-8', '인증시험2동', '2F', '복합챔버 #2', 0, '00072686-232', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (141, 'C 지구', 'C4-8', '인증시험2동', '2F', '복합챔버 #3', 0, '00072686-232', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (142, 'C 지구', 'C4-8', '인증시험2동', '2F', '소크룸', 0, '00072686-232', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (143, 'C 지구', 'C5-2', '상용환경시험동', '3F', '환경풍동 냉동기 #1', 0, '00072686-206', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (144, 'C 지구', 'C5-2', '상용환경시험동', '3F', '환경풍동 냉동기 #2', 0, '00072686-206', 1, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (145, 'C 지구', 'C5-2', '상용환경시험동', '3F', '환경풍동 제습기', 0, '00072686-207', 3, true, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (146, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#1 냉동기 #1', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (147, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#1 냉동기 #2', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (148, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#2 냉동기 #1', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (149, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#2 냉동기 #2', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (150, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#3 냉동기 #1', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (151, 'C 지구', 'C5-3', '종합내구시험동', '4F', '환경챔버#3 냉동기 #2', 0, '00072686-129', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (152, 'C 지구', 'C5-10', '상용설계동', '2F', '제동소음시험실 #1', 0, '00072686-142', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (153, 'C 지구', 'C5-10', '상용설계동', '2F', '제동소음시험실 #2', 0, '00072686-142', 1, false, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (154, 'C 지구', 'C5-10', '상용설계동', '1F', '기후재현시험실', 0, '00072686-143', 2, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (155, 'C 지구', 'C5-10', '상용설계동', 'B1F', '환경진동시험실', 0, '00072686-148', 3, true, NULL, NULL, NULL, NULL, false, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (156, 'C 지구', 'C5-14', '상용장비동', '1F', '샤시 동력계 환경챔버', 0, '00072686-090', 2, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (157, 'C 지구', 'C5-14', '상용장비동', '1F', '상용 실차 부식챔버', 0, '00072686-175', 2, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false),
  (158, 'C 지구', 'C4-4', '환경시험2동', '1F', '고온풍동 #2', 0, '00072686-119', 1, false, NULL, NULL, NULL, NULL, true, NULL, NULL, NULL, NULL, false, NULL, NULL, 100, 0, 1, false);

-- 3) 압축기 245대 (SequenceNo는 장비별로 id 순서대로 1부터 부여)
INSERT INTO "Compressors" ("Id", "EquipmentId", "IpAddress", "MacAddress", "CommunicationStatus", "AlarmStatus", "SequenceNo", "DisconnectedSince", "HasCommunicationAlarm") VALUES
  (1, 1, '10.90.21.233', '00:06:AC:E0:0D:46', 1, 0, 1, NULL, false),
  (2, 1, '10.90.21.234', '98:06:37:70:00:2C', 1, 0, 2, NULL, false),
  (3, 2, '10.90.21.231', '98:06:37:70:1E:57', 1, 0, 1, NULL, false),
  (4, 2, '10.90.21.232', '98:06:37:70:1E:58', 1, 0, 2, NULL, false),
  (5, 3, '10.90.64.211', '98:06:37:70:00:26', 1, 0, 1, NULL, false),
  (6, 4, '10.90.64.212', '98:06:37:70:00:3F', 1, 0, 1, NULL, false),
  (7, 5, '10.90.64.213', '98:06:37:70:00:32', 1, 0, 1, NULL, false),
  (8, 6, '10.90.64.214', '00:06:AC:E0:0D:7E', 1, 0, 1, NULL, false),
  (9, 7, '10.90.64.215', '98:06:37:70:00:93', 1, 0, 1, NULL, false),
  (10, 7, '10.90.64.216', '98:06:37:70:00:8C', 1, 0, 2, NULL, false),
  (11, 8, '10.90.64.217', '98:06:37:70:00:82', 1, 0, 1, NULL, false),
  (12, 8, '10.90.64.218', '98:06:37:70:00:91', 1, 0, 2, NULL, false),
  (13, 9, NULL, NULL, 1, 0, 1, NULL, false),
  (14, 10, '10.93.78.207', '00:06:AC:E0:0D:7C', 1, 0, 1, NULL, false),
  (15, 10, '10.93.78.208', '00:06:AC:E0:0D:87', 1, 0, 2, NULL, false),
  (16, 11, '10.90.190.235', '98:06:37:70:00:70', 1, 0, 1, NULL, false),
  (17, 12, '10.90.190.236', '98:06:37:70:00:99', 1, 0, 1, NULL, false),
  (18, 13, '10.90.190.237', '98:06:37:70:00:AA', 1, 0, 1, NULL, false),
  (19, 14, '10.90.190.238', '98:06:37:70:00:C4', 1, 0, 1, NULL, false),
  (20, 15, '10.90.190.239', '98:06:37:70:00:85', 1, 0, 1, NULL, false),
  (21, 16, '10.90.247.221', '00:06:AC:E0:0D:48', 1, 0, 1, NULL, false),
  (22, 16, '10.90.247.222', '98:06:37:70:00:37', 1, 0, 2, NULL, false),
  (23, 17, '10.90.247.223', '98:06:37:70:00:44', 1, 0, 1, NULL, false),
  (24, 17, '10.90.247.224', '98:06:37:70:00:19', 1, 0, 2, NULL, false),
  (25, 18, '10.90.247.225', '98:06:37:70:00:6B', 1, 0, 1, NULL, false),
  (26, 18, '10.90.247.226', '98:06:37:70:00:7A', 1, 0, 2, NULL, false),
  (27, 19, '10.90.247.227', '98:06:37:70:00:74', 1, 0, 1, NULL, false),
  (28, 19, '10.90.247.228', '98:06:37:70:00:A7', 1, 0, 2, NULL, false),
  (29, 20, '10.90.151.221', '98:06:37:70:00:31', 1, 0, 1, NULL, false),
  (30, 20, '10.90.151.222', '98:06:37:70:00:38', 1, 0, 2, NULL, false),
  (31, 21, '10.90.151.223', '98:06:37:70:00:46', 1, 0, 1, NULL, false),
  (32, 21, '10.90.151.224', '98:06:37:70:00:45', 1, 0, 2, NULL, false),
  (33, 22, '10.90.151.225', '98:06:37:70:00:7C', 1, 0, 1, NULL, false),
  (34, 23, '10.90.151.226', '98:06:37:70:00:86', 1, 0, 1, NULL, false),
  (35, 24, '10.90.151.227', '98:06:37:70:00:8D', 1, 0, 1, NULL, false),
  (36, 25, '10.90.151.228', '98:06:37:70:00:89', 1, 0, 1, NULL, false),
  (37, 26, '10.90.151.229', '98:06:37:70:00:8B', 1, 0, 1, NULL, false),
  (38, 27, '10.90.151.230', '98:06:37:70:00:8F', 1, 0, 1, NULL, false),
  (39, 28, '10.90.148.241', '98:06:37:70:00:1A', 1, 0, 1, NULL, false),
  (40, 29, '10.90.148.242', '00:06:AC:E0:0D:88', 1, 0, 1, NULL, false),
  (41, 30, '10.90.148.243', '98:06:37:70:00:3D', 1, 0, 1, NULL, false),
  (42, 30, '10.90.148.244', '00:06:AC:E0:0D:76', 1, 0, 2, NULL, false),
  (43, 31, '10.90.148.245', '00:06:AC:E0:0D:81', 1, 0, 1, NULL, false),
  (44, 31, '10.90.148.246', '98:06:37:70:00:1D', 1, 0, 2, NULL, false),
  (45, 32, '10.90.87.201', '00:06:AC:E0:0D:4B', 1, 0, 1, NULL, false),
  (46, 32, '10.90.87.202', '00:06:AC:E0:0D:5B', 1, 0, 2, NULL, false),
  (47, 32, '10.90.87.203', '00:06:AC:E0:0D:75', 1, 0, 3, NULL, false),
  (48, 33, '10.90.87.212', '98:06:37:70:00:21', 1, 0, 1, NULL, false),
  (49, 33, '10.90.87.213', '98:06:37:70:00:27', 1, 0, 2, NULL, false),
  (50, 33, '10.90.87.214', '98:06:37:70:00:47', 1, 0, 3, NULL, false),
  (51, 34, '10.90.87.216', '98:06:37:70:00:8A', 1, 0, 1, NULL, false),
  (52, 34, '10.90.87.215', '98:06:37:70:00:83', 1, 0, 2, NULL, false),
  (53, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (54, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (55, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (56, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (57, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (58, 35, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (59, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (60, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (61, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (62, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (63, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (64, 36, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (65, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (66, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (67, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (68, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (69, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (70, 37, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (71, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (72, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (73, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (74, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (75, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (76, 38, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (77, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (78, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (79, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (80, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (81, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (82, 39, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (83, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (84, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (85, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (86, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (87, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (88, 40, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (89, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 1, NULL, false),
  (90, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 2, NULL, false),
  (91, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 3, NULL, false),
  (92, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 4, NULL, false),
  (93, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 5, NULL, false),
  (94, 41, '10.90.87.217', '98:06:37:70:04:0D', 1, 0, 6, NULL, false),
  (95, 42, '10.90.154.221', '00:06:AC:E0:0D:70', 1, 0, 1, NULL, false),
  (96, 43, '10.90.154.222', '00:06:AC:E0:0D:6D', 1, 0, 1, NULL, false),
  (97, 44, '10.90.154.223', '00:06:AC:E0:0D:44', 1, 0, 1, NULL, false),
  (98, 45, '10.90.154.224', '00:06:AC:E0:0D:43', 1, 0, 1, NULL, false),
  (99, 46, '10.90.154.226', '00:06:AC:E0:0D:72', 1, 0, 1, NULL, false),
  (100, 46, '10.90.154.227', '00:06:AC:E0:0D:73', 1, 0, 2, NULL, false),
  (101, 46, '10.90.154.228', '00:06:AC:E0:0D:71', 1, 0, 3, NULL, false),
  (102, 47, '10.90.154.225', '00:06:AC:E0:0D:4E', 1, 0, 1, NULL, false),
  (103, 48, '10.90.154.211', '98:06:37:70:00:3B', 1, 0, 1, NULL, false),
  (104, 49, '10.90.154.212', '98:06:37:70:00:2F', 1, 0, 1, NULL, false),
  (105, 50, '10.90.154.214', '98:06:37:70:00:C3', 1, 0, 1, NULL, false),
  (106, 51, '10.90.154.213', '98:06:37:70:00:81', 1, 0, 1, NULL, false),
  (107, 52, '10.90.157.236', '98:06:37:70:00:42', 1, 0, 1, NULL, false),
  (108, 53, '10.90.162.230', NULL, 1, 0, 1, NULL, false),
  (109, 54, '10.90.162.231', NULL, 1, 0, 1, NULL, false),
  (110, 55, '10.90.162.232', NULL, 1, 0, 1, NULL, false),
  (111, 56, '10.90.162.233', NULL, 1, 0, 1, NULL, false),
  (112, 57, '10.90.164.239', '98:06:37:70:00:39', 1, 0, 1, NULL, false),
  (113, 58, '10.90.164.240', '98:06:37:70:00:01', 1, 0, 1, NULL, false),
  (114, 59, '10.90.164.241', '00:06:AC:E0:0D:62', 1, 0, 1, NULL, false),
  (115, 60, NULL, NULL, 1, 0, 1, NULL, false),
  (116, 60, NULL, NULL, 1, 0, 2, NULL, false),
  (117, 61, NULL, NULL, 1, 0, 1, NULL, false),
  (118, 61, NULL, NULL, 1, 0, 2, NULL, false),
  (119, 62, NULL, NULL, 1, 0, 1, NULL, false),
  (120, 62, NULL, NULL, 1, 0, 2, NULL, false),
  (121, 63, '10.90.31.228', '98:06:37:70:00:AC', 1, 0, 1, NULL, false),
  (122, 64, '10.90.31.227', '98:06:37:70:00:17', 1, 0, 1, NULL, false),
  (123, 65, '10.90.31.225', '00:06:AC:E0:0D:67', 1, 0, 1, NULL, false),
  (124, 66, '10.90.31.229', '98:06:37:70:00:28', 1, 0, 1, NULL, false),
  (125, 67, '10.90.200.221', '00:06:AC:E0:0C:A3', 1, 0, 1, NULL, false),
  (126, 68, '10.90.200.222', '98:06:37:70:00:3C', 1, 0, 1, NULL, false),
  (127, 69, '10.90.200.223', '98:06:37:70:00:4E', 1, 0, 1, NULL, false),
  (128, 70, '10.90.200.224', '00:06:AC:E0:0D:7B', 1, 0, 1, NULL, false),
  (129, 71, '10.90.200.225', '98:06:37:70:00:12', 1, 0, 1, NULL, false),
  (130, 72, '10.90.200.226', '00:06:AC:E0:0D:74', 1, 0, 1, NULL, false),
  (131, 73, '10.90.200.227', '98:06:37:70:00:13', 1, 0, 1, NULL, false),
  (132, 74, '10.90.200.228', '00:06:AC:E0:0D:55', 1, 0, 1, NULL, false),
  (133, 75, '10.90.200.229', '00:06:AC:E0:0D:78', 1, 0, 1, NULL, false),
  (134, 76, '10.90.200.230', '00:06:AC:E0:0D:7A', 1, 0, 1, NULL, false),
  (135, 77, '10.90.23.221', '98:06:37:70:00:29', 1, 0, 1, NULL, false),
  (136, 78, '10.90.23.222', '98:06:37:70:00:20', 1, 0, 1, NULL, false),
  (137, 79, '10.90.23.223', '98:06:37:70:00:62', 1, 0, 1, NULL, false),
  (138, 80, '10.90.23.224', '98:06:37:70:00:2A', 1, 0, 1, NULL, false),
  (139, 81, '10.90.23.225', '00:06:AC:E0:0D:66', 1, 0, 1, NULL, false),
  (140, 81, '10.90.23.226', '00:06:AC:E0:0D:6B', 1, 0, 2, NULL, false),
  (141, 81, '10.90.23.227', '98:06:37:70:00:43', 1, 0, 3, NULL, false),
  (142, 81, '10.90.23.228', '00:06:AC:E0:0D:84', 1, 0, 4, NULL, false),
  (143, 82, '10.90.195.221', '98:06:37:70:00:49', 1, 0, 1, NULL, false),
  (144, 83, '10.90.195.222', '98:06:37:70:00:33', 1, 0, 1, NULL, false),
  (145, 84, '10.90.195.223', '98:06:37:70:00:25', 1, 0, 1, NULL, false),
  (146, 85, '10.90.195.224', '00:06:AC:E0:0D:85', 1, 0, 1, NULL, false),
  (147, 86, '10.90.195.225', '98:06:37:70:00:00', 1, 0, 1, NULL, false),
  (148, 87, '10.90.195.226', '00:06:AC:E0:0D:59', 1, 0, 1, NULL, false),
  (149, 88, '10.90.195.227', '00:06:AC:E0:0D:7D', 1, 0, 1, NULL, false),
  (150, 89, '10.90.195.228', '00:06:AC:E0:0D:77', 1, 0, 1, NULL, false),
  (151, 90, '10.90.195.229', '98:06:37:70:00:72', 1, 0, 1, NULL, false),
  (152, 91, '10.90.36.237', '98:06:37:70:00:1E', 1, 0, 1, NULL, false),
  (153, 92, '10.90.36.238', '98:06:37:70:00:1F', 1, 0, 1, NULL, false),
  (154, 93, '10.90.36.239', '98:06:37:70:00:9D', 1, 0, 1, NULL, false),
  (155, 94, '10.90.36.240', '98:06:37:70:00:67', 1, 0, 1, NULL, false),
  (156, 95, '10.90.180.234', '98:06:37:70:00:80', 1, 0, 1, NULL, false),
  (157, 95, '10.90.180.235', '98:06:37:70:00:84', 1, 0, 2, NULL, false),
  (158, 95, '10.90.180.236', '98:06:37:70:00:7E', 1, 0, 3, NULL, false),
  (159, 95, '10.90.180.237', '98:06:37:70:00:A8', 1, 0, 4, NULL, false),
  (160, 95, '10.90.180.238', '98:06:37:70:00:A2', 1, 0, 5, NULL, false),
  (161, 96, '10.90.178.233', '98:06:37:70:00:88', 1, 0, 1, NULL, false),
  (162, 97, '10.90.93.236', '98:06:37:70:00:77', 1, 0, 1, NULL, false),
  (163, 98, '10.90.93.237', '98:06:37:70:00:7B', 1, 0, 1, NULL, false),
  (164, 99, '10.90.93.238', '98:06:37:70:00:65', 1, 0, 1, NULL, false),
  (165, 100, '10.90.93.231', '98:06:37:70:00:30', 1, 0, 1, NULL, false),
  (166, 100, '10.90.93.232', '00:06:AC:E0:0D:86', 1, 0, 2, NULL, false),
  (167, 101, '10.90.93.233', '98:06:37:70:00:35', 1, 0, 1, NULL, false),
  (168, 101, '10.90.93.234', '98:06:37:70:00:15', 1, 0, 2, NULL, false),
  (169, 102, '10.90.197.238', '00:06:AC:E0:0D:6F', 1, 0, 1, NULL, false),
  (170, 103, '10.90.197.239', '00:06:AC:E0:0D:51', 1, 0, 1, NULL, false),
  (171, 104, '10.90.208.250', '98:06:37:70:00:9E', 1, 0, 1, NULL, false),
  (172, 104, '10.90.208.251', '98:06:37:70:00:97', 1, 0, 2, NULL, false),
  (173, 105, '10.90.104.238', '98:06:37:70:00:6D', 1, 0, 1, NULL, false),
  (174, 105, '10.90.104.239', '98:06:37:70:00:68', 1, 0, 2, NULL, false),
  (175, 106, '10.90.89.236', '00:06:AC:E0:0D:45', 1, 0, 1, NULL, false),
  (176, 106, '10.90.89.237', '00:06:AC:E0:0D:5D', 1, 0, 2, NULL, false),
  (177, 107, '10.90.89.232', '98:06:37:70:14:8B', 1, 0, 1, NULL, false),
  (178, 108, '10.90.152.239', NULL, 1, 0, 1, NULL, false),
  (179, 108, NULL, NULL, 1, 0, 2, NULL, false),
  (180, 109, '10.90.40.246', '98:06:37:70:00:6C', 1, 0, 1, NULL, false),
  (181, 110, '10.90.40.247', '98:06:37:70:00:9C', 1, 0, 1, NULL, false),
  (182, 111, NULL, NULL, 1, 0, 1, NULL, false),
  (183, 112, NULL, NULL, 1, 0, 1, NULL, false),
  (184, 113, NULL, NULL, 1, 0, 1, NULL, false),
  (185, 114, NULL, NULL, 1, 0, 1, NULL, false),
  (186, 115, NULL, NULL, 1, 0, 1, NULL, false),
  (187, 116, NULL, NULL, 1, 0, 1, NULL, false),
  (188, 117, NULL, NULL, 1, 0, 1, NULL, false),
  (189, 118, NULL, NULL, 1, 0, 1, NULL, false),
  (190, 119, '10.90.171.227', '98:06:37:70:00:73', 1, 0, 1, NULL, false),
  (191, 120, '10.90.171.222', '98:06:37:70:00:A9', 1, 0, 1, NULL, false),
  (192, 121, '10.90.171.228', '98:06:37:70:00:75', 1, 0, 1, NULL, false),
  (193, 122, '10.90.171.224', '98:06:37:70:00:A1', 1, 0, 1, NULL, false),
  (194, 123, '10.90.171.229', '98:06:37:70:00:6E', 1, 0, 1, NULL, false),
  (195, 124, '10.90.171.230', '98:06:37:70:04:06', 1, 0, 1, NULL, false),
  (196, 125, '10.90.171.225', '98:06:37:70:00:73', 1, 0, 1, NULL, false),
  (197, 126, '10.90.171.226', '98:06:37:70:00:A6', 1, 0, 1, NULL, false),
  (198, 127, NULL, NULL, 1, 0, 1, NULL, false),
  (199, 127, NULL, NULL, 1, 0, 2, NULL, false),
  (200, 128, NULL, NULL, 1, 0, 1, NULL, false),
  (201, 128, NULL, NULL, 1, 0, 2, NULL, false),
  (202, 129, '10.90.94.243', '98:06:37:70:00:66', 1, 0, 1, NULL, false),
  (203, 129, '10.90.94.244', '98:06:37:70:00:76', 1, 0, 2, NULL, false),
  (204, 130, '10.90.94.245', '98:06:37:70:00:A5', 1, 0, 1, NULL, false),
  (205, 130, '10.90.94.246', '98:06:37:70:00:9A', 1, 0, 2, NULL, false),
  (206, 131, '10.90.94.247', '98:06:37:70:00:69', 1, 0, 1, NULL, false),
  (207, 158, '10.90.94.248', '98:06:37:70:00:79', 1, 0, 1, NULL, false),
  (208, 132, '10.90.94.236', '98:06:37:70:00:8E', 1, 0, 1, NULL, false),
  (209, 133, '10.90.94.237', '98:06:37:70:00:9B', 1, 0, 1, NULL, false),
  (210, 134, '10.90.94.238', '00:06:AC:E0:0D:80', 1, 0, 1, NULL, false),
  (211, 135, '10.90.94.239', '00:06:AC:E0:0D:82', 1, 0, 1, NULL, false),
  (212, 136, NULL, NULL, 1, 0, 1, NULL, false),
  (213, 136, NULL, NULL, 1, 0, 2, NULL, false),
  (214, 137, '10.90.35.232', '98:06:37:70:00:92', 1, 0, 1, NULL, false),
  (215, 138, '10.90.35.233', '98:06:37:70:00:6F', 1, 0, 1, NULL, false),
  (216, 139, '10.90.96.231', '98:06:37:70:14:94', 1, 0, 1, NULL, false),
  (217, 139, '10.90.96.232', '98:06:37:70:14:8F', 1, 0, 2, NULL, false),
  (218, 140, '10.90.96.233', '98:06:37:70:14:8E', 1, 0, 1, NULL, false),
  (219, 140, '10.90.96.234', '98:06:37:70:14:95', 1, 0, 2, NULL, false),
  (220, 141, '10.90.96.235', '98:06:37:70:14:91', 1, 0, 1, NULL, false),
  (221, 141, '10.90.96.236', '98:06:37:70:14:98', 1, 0, 2, NULL, false),
  (222, 142, '10.90.96.17', '98:06:37:70:14:8D', 1, 0, 1, NULL, false),
  (223, 142, '10.90.96.238', '98:06:37:70:14:90', 1, 0, 2, NULL, false),
  (224, 143, '10.90.41.232', '98:06:37:70:02:0B', 1, 0, 1, NULL, false),
  (225, 144, '10.90.41.233', '98:06:37:70:02:68', 1, 0, 1, NULL, false),
  (226, 145, '10.90.41.234', '98:06:37:70:02:69', 1, 0, 1, NULL, false),
  (227, 145, '10.90.41.235', '98:06:37:70:02:66', 1, 0, 2, NULL, false),
  (228, 145, '10.90.41.236', '98:06:37:70:02:08', 1, 0, 3, NULL, false),
  (229, 146, '10.90.37.233', '00:06:AC:E0:0D:61', 1, 0, 1, NULL, false),
  (230, 147, '10.90.37.234', '00:06:AC:E0:0D:41', 1, 0, 1, NULL, false),
  (231, 148, '10.90.37.235', '00:06:AC:E0:0D:69', 1, 0, 1, NULL, false),
  (232, 149, '10.90.37.236', '00:06:AC:E0:0D:42', 1, 0, 1, NULL, false),
  (233, 150, '10.90.37.237', '00:06:AC:E0:0D:63', 1, 0, 1, NULL, false),
  (234, 151, '10.90.37.238', '00:06:AC:E0:0D:64', 1, 0, 1, NULL, false),
  (235, 152, '10.90.198.233', '00:06:AC:E0:0D:49', 1, 0, 1, NULL, false),
  (236, 153, '10.90.198.234', '00:06:AC:E0:0D:53', 1, 0, 1, NULL, false),
  (237, 154, '10.90.198.235', '98:06:37:70:00:2D', 1, 0, 1, NULL, false),
  (238, 154, '10.90.198.236', '98:06:37:70:00:34', 1, 0, 2, NULL, false),
  (239, 155, '10.90.198.237', '00:06:AC:E0:0D:47', 1, 0, 1, NULL, false),
  (240, 155, '10.90.198.238', '00:06:AC:E0:0D:5C', 1, 0, 2, NULL, false),
  (241, 155, '10.90.198.239', '00:06:AC:E0:0D:57', 1, 0, 3, NULL, false),
  (242, 156, '10.90.47.236', '98:06:37:70:00:A4', 1, 0, 1, NULL, false),
  (243, 156, '10.90.47.237', '98:06:37:70:00:9F', 1, 0, 2, NULL, false),
  (244, 157, '10.90.47.238', '98:06:37:70:00:A3', 1, 0, 1, NULL, false),
  (245, 157, '10.90.47.239', '98:06:37:70:00:90', 1, 0, 2, NULL, false),
  (246, 9, NULL, NULL, 1, 0, 2, NULL, false);

-- 4) 채널 설정 — 압축기마다 CH01~07 7행 (기존 시드 기본값과 동일)
INSERT INTO "CompressorChannelSettings" ("CompressorId", "ChannelNo", "ChannelName", "Unit", "Enabled", "LowerLimit", "UpperLimit", "AlarmEnabled", "AlarmDelaySeconds", "AlarmClearDelaySeconds", "DecimalPlaces")
SELECT c."Id", ch."no", ch."name", ch."unit", true, 0, 1000, true, 30, 30, ch."dp"
FROM "Compressors" c CROSS JOIN (VALUES
  (1, '저온', '℃', 1),
  (2, '고온', '℃', 1),
  (3, '오일온도', '℃', 1),
  (4, '저압', 'MPa', 2),
  (5, '고압', 'MPa', 2),
  (6, '오일압력', 'MPa', 2),
  (7, '운전전류', 'A', 1)
) AS ch("no", "name", "unit", "dp");

-- 4-1) CH01~CH07 레지스터 주소(표준 장비 기준). 압축기별 설정값이라 채널 설정에 둔다.
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = CASE "ChannelNo"
    WHEN 1 THEN 360 WHEN 2 THEN 361 WHEN 3 THEN 362 WHEN 4 THEN 364
    WHEN 5 THEN 365 WHEN 6 THEN 366 WHEN 7 THEN 367 END;

-- 4-2) LG 냉동기(배터리 #1~#7): 압축기별 주소, CH03·CH06은 센서 없음(NULL) + 사용 안 함
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 360 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 361 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 362 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 363 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 364 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 365 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 366 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 367 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 368 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 369 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 370 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 371 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 372 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 373 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 374 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 375 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 376 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 377 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 378 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 379 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 380 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 381 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 382 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 383 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 384 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #1' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 390 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 391 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 392 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 393 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 394 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 395 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 396 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 397 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 398 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 399 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 400 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 401 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 402 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 403 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 404 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 405 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 406 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 407 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 408 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 409 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 410 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 411 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 412 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 413 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 414 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #2' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 420 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 421 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 422 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 423 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 424 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 425 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 426 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 427 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 428 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 429 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 430 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 431 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 432 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 433 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 434 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 435 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 436 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 437 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 438 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 439 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 440 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 441 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 442 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 443 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 444 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #3' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 450 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 451 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 452 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 453 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 454 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 455 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 456 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 457 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 458 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 459 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 460 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 461 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 462 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 463 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 464 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 465 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 466 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 467 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 468 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 469 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 470 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 471 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 472 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 473 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 474 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #4' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 480 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 481 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 482 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 483 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 484 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 485 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 486 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 487 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 488 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 489 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 490 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 491 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 492 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 493 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 494 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 495 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 496 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 497 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 498 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 499 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 500 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 501 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 502 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 503 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 504 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #5' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 510 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 511 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 512 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 513 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 514 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 515 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 516 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 517 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 518 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 519 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 520 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 521 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 522 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 523 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 524 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 525 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 526 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 527 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 528 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 529 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 530 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 531 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 532 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 533 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 534 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #6' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 540 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 541 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 542 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 543 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 1);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 544 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 545 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 546 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 547 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 2);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 548 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 549 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 550 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 551 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 3);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 552 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 553 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 554 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 555 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 4);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 556 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 557 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 558 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 559 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 5);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 560 WHERE "ChannelNo" = 1 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 561 WHERE "ChannelNo" = 2 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 3 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 562 WHERE "ChannelNo" = 4 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 563 WHERE "ChannelNo" = 5 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = NULL WHERE "ChannelNo" = 6 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "RegisterAddress" = 564 WHERE "ChannelNo" = 7 AND "CompressorId" = (SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId" WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #7' AND c."SequenceNo" = 6);
UPDATE "CompressorChannelSettings" SET "Enabled" = false WHERE "RegisterAddress" IS NULL;

-- 5) id 시퀀스를 마지막 id 다음으로 맞춘다 (이후 API 등록이 충돌하지 않도록)
SELECT setval(pg_get_serial_sequence('"Equipments"', 'Id'), (SELECT MAX("Id") FROM "Equipments"));
SELECT setval(pg_get_serial_sequence('"Compressors"', 'Id'), (SELECT MAX("Id") FROM "Compressors"));

COMMIT;
