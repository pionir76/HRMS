using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment;

public static class EquipmentStatusAggregator
{
    public static async Task UpdateAsync(AppDbContext db, CancellationToken stoppingToken)
    {
        var channelsByCompressor = (await db.CompressorSensorCurrents.ToListAsync(stoppingToken))
            .GroupBy(s => s.CompressorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var compressors = await db.Compressors.ToListAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        // 압축기 단위로 경보상태 집계
        //--------------------------------------------------------------------------------//
        foreach (var compressor in compressors)
        {
            if (channelsByCompressor.TryGetValue(compressor.Id, out var channels) && channels.Count > 0)
                compressor.AlarmStatus = AggregateAlarm(channels.Select(c => c.AlarmStatus));
        }

        //--------------------------------------------------------------------------------//
        // 장비별 압축기 그룹핑
        //--------------------------------------------------------------------------------//
        var compressorsByEquipment = compressors.GroupBy(c => c.EquipmentId).ToDictionary(g => g.Key, g => g.ToList());
        var equipments = await db.Equipments.ToListAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        // 장비 (통신/경보 집계 + 운전전류 판정)
        //--------------------------------------------------------------------------------//
        foreach (var equipment in equipments)
        {
            if (!compressorsByEquipment.TryGetValue(equipment.Id, out var members) || members.Count == 0)
                continue;

            equipment.CommunicationStatus = AggregateCommunication(members.Select(c => c.CommunicationStatus));
            equipment.AlarmStatus = AggregateAlarm(members.Select(c => c.AlarmStatus));
            equipment.IsRunning = IsRunning(equipment, members, channelsByCompressor);
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static bool IsRunning(
        Models.Equipment equipment,
        List<Compressor> members,
        Dictionary<int, List<CompressorSensorCurrent>> channelsByCompressor)
    {
        //--------------------------------------------------------------------------------//
        // 임계값 미설정 상태에서는 판정하지 않고 정지로 본다
        //--------------------------------------------------------------------------------//
        if (equipment.RunningCurrentThreshold is not { } threshold)
            return false; 

        //--------------------------------------------------------------------------------//
        // 통신이 끊기거나(끊김) 막 끊긴 상태(재접속중)인 압축기는 CH07 값을 신뢰할 수 없으므로
        // 이전 값이 임계값을 넘었더라도 정지로 간주한다. 값을 갱신할 방법이 없어 그대로 얼어있는
        // 값이라, 이 조건이 없으면 통신이 끊긴 뒤에도 계속 운전 중으로 잘못 판정된다.
        //--------------------------------------------------------------------------------//
        return members.Any(c =>
            c.CommunicationStatus == CommunicationStatus.연결됨 &&
            channelsByCompressor.TryGetValue(c.Id, out var channels) &&
            channels.FirstOrDefault(ch => ch.ChannelNo == ChannelNo.CH07) is { } ch07 &&
            ch07.Value > threshold);
    }

    private static AlarmStatus AggregateAlarm(IEnumerable<AlarmStatus> statuses)
    {
        if (statuses.Contains(AlarmStatus.경보발생)) return AlarmStatus.경보발생;
        if (statuses.Contains(AlarmStatus.정상복귀대기)) return AlarmStatus.정상복귀대기;
        if (statuses.Contains(AlarmStatus.경보발생대기)) return AlarmStatus.경보발생대기;
        if (statuses.Contains(AlarmStatus.정상)) return AlarmStatus.정상;
        return AlarmStatus.경보비활성화;
    }

    private static CommunicationStatus AggregateCommunication(IEnumerable<CommunicationStatus> statuses)
    {
        if (statuses.Contains(CommunicationStatus.끊김)) return CommunicationStatus.끊김;
        if (statuses.Contains(CommunicationStatus.재접속중)) return CommunicationStatus.재접속중;
        return CommunicationStatus.연결됨;
    }
}
