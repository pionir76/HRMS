// Infrastructure/Equipments.csv, Compressors.csv -> Infrastructure/Seed/seed_from_csv.sql 생성
//
// 실행: 저장소 루트에서  node Infrastructure/Seed/generate_seed_from_csv.js
// 장비 자료(CSV)가 갱신되면 이 스크립트로 seed_from_csv.sql을 다시 만들고, 그 SQL을 psql로 실행한다
// (setup.md 6단계). SQL을 직접 손대지 말 것 — 다음 생성 때 덮어써진다.
// 사용자 결정:
//  - 상태: A지구 PDI 1동 실차환경챔버 #1(id 9)과 수소충전소 8대(id 111~118)만 미운영, 나머지 운영
//  - 운전전류 임계값: raw 100 (실제 10.0 A, 소수점 1자리)
//  - 기존 장비/압축기 및 딸린 수집·일지 데이터는 전부 삭제하고 현재 기준으로 리셋
const fs = require("fs");
const ROOT = ""; // 저장소 루트에서 실행하는 것을 전제로 한다
const OUT = "Infrastructure/Seed/seed_from_csv.sql";

const NON_OPERATIONAL = new Set([9, 111, 112, 113, 114, 115, 116, 117, 118]); // EquipmentStatus.미운영 = 1
const RUNNING_CURRENT_THRESHOLD = 100;

function parseCsv(text) {
  text = text.replace(/^\uFEFF/, "");
  const rows = []; let row = [], cell = "", q = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (q) { if (c === '"') { if (text[i + 1] === '"') { cell += '"'; i++; } else q = false; } else cell += c; }
    else if (c === '"') q = true;
    else if (c === ",") { row.push(cell); cell = ""; }
    else if (c === "\n") { row.push(cell); rows.push(row); row = []; cell = ""; }
    else if (c !== "\r") cell += c;
  }
  if (cell.length || row.length) { row.push(cell); rows.push(row); }
  return rows.filter(r => r.some(x => x !== ""));
}
const toObjs = f => { const r = parseCsv(fs.readFileSync(ROOT + f, "utf8")); const h = r[0]; return r.slice(1).map(x => Object.fromEntries(h.map((k, i) => [k, (x[i] ?? "").trim()]))); };

