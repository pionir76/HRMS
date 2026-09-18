using HRMS.Common;
using System.Globalization;
using HRMS.Infrastructure;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.OperationReport.Models;
// "Equipment"가 클래스명이자 형제 네임스페이스(HRMS.Modules.Equipment)라 컴파일러가 헷갈려해서 별칭을 둔다.
using EquipmentEntity = HRMS.Modules.Equipment.Models.Equipment;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.OperationReport;

//--------------------------------------------------------------------------------//
// 운전일지 자동 기록. 매일 한국시간 09:00/13:00/16:00/21:00, 총 4번 실행되며 우선순위대로 판정한다
// (자세한 규칙은 README "항목별 자동화 방식" 참고):
//
//   1) 장비가 운영 중이 아니면(통신 오류/미사용/폐기 등) 모든 항목 "/" — 나머지 규칙보다 우선.
//   2) 운전 중이 아니면(Equipment.IsRunning=false), 채널 자동측정/장비설정 랜덤값 항목만 "/".
//   3) 채널 자동측정 5개: 그 시각의 압축기 채널값으로 덮어쓴다.
//   4) 장비설정 기반 랜덤값 5개: 그 장비가 "사용함"이면 min~max 사이 랜덤값(소수점 1자리),
//      "사용 안 함"이거나 min/max 미설정이면 "/".
//   5) 사용자 직접입력 6개: 빈 칸일 때만 직전 값을 이어받는다(날짜 경계도 넘음). 단 1)에 해당하면
//      이어받기 대신 "/"로 덮어쓴다.
//
// 운전일지는 절대 누락되면 안 되므로, 전 장비에 대해 매번 로그가 없으면 새로 만든다
// (점검일지의 "사용자가 최초 1회 작성해야 한다" 정책과 정반대).
//--------------------------------------------------------------------------------//
public class OperationAutoFillService(
    IServiceScopeFactory scopeFactory,
    ILogger<OperationAutoFillService> logger) : BackgroundService
{
    private static readonly TimeSpan[] TriggerTimes =
        [TimeSpan.FromHours(9), TimeSpan.FromHours(13), TimeSpan.FromHours(16), TimeSpan.FromHours(21)];

    //--------------------------------------------------------------------------------//
    // 운전일지에서 "운영 중이 아님"으로 보는 장비 상태(통신오류는 CommunicationStatus로 별도 판정).
    // **주의(2026-09-17)**: 수집 기준은 "운영 상태만 수집"으로 바뀌었지만(CompressorPollingService.
    // CollectionTargets), 운전일지는 예외가 될 수 있어 세부 사양이 미정이라 이 목록은 아직 옛 기준
    // 그대로 둔다. 그래서 수리중/점검중/철거예정/기타 장비는 수집이 안 되는데도 여기선 운영 중으로
    // 판정된다 — 사양이 정해지면 같이 고칠 것(Modules/OperationReport/README.md).
    //--------------------------------------------------------------------------------//
    private static readonly EquipmentStatus[] NonOperationalStatuses =
        [EquipmentStatus.미운영, EquipmentStatus.철거, EquipmentStatus.사용중지];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var nowKst = nowUtc.ToOffset(KoreanTime.Offset);
            var (nextTriggerKst, slotIndex) = GetNextTrigger(nowKst);
            var delay = nextTriggerKst - nowUtc;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                await RunOnceAsync(DateOnly.FromDateTime(nextTriggerKst.Date), slotIndex, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "운전일지 자동 기록 중 오류 발생");
            }
        }
    }

    //--------------------------------------------------------------------------------//
    // 지금(KST) 이후 가장 가까운 트리거 시각과 그 슬롯 인덱스(0=9시 ~ 3=21시)를 계산한다.
    // 오늘 남은 트리거가 없으면 내일 09:00(슬롯 0)이다.
    //--------------------------------------------------------------------------------//
    private static (DateTimeOffset TriggerKst, int SlotIndex) GetNextTrigger(DateTimeOffset nowKst)
    {
        var todayMidnight = new DateTimeOffset(nowKst.Date, KoreanTime.Offset);
        for (int i = 0; i < TriggerTimes.Length; i++)
        {
            var candidate = todayMidnight + TriggerTimes[i];
            if (candidate > nowKst)
                return (candidate, i);
        }
        return (todayMidnight.AddDays(1) + TriggerTimes[0], 0);
    }

    private async Task RunOnceAsync(DateOnly today, int slotIndex, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var equipments = await db.Equipments.ToListAsync(stoppingToken);
        var compressorsByEquipment = (await db.Compressors.ToListAsync(stoppingToken))
            .GroupBy(c => c.EquipmentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SequenceNo).ToList());
        var sensorsByCompressor = (await db.CompressorSensorCurrents.ToListAsync(stoppingToken))
            .GroupBy(s => s.CompressorId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.ChannelNo, s => s.Value));

        foreach (var equipment in equipments)
        {
            var log = await db.OperationLogs
                .Include(l => l.Items)
                .Include(l => l.References)
                .FirstOrDefaultAsync(l => l.EquipmentId == equipment.Id && l.Date == today, stoppingToken);

            if (log is null)
            {
                log = new OperationLog
                {
                    EquipmentId = equipment.Id, Date = today,
                    CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                };
                db.OperationLogs.Add(log);
            }

            compressorsByEquipment.TryGetValue(equipment.Id, out var compressors);
            compressors ??= [];

            bool operational = IsOperational(equipment);
            bool running = equipment.IsRunning; // EquipmentStatusAggregator가 이미 계산해둔 값을 그대로 쓴다

            //--------------------------------------------------------------------------------//
            // 1) 채널 자동측정 5개 — 운영 중 + 운전 중일 때만 채널값, 아니면 "/". 이 시각(slotIndex)
            //    칸을 항상 덮어쓴다.
            //--------------------------------------------------------------------------------//
            foreach (var autoItem in AutoMeasuredItemCatalog.Items)
            {
                if (autoItem.PerCompressor)
                {
                    foreach (var compressor in compressors)
                    {
                        var item = GetOrAddItem(log, autoItem.ItemKey, compressor.Id);
                        var value = !operational || !running
                            ? "/"
                            : ReadChannelValue(compressor, autoItem.ChannelNo, sensorsByCompressor);
                        SetSlot(item, slotIndex, value);
                    }
                }
                else if (compressors.Count > 0)
                {
                    var representative = compressors[0]; // SequenceNo=1(가장 작은 순번)
                    var item = GetOrAddItem(log, autoItem.ItemKey, null);
                    var value = !operational || !running
                        ? "/"
                        : ReadChannelValue(representative, autoItem.ChannelNo, sensorsByCompressor);
                    SetSlot(item, slotIndex, value);
                }
            }

            //--------------------------------------------------------------------------------//
            // 2) 장비설정 기반 랜덤값 5개 — 운영 중 + 운전 중 + "사용함" + min/max 설정됨일 때만
            //    랜덤값, 아니면 "/". 채널을 안 읽고 Equipment 속성만 본다(압축기 무관, 장비당 1행).
            //--------------------------------------------------------------------------------//
            foreach (var rangeItem in EquipmentRangeItemCatalog.Items)
            {
                var item = GetOrAddItem(log, rangeItem.ItemKey, null);
                var value = !operational || !running || !rangeItem.Has(equipment)
                    ? "/"
                    : GenerateRandomInRange(rangeItem.Min(equipment), rangeItem.Max(equipment)) ?? "/";
                SetSlot(item, slotIndex, value);
            }

            //--------------------------------------------------------------------------------//
            // 3) 사용자 직접입력 6개 — 운영 중이면 기존처럼 빈 칸만 이어받고(날짜 경계도 넘음),
            //    운영 중이 아니면 이어받기 대신 이 시각 칸을 "/"로 강제한다(이미 있는 항목 행만).
            //--------------------------------------------------------------------------------//
            if (operational)
            {
                var previousLog = await db.OperationLogs
                    .Include(l => l.Items)
                    .Include(l => l.References)
                    .FirstOrDefaultAsync(l => l.EquipmentId == equipment.Id && l.Date == today.AddDays(-1), stoppingToken);

                if (previousLog is not null)
                    CarryOverFromPreviousDay(log, previousLog);

                for (int s = 1; s <= slotIndex; s++)
                    CarryWithinDay(log, s);
            }
            else
            {
                foreach (var item in log.Items)
                {
                    if (AutoMeasuredItemCatalog.IsAutoMeasured(item.ItemKey) || EquipmentRangeItemCatalog.IsRangeGenerated(item.ItemKey))
                        continue; // 위 1)/2)에서 이미 처리됨

                    SetSlot(item, slotIndex, "/");
                }
            }

            await db.SaveChangesAsync(stoppingToken);
        }
    }

    private static bool IsOperational(EquipmentEntity equipment) =>
        !NonOperationalStatuses.Contains(equipment.Status) && equipment.CommunicationStatus == CommunicationStatus.연결됨;

    //--------------------------------------------------------------------------------//
    // min~max 사이 랜덤값을 만들어 소수점 1자리로 반올림/패딩한다(단순 반올림 — raw 스케일 변환 아님).
    // min/max 중 하나라도 없으면 null(호출부에서 "/"로 처리). 0은 유효한 값이라 그대로 0.0이 된다.
    //--------------------------------------------------------------------------------//
    private static string? GenerateRandomInRange(decimal? min, decimal? max)
    {
        if (min is not { } lo || max is not { } hi)
            return null;

        if (lo > hi)
            (lo, hi) = (hi, lo);

        var raw = lo + (decimal)Random.Shared.NextDouble() * (hi - lo);
        return Math.Round(raw, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static void CarryOverFromPreviousDay(OperationLog today, OperationLog previous)
    {
        foreach (var previousItem in previous.Items)
        {
            if (AutoMeasuredItemCatalog.IsAutoMeasured(previousItem.ItemKey) || EquipmentRangeItemCatalog.IsRangeGenerated(previousItem.ItemKey))
                continue;
            if (previousItem.Time2100 is null)
                continue;

            var todayItem = GetOrAddItem(today, previousItem.ItemKey, previousItem.CompressorId);
            todayItem.Time0900 ??= previousItem.Time2100;
        }

        foreach (var previousReference in previous.References)
        {
            if (today.References.Any(r => r.ItemKey == previousReference.ItemKey))
                continue;

            today.References.Add(new OperationReferenceValue
            {
                ItemKey = previousReference.ItemKey,
                ReferenceText = previousReference.ReferenceText
            });
        }
    }

    private static void CarryWithinDay(OperationLog log, int slot)
    {
        foreach (var item in log.Items)
        {
            if (AutoMeasuredItemCatalog.IsAutoMeasured(item.ItemKey) || EquipmentRangeItemCatalog.IsRangeGenerated(item.ItemKey))
                continue;

            if (GetSlot(item, slot) is not null)
                continue;

            var previous = GetSlot(item, slot - 1);
            if (previous is not null)
                SetSlot(item, slot, previous);
        }
    }

    //--------------------------------------------------------------------------------//
    // 압축기가 통신 불가 상태이거나 그 채널값을 읽어온 적이 없으면 "/"(통신오류/기록불가)로 기입한다.
    //--------------------------------------------------------------------------------//
    private static string ReadChannelValue(
        Compressor compressor, ChannelNo channelNo,
        Dictionary<int, Dictionary<ChannelNo, short>> sensorsByCompressor)
    {
        if (compressor.CommunicationStatus != CommunicationStatus.연결됨)
            return "/";

        if (sensorsByCompressor.TryGetValue(compressor.Id, out var channels) && channels.TryGetValue(channelNo, out var value))
            return value.ToString();

        return "/";
    }

    private static OperationItemValue GetOrAddItem(OperationLog log, string itemKey, int? compressorId)
    {
        var existing = log.Items.FirstOrDefault(i => i.ItemKey == itemKey && i.CompressorId == compressorId);
        if (existing is not null)
            return existing;

        var created = new OperationItemValue { ItemKey = itemKey, CompressorId = compressorId };
        log.Items.Add(created);
        return created;
    }

    private static string? GetSlot(OperationItemValue item, int slot) => slot switch
    {
        0 => item.Time0900, 1 => item.Time1300, 2 => item.Time1600, 3 => item.Time2100,
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };

    private static void SetSlot(OperationItemValue item, int slot, string? value)
    {
        switch (slot)
        {
            case 0: item.Time0900 = value; break;
            case 1: item.Time1300 = value; break;
            case 2: item.Time1600 = value; break;
            case 3: item.Time2100 = value; break;
        }
    }
}
