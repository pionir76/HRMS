using HRMS.Infrastructure;
using HRMS.Modules.Alarm;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Communication.Protocol;
using HRMS.Modules.Equipment;
using HRMS.Modules.Equipment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRMS.Modules.Communication;

//--------------------------------------------------------------------------------//
// 앱 시작 시 자동 등록되어 백그라운드에서 계속 도는 폴링 루프(Program.cs의 AddHostedService).
// 3초마다: 활성 압축기 목록을 DB에서 새로 읽고 → 전부 동시에 TCP로 값을 읽어온 뒤
// → 통신상태·채널값·경보상태를 갱신하고 → 장비 단위로 집계한다.
//--------------------------------------------------------------------------------//
public class CompressorPollingService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CompressorPollingService> logger) : BackgroundService
{
    //--------------------------------------------------------------------------------//
    // overview.md 8.1의 시스템 공통 Polling Interval과 동일하게 3초로 고정한다. 
    // (설정화면에서 바꾸는 기능은 아직 없다.)
    //--------------------------------------------------------------------------------//
    private const int PollIntervalMs = 3000; 

    // 이 상태의 장비에 속한 압축기는 수집 대상에서 제외한다 (overview.md 4.1).
    private static readonly EquipmentStatus[] ExcludedEquipmentStatuses =
        [EquipmentStatus.미운영, EquipmentStatus.철거, EquipmentStatus.사용중지];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                //--------------------------------------------------------------------------------//
                // 한 사이클 전체가 실패해도(예: DB 순단) 
                // 서비스 자체는 죽지 않고 다음 사이클을 계속 시도한다.
                //--------------------------------------------------------------------------------//
                logger.LogError(ex, "압축기 폴링 중 오류 발생");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken stoppingToken)
    {
        //--------------------------------------------------------------------------------//
        // 테스트 모드: 실제 TCP 통신 없이 전 압축기가 정상 통신하는 것으로 가정하고 랜덤값을 채운다.
        // appsettings.*.json의 "Communication:TestMode"를 껐다 켰다 하고 앱을 재시작하면 된다.
        //--------------------------------------------------------------------------------//
        bool testMode = configuration.GetValue("Communication:TestMode", false);

        //--------------------------------------------------------------------------------//
        // BackgroundService는 싱글턴이라 DbContext(스코프드)를 직접 주입받을 수 없어서,
        // 사이클마다 스코프를 새로 만들어 그 안에서 DbContext를 가져온다.
        //--------------------------------------------------------------------------------//
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var query = db.Compressors
            .Join(db.Equipments, c => c.EquipmentId, e => e.Id, (c, e) => new { Compressor = c, e.Status })
            .Where(x => !ExcludedEquipmentStatuses.Contains(x.Status));

        // 테스트 모드에서는 IP 없는 압축기도 포함
        if (!testMode)
            query = query.Where(x => x.Compressor.IpAddress != null); 

        //--------------------------------------------------------------------------------//
        // 압축기 목록을 DB에서 새로 읽는다. (이 단계에서는 아직 통신은 안 하고, DB에 손대지 않고 메모리에만 올린다.)
        //--------------------------------------------------------------------------------//
        var compressors = await query.Select(x => x.Compressor).ToListAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        // 압축기별로 독립적으로 통신하여, 한 대의 장애/지연이 다른 압축기 폴링에 영향을 주지 않도록 한다.
        // (DbContext는 스레드에 안전하지 않으므로 이 단계에서는 DB에 손대지 않고, 결과만 메모리에 모은다.)
        //--------------------------------------------------------------------------------//
        var results = await Task.WhenAll(compressors.Select(async c =>
        {
            bool ok;
            short[] values;

            //--------------------------------------------------------------------------------//
            // 테스트 모드에서는 실제 TCP 통신 없이 전 압축기가 정상 통신하는 것으로 가정하고 랜덤값을 채운다.
            //--------------------------------------------------------------------------------//    
            if (testMode)
            {
                ok = true;
                values = GenerateTestValues();
            }

            //--------------------------------------------------------------------------------//
            // Not test mode: 실제 TCP 통신으로 CH01~CH07 7개 채널값을 읽어온다.
            //--------------------------------------------------------------------------------//
            else
            {
                try
                {
                    (ok, values, _) = await PcLinkClient.ReadChannelsAsync(c.IpAddress!);
                }
                catch
                {
                    ok = false;
                    values = [];
                }
            }
            return (c.Id, PreviousStatus: c.CommunicationStatus, Ok: ok, Values: values);
        }));

        //--------------------------------------------------------------------------------//    
        // 여기서부터는 단일 스레드로 DbContext를 순차 갱신하므로 동시성 문제 없음.
        // results는 압축기별로 (Id, 이전 통신상태, 통신성공여부, 읽어온 채널값) 튜플 배열
        // 비동기 통신으로 전체 압축기에서 읽어온 결과를 모은 뒤, 통신상태·채널값·경보상태를 갱신하고 장비 단위로 집계한다.
        //--------------------------------------------------------------------------------//    
        foreach (var (id, previousStatus, ok, _) in results)
        {
            var compressor = compressors.First(c => c.Id == id);

            //--------------------------------------------------------------------------------//    
            // 성공하면 무조건 연결됨. 실패는 직전이 연결됨이었으면(막 끊긴 상태) 재접속중,
            // 그 외(원래도 안 됐던 경우)는 끊김으로 본다.
            //--------------------------------------------------------------------------------//    
            compressor.CommunicationStatus = ok
                ? CommunicationStatus.연결됨
                : previousStatus == CommunicationStatus.연결됨
                    ? CommunicationStatus.재접속중
                    : CommunicationStatus.끊김;
        }

        await UpdateCurrentValuesAsync(db, results.Where(r => r.Ok), stoppingToken);

        //--------------------------------------------------------------------------------//
        // DB에 갱신된 통신상태·채널값·경보상태를 저장하고, 장비 단위로 집계한다.
        //--------------------------------------------------------------------------------//
        await db.SaveChangesAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        //  장비 상태 갱신
        //--------------------------------------------------------------------------------//
        await EquipmentStatusAggregator.UpdateAsync(db, stoppingToken);
    }

    //--------------------------------------------------------------------------------//    
    // 원시값(raw int16) 기준 -200 ~ 1200 범위로 생성한다. 기본 경보 상/하한(raw 0~1000)을
    // 넘나들게 해서 경보발생대기/경보발생/정상복귀대기 전이가 실제로 일어나는 걸 볼 수 있다.
    //--------------------------------------------------------------------------------//    
    private static short[] GenerateTestValues() =>
        [.. Enumerable.Range(0, 7).Select(_ => (short)Random.Shared.Next(-200, 1201))];

    //--------------------------------------------------------------------------------//    
    // CH01~CH07 7개 채널의 최신값을 갱신하고, 채널별 경보 상태까지 같이 판정한다.
    // 채널 사용 여부(Enabled)와 무관하게 원시값은 항상 저장한다
    // (overview.md 4.6의 "경보를 꺼도 데이터 수집은 계속한다" 원칙과 동일하게 적용. 경보 판정 자체는
    //  AlarmEvaluator가 Enabled/AlarmEnabled를 보고 알아서 "경보비활성화"로 처리한다).
    // CompressorSensorCurrent는 압축기·채널당 정확히 1행만 유지하는 최신값 테이블이라, 이 메서드는
    // 누적 INSERT가 아니라 있으면 갱신(UPDATE)·없으면 최초 1회 생성(INSERT)하는 UPSERT로 동작한다.
    //--------------------------------------------------------------------------------//    
    private static async Task UpdateCurrentValuesAsync(
        AppDbContext db,
        IEnumerable<(int Id, CommunicationStatus PreviousStatus, bool Ok, short[] Values)> successfulResults,
        CancellationToken stoppingToken)
    {
        var successfulIds = successfulResults.Select(r => r.Id).ToList();
        if (successfulIds.Count == 0) return;

        var existingCurrents = await db.CompressorSensorCurrents
            .Where(s => successfulIds.Contains(s.CompressorId))
            .ToDictionaryAsync(s => (s.CompressorId, s.ChannelNo), stoppingToken);

        var settings = await db.CompressorChannelSettings
            .Where(s => successfulIds.Contains(s.CompressorId))
            .ToDictionaryAsync(s => (s.CompressorId, s.ChannelNo), stoppingToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var r in successfulResults)
        {
            //--------------------------------------------------------------------------------//
            // 방어적 체크: 정상 응답이면 항상 9개(그중 앞 7개 사용)
            //--------------------------------------------------------------------------------//
            if (r.Values.Length < 7) continue; 

            for (int i = 0; i < 7; i++)
            {
                //--------------------------------------------------------------------------------//
                // PcLinkClient.ReadChannelsAsync에서 반환한 Values[0..6] = CH01..CH07 순서와 일치
                //--------------------------------------------------------------------------------//
                var channelNo = (ChannelNo)(i + 1); 

                if (!existingCurrents.TryGetValue((r.Id, channelNo), out var current))
                {
                    current = new CompressorSensorCurrent { CompressorId = r.Id, ChannelNo = channelNo };
                    
                    //--------------------------------------------------------------------------------//
                    // DB에 없으면 최초 1회 INSERT. 이후에는 UPDATE로 갱신
                    //--------------------------------------------------------------------------------//
                    db.CompressorSensorCurrents.Add(current);
                }

                current.Value = r.Values[i];
                current.MeasuredAt = now;

                if (settings.TryGetValue((r.Id, channelNo), out var setting))
                    AlarmEvaluator.Evaluate(current, setting, now);
            }
        }
    }
}