const S = v => (v === "" || v == null) ? "NULL" : "'" + String(v).replace(/'/g, "''") + "'";
const N = v => (v === "" || v == null) ? "NULL" : (Number.isNaN(Number(v)) ? "NULL" : String(Number(v)));
// 범위값: 빈칸이거나 0이면 "미설정"으로 본다(0~0 범위는 의미가 없고, 운전일지에서 "/"가 되어야 한다)
const R = v => (v === "" || Number(v) === 0) ? "NULL" : String(Number(v));
const B = v => (v === "1" || v.toLowerCase?.() === "true") ? "true" : "false";

const eq = toObjs("Infrastructure/Equipments.csv");
const comp = toObjs("Infrastructure/Compressors.csv");

const lines = [];
lines.push("-- 장비/압축기 전면 교체 (Infrastructure/Equipments.csv, Compressors.csv 기준, 2026-09-18)");
lines.push("-- 생성: Infrastructure/Seed/generate_seed_from_csv.js — SQL을 직접 편집하지 말고 스크립트로 다시 생성할 것");
lines.push("\\set ON_ERROR_STOP on");
lines.push("BEGIN;");
lines.push("");
lines.push("-- 1) 장비/압축기에 딸린 기존 데이터 전부 삭제 (사용자 결정: 현재 기준으로 리셋)");
lines.push(`DELETE FROM "Attachments" WHERE "OwnerType" IN (1, 2); -- EquipmentPhoto, EquipmentInspectionHistory`);
lines.push(`TRUNCATE TABLE "CompressorMeasurements", "CompressorSensorCurrents", "CompressorChannelSettings",`);
lines.push(`    "OperationItemValues", "OperationReferenceValues", "OperationLogs",`);
lines.push(`    "InspectionResults", "InspectionLogs", "RepairLogs", "EquipmentInspectionHistories",`);
lines.push(`    "EventLogs", "UserEquipments", "Compressors", "Equipments" RESTART IDENTITY CASCADE;`);
lines.push("");
lines.push("-- 2) 장비 157대");

const eqCols = ['Id','Region','BuildingNumber','BuildingName','Location','Name','Status','ManagementNumber','CompressorCount',
  'HasCoolingWater','CoolingWaterInletMin','CoolingWaterInletMax','CoolingWaterOutletMin','CoolingWaterOutletMax',
  'HasBrine','BrineInletMin','BrineInletMax','BrineOutletMin','BrineOutletMax',
  'HasVoltage','VoltageMin','VoltageMax','RunningCurrentThreshold','AlarmStatus','CommunicationStatus','IsRunning'];
lines.push(`INSERT INTO "Equipments" (${eqCols.map(c => `"${c}"`).join(", ")}) VALUES`);
const eqVals = eq.map(e => {
  const id = Number(e.id);
  const status = NON_OPERATIONAL.has(id) ? 1 : 0; // 1=미운영, 0=운영
  return "  (" + [
    id, S(e["지역"]), S(e["건물번호"] === "-" ? "" : e["건물번호"]), S(e["건물이름"]), S(e["위치"]), S(e["장비명"]), status,
    S(e["관리번호 (KGS)"]), N(e["압축기수량"]),
    B(e["냉각수유무"]), R(e["냉각수입구min"]), R(e["냉각수입구max"]), R(e["냉각수출구min"]), R(e["냉각수출구max"]),
    B(e["브라인유무"]), R(e["브라인입구min"]), R(e["브라인입구max"]), R(e["브라인출구min"]), R(e["브라인출구max"]),
    B(e["전압유무"]), R(e["전압min"]), R(e["전압max"]),
    RUNNING_CURRENT_THRESHOLD, 0 /*AlarmStatus 정상*/, 1 /*CommunicationStatus 끊김 — 아직 폴링 전*/, "false"
  ].join(", ") + ")";
});
lines.push(eqVals.join(",\n") + ";");
lines.push("");

lines.push("-- 3) 압축기 245대 (SequenceNo는 장비별로 id 순서대로 1부터 부여)");
const seqByEq = {};
const compVals = comp.map(c => {
  const eqId = Number(c.EquipmentId);
  seqByEq[eqId] = (seqByEq[eqId] || 0) + 1;
  return "  (" + [
    Number(c.id), eqId, S(c.IpAddress), S(c.MacAddress),
    1 /*끊김*/, 0 /*정상*/, seqByEq[eqId], "NULL" /*DisconnectedSince*/, "false" /*HasCommunicationAlarm*/
  ].join(", ") + ")";
});
lines.push(`INSERT INTO "Compressors" ("Id", "EquipmentId", "IpAddress", "MacAddress", "CommunicationStatus", "AlarmStatus", "SequenceNo", "DisconnectedSince", "HasCommunicationAlarm") VALUES`);
lines.push(compVals.join(",\n") + ";");
lines.push("");

lines.push("-- 4) 채널 설정 — 압축기마다 CH01~07 7행 (기존 시드 기본값과 동일)");
lines.push(`INSERT INTO "CompressorChannelSettings" ("CompressorId", "ChannelNo", "ChannelName", "Unit", "Enabled", "LowerLimit", "UpperLimit", "AlarmEnabled", "AlarmDelaySeconds", "AlarmClearDelaySeconds", "DecimalPlaces")`);
lines.push(`SELECT c."Id", ch."no", ch."name", ch."unit", true, 0, 1000, true, 30, 30, ch."dp"`);
lines.push(`FROM "Compressors" c CROSS JOIN (VALUES`);
lines.push([
  `  (1, '저온', '℃', 1)`, `  (2, '고온', '℃', 1)`, `  (3, '오일온도', '℃', 1)`,
  `  (4, '저압', 'MPa', 2)`, `  (5, '고압', 'MPa', 2)`, `  (6, '오일압력', 'MPa', 2)`, `  (7, '운전전류', 'A', 1)`
].join(",\n"));
lines.push(`) AS ch("no", "name", "unit", "dp");`);
lines.push("");

lines.push("-- 4-1) CH01~CH07 레지스터 주소(표준 장비 기준). 압축기별 설정값이라 채널 설정에 둔다.");
lines.push(`UPDATE "CompressorChannelSettings" SET "RegisterAddress" = CASE "ChannelNo"
    WHEN 1 THEN 360 WHEN 2 THEN 361 WHEN 3 THEN 362 WHEN 4 THEN 364
    WHEN 5 THEN 365 WHEN 6 THEN 366 WHEN 7 THEN 367 END;`);
lines.push("");

//--------------------------------------------------------------------------------//
// 4-2) LG 냉동기(환경차개발시험2동 배터리 #1~#7): TLC 1대가 압축기 42대를 중계하면서
// 압축기마다 주소가 다르다. 셀마다 30개 블록을 쓰고 그 안에서 6대가 4개씩 나눠 가지며,
// CH07(운전전류)은 블록의 25번째 주소를 6대가 공유한다. CH03(오일온도)·CH06(오일압력)은
// 센서가 없어 주소를 비우고 채널도 "사용 안 함"으로 둔다.
// 전체 주소표는 Doc/pclink protocol.md 참고 — 이 계산 결과와 같아야 한다.
//--------------------------------------------------------------------------------//
lines.push("-- 4-2) LG 냉동기(배터리 #1~#7): 압축기별 주소, CH03·CH06은 센서 없음(NULL) + 사용 안 함");
const LG_CELL_STARTS = [360, 390, 420, 450, 480, 510, 540];
const lgCompressor = (cell, seq) =>
  `(SELECT c."Id" FROM "Compressors" c JOIN "Equipments" e ON e."Id" = c."EquipmentId"` +
  ` WHERE e."BuildingName" = '환경차개발시험2동' AND e."Name" = '배터리 #${cell}' AND c."SequenceNo" = ${seq})`;
for (let cell = 1; cell <= 7; cell++) {
  const start = LG_CELL_STARTS[cell - 1];
  for (let seq = 1; seq <= 6; seq++) {
    const base = start + 4 * (seq - 1);
    const byChannel = { 1: base, 2: base + 1, 3: "NULL", 4: base + 2, 5: base + 3, 6: "NULL", 7: start + 24 };
    for (const ch of [1, 2, 3, 4, 5, 6, 7])
      lines.push(`UPDATE "CompressorChannelSettings" SET "RegisterAddress" = ${byChannel[ch]} WHERE "ChannelNo" = ${ch} AND "CompressorId" = ${lgCompressor(cell, seq)};`);
  }
}
lines.push(`UPDATE "CompressorChannelSettings" SET "Enabled" = false WHERE "RegisterAddress" IS NULL;`);
lines.push("");

lines.push("-- 5) id 시퀀스를 마지막 id 다음으로 맞춘다 (이후 API 등록이 충돌하지 않도록)");
lines.push(`SELECT setval(pg_get_serial_sequence('"Equipments"', 'Id'), (SELECT MAX("Id") FROM "Equipments"));`);
lines.push(`SELECT setval(pg_get_serial_sequence('"Compressors"', 'Id'), (SELECT MAX("Id") FROM "Compressors"));`);
lines.push("");
lines.push("COMMIT;");

fs.writeFileSync(OUT, lines.join("\n") + "\n", "utf8");
console.log(`생성 완료: ${OUT}`);
console.log(`장비 ${eq.length}행, 압축기 ${comp.length}행, 채널설정 ${comp.length * 7}행`);
console.log(`미운영 장비: ${[...NON_OPERATIONAL].join(", ")}`);
const seqMax = Math.max(...Object.values(seqByEq));
console.log(`장비당 압축기 최대 ${seqMax}대, SequenceNo 부여 완료`);
