using HRMS.Infrastructure;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Trend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRMS.Modules.Trend;

// 매 분 정각(초=0)에 압축기별 최신값(CompressorSensorCurrent)을 스냅샷 찍어 CompressorMeasurement에 누적 기록한다.
// CompressorPollingService와는 완전히 독립된 별도 루프다 — 직접 통신하지 않고, 이미 폴링이 채워둔
// 최신값 테이블을 그대로 복사만 한다 (overview.md 4.5: 1분 대표값 = 마지막값).
public class TrendRecordingService(IServiceScopeFactory scopeFactory, ILogger<TrendRecordingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // "60초마다"가 아니라 다음 정각까지 남은 시간을 매번 다시 계산한다 — 그래야
            // 10:00:00, 10:01:00처럼 정확한 정각에 기록되고, 오차가 누적되지 않는다.
            // UTC 기준으로 계산한다 — Npgsql은 timestamptz 컬럼에 offset=0(UTC)인 DateTimeOffset만 받는다.
            var now = DateTimeOffset.UtcNow;
            var nextMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero)
                .AddMinutes(1);
            var delay = nextMinute - now;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                await RecordOnceAsync(nextMinute, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "트렌드 기록 중 오류 발생");
            }
        }
    }

    private async Task RecordOnceAsync(DateTimeOffset measuredAt, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 압축기 개별이 아니라 소속 장비의 공식 RunningStatus를 그대로 기록한다(사용자 결정).
        var compressors = await (
            from c in db.Compressors
            join e in db.Equipments on c.EquipmentId equals e.Id
            select new { c.Id, c.AlarmStatus, c.CommunicationStatus, e.RunningStatus }
        ).ToListAsync(stoppingToken);

        var currentsByCompressor = (await db.CompressorSensorCurrents.ToListAsync(stoppingToken))
            .GroupBy(s => s.CompressorId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.ChannelNo, s => s.Value));

        // 통신 이력이 한 번도 없는 압축기도 채널값 NULL 상태로 행을 만든다(사용자 결정).
        foreach (var c in compressors)
        {
            currentsByCompressor.TryGetValue(c.Id, out var channels);

            db.CompressorMeasurements.Add(new CompressorMeasurement
            {
                CompressorId = c.Id,
                MeasuredAt = measuredAt,
                Ch01 = GetValue(channels, ChannelNo.CH01),
                Ch02 = GetValue(channels, ChannelNo.CH02),
                Ch03 = GetValue(channels, ChannelNo.CH03),
                Ch04 = GetValue(channels, ChannelNo.CH04),
                Ch05 = GetValue(channels, ChannelNo.CH05),
                Ch06 = GetValue(channels, ChannelNo.CH06),
                Ch07 = GetValue(channels, ChannelNo.CH07),
                RunningStatus = c.RunningStatus,
                AlarmStatus = c.AlarmStatus,
                CommunicationStatus = c.CommunicationStatus
            });
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static decimal? GetValue(Dictionary<ChannelNo, decimal>? channels, ChannelNo channelNo) =>
        channels != null && channels.TryGetValue(channelNo, out var value) ? value : null;
}
