using HRMS.Infrastructure;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Logging.Models;

namespace HRMS.Modules.Logging;

// EventLog 저장 헬퍼. 서비스/리포지토리 계층 없이 DbContext를 직접 받아서 한 줄로 기록한다.
public static class EventLogger
{
    public static async Task LogAsync(
        AppDbContext db, EventLogCategory category, string message, string? username = null,
        int? equipmentId = null, int? compressorId = null, ChannelNo? channelNo = null)
    {
        db.EventLogs.Add(new EventLog
        {
            Category = category,
            Message = message,
            Username = username,
            EquipmentId = equipmentId,
            CompressorId = compressorId,
            ChannelNo = channelNo,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    //-----------------------------------------------------------------------------//
    // 경보 이벤트 전용. 위 LogAsync에 상세 스냅샷 파라미터를 더 붙이면 인자가 12개가 되어
    // 호출부를 읽기 어려워지므로 별도 메서드로 둔다.
    // value는 그 시점의 raw 측정값, setting은 판정에 쓰인 채널 설정이다 — 임계값/단위/소수점을
    // 여기서 스냅샷으로 떠서 저장한다(이유는 EventLog의 주석 참고).
    //-----------------------------------------------------------------------------//
    public static async Task LogAlarmAsync(
        AppDbContext db, string message, int? equipmentId, int compressorId,
        ChannelNo channelNo, short? value, CompressorChannelSetting setting)
    {
        db.EventLogs.Add(new EventLog
        {
            Category = EventLogCategory.Alarm,
            Message = message,
            EquipmentId = equipmentId,
            CompressorId = compressorId,
            ChannelNo = channelNo,
            Value = value,
            LowerLimit = setting.LowerLimit,
            UpperLimit = setting.UpperLimit,
            DecimalPlaces = setting.DecimalPlaces,
            Unit = setting.Unit,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
