INSERT INTO "CompressorChannelSettings" ("CompressorId", "ChannelNo", "Enabled", "AlarmEnabled")
SELECT c."Id", ch, true, true
FROM "Compressors" c
CROSS JOIN generate_series(1, 7) AS ch;
