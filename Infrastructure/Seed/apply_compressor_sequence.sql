-- 압축기의 소속 장비 내 순번(SequenceNo)을 계산해서 채운다. 같은 장비 소속 압축기끼리
-- Id 오름차순으로 1부터 번호를 매긴다 (경보 메시지 등에서 "압축기 1번"으로 표시할 때 사용).
UPDATE "Compressors" c
SET "SequenceNo" = sub."SequenceNo"
FROM (
    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "EquipmentId" ORDER BY "Id") AS "SequenceNo"
    FROM "Compressors"
) sub
WHERE c."Id" = sub."Id";
