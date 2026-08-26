using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Operation.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment;

// 채널 → 압축기 → 장비, 2단계로 "가장 심각한 상태로 집계"한다 (overview.md 8.1 상태 판정).
// CompressorPollingService의 매 폴링 사이클 마지막 단계로 호출된다. 장비/압축기 수가
// 이 시스템 규모에서는 몇백 대 수준이라, 대상만 골라내지 않고 매번 전체를 다시 계산해도 무리 없다.
public static class EquipmentStatusAggregator
{
    public static async Task UpdateAsync(AppDbContext db, CancellationToken stoppingToken)
    {
        var channelsByCompressor = (await db.CompressorSensorCurrents.ToListAsync(stoppingToken))
            .GroupBy(s => s.CompressorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var compressors = await db.Compressors.ToListAsync(stoppingToken);

        // 1단계: 채널 → 압축기
        foreach (var compressor in compressors)
        {
            if (channelsByCompressor.TryGetValue(compressor.Id, out var channels) && channels.Count > 0)
                compressor.AlarmStatus = channels.Max(c => c.AlarmStatus, AlarmSeverity);
        }

        var compressorsByEquipment = compressors.GroupBy(c => c.EquipmentId).ToDictionary(g => g.Key, g => g.ToList());
        var equipments = await db.Equipments.ToListAsync(stoppingToken);

        // 2단계: 압축기 → 장비 (통신/경보 집계 + 운전전류 판정)
        foreach (var equipment in equipments)
        {
            if (!compressorsByEquipment.TryGetValue(equipment.Id, out var members) || members.Count == 0)
                continue;

            equipment.CommunicationStatus = members.Max(c => c.CommunicationStatus, CommunicationSeverity);
            equipment.AlarmStatus = members.Max(c => c.AlarmStatus, AlarmSeverity);
            equipment.RunningStatus = IsRunning(equipment, members, channelsByCompressor)
                ? RunningStatus.운전
                : RunningStatus.정지;
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static bool IsRunning(
        Models.Equipment equipment,
        List<Compressor> members,
        Dictionary<int, List<CompressorSensorCurrent>> channelsByCompressor)
    {
        if (equipment.RunningCurrentThreshold is not { } threshold)
            return false; // 임계값 미설정 상태에서는 판정하지 않고 정지로 본다

        return members.Any(c =>
            channelsByCompressor.TryGetValue(c.Id, out var channels) &&
            channels.FirstOrDefault(ch => ch.ChannelNo == ChannelNo.CH07) is { } ch07 &&
            ch07.Value > threshold);
    }

    // 값이 클수록 더 심각한 상태로 취급한다. enum 선언 순서와 무관하게 명시적으로 정의한다.
    private static int AlarmSeverity(AlarmStatus status) => status switch
    {
        AlarmStatus.경보발생 => 4,
        AlarmStatus.정상복귀대기 => 3,
        AlarmStatus.경보발생대기 => 2,
        AlarmStatus.정상 => 1,
        AlarmStatus.경보비활성화 => 0,
        _ => 0
    };

    private static int CommunicationSeverity(CommunicationStatus status) => status switch
    {
        CommunicationStatus.끊김 => 2,
        CommunicationStatus.재접속중 => 1,
        CommunicationStatus.연결됨 => 0,
        _ => 0
    };

    private static T Max<TSource, T>(this IEnumerable<TSource> source, Func<TSource, T> selector, Func<T, int> severity)
        => source.Select(selector).OrderByDescending(severity).First();
}
