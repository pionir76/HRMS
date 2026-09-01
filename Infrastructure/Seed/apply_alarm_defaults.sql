UPDATE "CompressorChannelSettings"
SET "LowerLimit" = 0, "UpperLimit" = 1000, "DecimalPlaces" = 1, "Enabled" = true, "AlarmEnabled" = true,
    "AlarmDelaySeconds" = 30, "AlarmClearDelaySeconds" = 30;

UPDATE "Equipments" SET "RunningCurrentThreshold" = 10;
