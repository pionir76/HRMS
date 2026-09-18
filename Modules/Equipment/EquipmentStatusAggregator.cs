using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment;

//--------------------------------------------------------------------------------//
// 압축기/채널 데이터로부터 장비·압축기의 실시간 파생 상태를 계산한다. CompressorPollingService가
// 매 사이클 두 번에 나눠 호출한다:
//   1) UpdateRunningAsync — 채널값 저장 직후, 경보 판정 **전에** 장비 운전 여부를 먼저 정한다.
//      "장비가 운전 중이 아니면 경보 판정을 하지 않는다"(사용자 결정 2026-09-17)는 사양 때문에,
//      경보 판정이 이번 사이클의 운전 여부를 봐야 하기 때문이다.
//   2) UpdateAlarmAndCommunicationAsync — 경보 판정이 끝난 뒤 압축기/장비 단위 경보·통신 상태를 집계한다.
//--------------------------------------------------------------------------------//
public static class EquipmentStatusAggregator
{
    public static async Task UpdateRunningAsync(AppDbContext db, CancellationToken stoppingToken)
    {
        var channelsByCompressor = await LoadChannelsByCompressorAsync(db, stoppingToken);
        var compressorsByEquipment = (await db.Compressors.ToListAsync(stoppingToken))
            .GroupBy(c => c.EquipmentId).ToDictionary(g => g.Key, g => g.ToList());
        var equipments = await db.Equipments.ToListAsync(stoppingToken);

        foreach (var equipment in equipments)
        {
            if (!compressorsByEquipment.TryGetValue(equipment.Id, out var members) || members.Count == 0)
                continue;

            equipment.IsRunning = IsRunning(equipment, members, channelsByCompressor);
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    public static async Task UpdateAlarmAndCommunicationAsync(AppDbContext db, CancellationToken stoppingToken)
    {
        var channelsByCompressor = await LoadChannelsByCompressorAsync(db, stoppingToken);
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
        // 장비 (통신/경보 집계)
        //--------------------------------------------------------------------------------//
        foreach (var equipment in equipments)
        {
            if (!compressorsByEquipment.TryGetValue(equipment.Id, out var members) || members.Count == 0)
                continue;

            equipment.CommunicationStatus = AggregateCommunication(members.Select(c => c.CommunicationStatus));
            equipment.AlarmStatus = AggregateAlarm(members.Select(c => c.AlarmStatus));
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static async Task<Dictionary<int, List<CompressorSensorCurrent>>> LoadChannelsByCompressorAsync(
        AppDbContext db, CancellationToken stoppingToken) =>
        (await db.CompressorSensorCurrents.ToListAsync(stoppingToken))
            .GroupBy(s => s.CompressorId)
            .ToDictionary(g => g.Key, g => g.ToList());

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
