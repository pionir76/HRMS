namespace HRMS.Modules.Equipment.Models;

//--------------------------------------------------------------------------------//
// 압축기 신규 등록 시 CH01~07 채널 설정 7행을 채우는 기본값. 기존 Infrastructure/Seed의
// Infrastructure/Seed/seed_from_csv.sql이 시드 때 채우는 값을 API 등록 시점에 재현한다.
// 단 CH03(오일온도)은 여기서 이름을 비워둔다 — 시드와 다른 부분이니 주의(예비 채널 취급).
// Enabled/AlarmEnabled는 CompressorChannelSetting의 속성 기본값을 그대로 쓴다.
//--------------------------------------------------------------------------------//
public static class ChannelDefaults
{
    private static readonly Dictionary<ChannelNo, (string Name, string Unit)> NamesAndUnits = new()
    {
        [ChannelNo.CH01] = ("저온", "℃"),
        [ChannelNo.CH02] = ("고온", "℃"),
        [ChannelNo.CH04] = ("저압", "MPa"),
        [ChannelNo.CH05] = ("고압", "MPa"),
        [ChannelNo.CH06] = ("오일압력", "MPa"),
        [ChannelNo.CH07] = ("운전전류", "A"),
    };

    //----------------------------------------------------------------------------------//
    // 표준 장비(구형 센서 설치형 + Fric)의 D-Register 주소. LG 냉동기처럼 압축기마다 주소가
    // 다른 장비는 등록 후 관리 화면에서 고쳐야 한다(Doc/pclink protocol.md 참고).
    //----------------------------------------------------------------------------------//
    public static readonly Dictionary<ChannelNo, int> StandardRegisters = new()
    {
        [ChannelNo.CH01] = 360,
        [ChannelNo.CH02] = 361,
        [ChannelNo.CH03] = 362,
        [ChannelNo.CH04] = 364,
        [ChannelNo.CH05] = 365,
        [ChannelNo.CH06] = 366,
        [ChannelNo.CH07] = 367,
    };

    //----------------------------------------------------------------------------------//
    // 단위별 표시 소수점 자리수 기본값(사용자 결정, 2026-09-14): 압력(MPa) 둘째자리,
    // 온도(℃)/전압(V)/전류(A) 첫째자리. 기존 DB 데이터도 이 기준으로 직접 UPDATE해서 맞춰뒀다
    // (CompressorChannelSettings.Unit 기준 일괄 수정, API를 거치지 않음).
    //----------------------------------------------------------------------------------//
    private static readonly Dictionary<string, int> DecimalPlacesByUnit = new()
    {
        ["MPa"] = 2,
        ["℃"] = 1,
        ["V"] = 1,
        ["A"] = 1,
    };

    public static List<CompressorChannelSetting> CreateAll() =>
        Enum.GetValues<ChannelNo>()
            .Select(ch =>
            {
                var setting = new CompressorChannelSetting { ChannelNo = ch, RegisterAddress = StandardRegisters[ch] };
                if (NamesAndUnits.TryGetValue(ch, out var nameAndUnit))
                {
                    setting.ChannelName = nameAndUnit.Name;
                    setting.Unit = nameAndUnit.Unit;
                    if (DecimalPlacesByUnit.TryGetValue(nameAndUnit.Unit, out var decimalPlaces))
                        setting.DecimalPlaces = decimalPlaces;
                }
                return setting;
            })
            .ToList();
}
