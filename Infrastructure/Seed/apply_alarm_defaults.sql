UPDATE "CompressorChannelSettings"
SET "LowerLimit" = 0, "UpperLimit" = 1000, "DecimalPlaces" = 1, "Enabled" = true, "AlarmEnabled" = true;

UPDATE "Equipments" SET "RunningCurrentThreshold" = 10;
