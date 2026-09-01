-- 채널 한글 명칭/단위 적용 (overview.md 8.1 센서 채널 정의 표 기준). 전 압축기 공통.
UPDATE "CompressorChannelSettings" SET "ChannelName" = '저온', "Unit" = '℃' WHERE "ChannelNo" = 1;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '고온', "Unit" = '℃' WHERE "ChannelNo" = 2;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '오일온도', "Unit" = '℃' WHERE "ChannelNo" = 3;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '저압', "Unit" = 'MPa' WHERE "ChannelNo" = 4;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '고압', "Unit" = 'MPa' WHERE "ChannelNo" = 5;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '오일압력', "Unit" = 'MPa' WHERE "ChannelNo" = 6;
UPDATE "CompressorChannelSettings" SET "ChannelName" = '운전전류', "Unit" = 'A' WHERE "ChannelNo" = 7;
