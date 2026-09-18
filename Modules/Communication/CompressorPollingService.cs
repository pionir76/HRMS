using HRMS.Common;
using HRMS.Infrastructure;
using HRMS.Modules.Alarm;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Communication.Protocol;
using HRMS.Modules.Equipment;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.EntityFrameworkCore;

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
    public const int PollIntervalMs = 3000;

    //--------------------------------------------------------------------------------//
    // 마지막으로 폴링 사이클이 끝까지 성공한 시각(UTC ticks, 아직 한 번도 없으면 0).
    // GET /api/system/status의 api.status 판정(수집 서비스가 멈췄는지)에만 쓴다.
    // 폴링 루프와 API 요청 스레드가 동시에 접근하므로 Interlocked로 읽고 쓴다.
    //--------------------------------------------------------------------------------//
    private static long lastCycleCompletedTicks;

    public static DateTimeOffset? LastCycleCompletedAt
    {
        get
        {
            long ticks = Interlocked.Read(ref lastCycleCompletedTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    //--------------------------------------------------------------------------------//
    // 통신 장애가 이 시간 이상 계속 지속되어야 HasCommunicationAlarm을 켠다 — 압축기별 설정이 아니라
    // 통신 모듈 공통값 하나로 관리한다(사용자 결정). 짧게 끊겼다 붙는 불안정한 연결까지 매번 경보로
    // 잡으면 이벤트가 너무 잦아지는 걸 막기 위한 디바운스 목적이다.
    //--------------------------------------------------------------------------------//
    private static readonly TimeSpan CommunicationFailureAlarmDelay = TimeSpan.FromSeconds(30);

    //--------------------------------------------------------------------------------//
    // 수집 대상 압축기 조회. **장비 상태가 `운영`인 장비의 압축기만** 수집한다(overview.md 4.1,
    // 사용자 결정 2026-09-17 — 운영이 아닌 장비는 수집도 노출도 하지 않는다). 테스트 모드가 아니면
    // IP가 없는 압축기는 통신할 수 없으므로 뺀다.
    // 폴링 루프와 GET /api/system/status(collection.compressorCount)가 같은 기준을 쓰도록 공개한다.
    //--------------------------------------------------------------------------------//
    public static IQueryable<Compressor> CollectionTargets(AppDbContext db, bool testMode)
    {
        var query = db.Compressors
            .Join(db.Equipments, c => c.EquipmentId, e => e.Id, (c, e) => new { Compressor = c, e.Status })
            .Where(x => x.Status == EquipmentStatus.운영)
            .Select(x => x.Compressor);

        return testMode ? query : query.Where(c => c.IpAddress != null);
    }

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

        //--------------------------------------------------------------------------------//
        // 압축기 목록을 DB에서 새로 읽는다. (이 단계에서는 아직 통신은 안 하고, DB에 손대지 않고 메모리에만 올린다.)
        //--------------------------------------------------------------------------------//
        var compressors = await CollectionTargets(db, testMode).ToListAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        // 압축기별로 독립적으로 통신하여, 한 대의 장애/지연이 다른 압축기 폴링에 영향을 주지 않도록 한다.
        // (DbContext는 스레드에 안전하지 않으므로 이 단계에서는 DB에 손대지 않고, 결과만 메모리에 모은다.)
        //--------------------------------------------------------------------------------//
        var results = await Task.WhenAll(compressors.Select(async c =>
        {
            bool ok;
            short[] values;

            //--------------------------------------------------------------------------------//
            // 테스트 모드에서는 실제 TCP 통신 없이 전 압축기가 정상 통신하는 것으로 가정하고,
            // 시간에 따라 완만하게 변하는 모의값을 채운다(GenerateTestValues 주석 참고).
            //--------------------------------------------------------------------------------//
            if (testMode)
            {
                ok = true;
                values = GenerateTestValues(c.Id);
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
        var now = DateTimeOffset.UtcNow;
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

            //--------------------------------------------------------------------------------//
            // 통신 장애 경보(AlarmStatus와 별도). 성공하면 즉시 해제하고, 실패하면 처음 끊긴
            // 시각만 기록해뒀다가 CommunicationFailureAlarmDelay 이상 계속 끊겨있을 때만 켠다.
            //--------------------------------------------------------------------------------//
            if (ok)
            {
                compressor.DisconnectedSince = null;
                compressor.HasCommunicationAlarm = false;
            }
            else
            {
                compressor.DisconnectedSince ??= now;
                bool wasAlarm = compressor.HasCommunicationAlarm;
                compressor.HasCommunicationAlarm = now - compressor.DisconnectedSince >= CommunicationFailureAlarmDelay;

                //--------------------------------------------------------------------------------//
                // 통신 장애 경보는 켜지는 순간만 기록한다(사용자 결정) — 복구는 기록하지 않고,
                // 연결됨/끊김/재접속중 상태 전이 자체도 너무 잦아서 기록하지 않는다.
                //--------------------------------------------------------------------------------//
                if (!wasAlarm && compressor.HasCommunicationAlarm)
                    await LogCommunicationAlarmAsync(db, compressor.Id);
            }
        }

        await UpdateCurrentValuesAsync(db, results.Where(r => r.Ok), stoppingToken);

        //--------------------------------------------------------------------------------//
        // 통신이 끊긴 압축기는 채널값을 건드리지 않아 직전 성공값이 그대로 유지된다(사양).
        // 단 그 값이 어제 이전에 측정된 것이면, 즉 "하루가 시작되는데 여전히 끊긴 상태"라면
        // 전날 값을 계속 끌고 가지 않고 0으로 초기화한다(사용자 결정 2026-09-16).
        //--------------------------------------------------------------------------------//
        await ResetStaleValuesAtDayStartAsync(db, results.Where(r => !r.Ok).Select(r => r.Id), stoppingToken);

        //--------------------------------------------------------------------------------//
        // 갱신된 통신상태·채널값을 저장하고, 경보 판정보다 먼저 장비 운전 여부를 정한다
        // (경보 판정이 이번 사이클의 운전 여부를 봐야 하므로 — EvaluateAlarmsAsync 참고).
        //--------------------------------------------------------------------------------//
        await db.SaveChangesAsync(stoppingToken);
        await EquipmentStatusAggregator.UpdateRunningAsync(db, stoppingToken);

        await EvaluateAlarmsAsync(db, compressors, results.Where(r => r.Ok).Select(r => r.Id), stoppingToken);
        await db.SaveChangesAsync(stoppingToken);

        //--------------------------------------------------------------------------------//
        //  압축기/장비 단위 경보·통신 상태 집계
        //--------------------------------------------------------------------------------//
        await EquipmentStatusAggregator.UpdateAlarmAndCommunicationAsync(db, stoppingToken);

        Interlocked.Exchange(ref lastCycleCompletedTicks, DateTimeOffset.UtcNow.UtcTicks);
    }

    //--------------------------------------------------------------------------------//
    // 테스트 모드 채널값 생성 (raw int16, -200 ~ 1200).
    //
    // 예전에는 매 폴링마다 독립 난수를 뽑았는데(`Random.Shared.Next(-200, 1201)`), 그러면 값이
    // 3초마다 완전히 튀어서 **"30초 연속 범위 이탈"이 사실상 발생하지 않았다** — 이탈 확률이
    // 28.6%라 10회 연속 이탈 확률이 0.286^10 ≈ 3e-6. 그래서 경보가 거의 확정되지 않아
    // 프론트가 경보/이벤트 화면을 검증할 수 없었다(2026-09-16 확인).
    //
    // 실제 센서값은 연속적으로 변하므로, 시간에 따라 완만하게 움직이는 사인파 + 약한 지터로
    // 바꿨다. 이탈 구간이 수십 분 단위로 유지되어 경보 발생(30초 지연)과 해제가 정상적으로
    // 확정된다. 트렌드 그래프도 난수 노이즈가 아니라 실제 설비처럼 보인다.
    //
    //  - 위상을 압축기·채널마다 다르게 줘서 전 채널이 동시에 경보로 몰리지 않게 한다.
    //  - **채널 64개 중 1개만** 진폭을 키워 주기적으로 범위(0~1000)를 벗어나고, 나머지는
    //    범위 안(185~815)에서만 움직인다. 실제 현장처럼 "대부분 정상, 일부만 이상"을 만들면서
    //    이벤트 물량도 통제하기 위함이다(전 채널이 이탈하면 하루 수만 건이 쌓인다).
    //--------------------------------------------------------------------------------//
    private static readonly TimeSpan TestCyclePeriod = TimeSpan.FromHours(6);

    private static short[] GenerateTestValues(int compressorId) =>
        [.. Enumerable.Range(0, 7).Select(channelIndex =>
        {
            int channelSeed = compressorId * 7 + channelIndex;
            double amplitude = channelSeed % 64 == 0 ? 700 : 300; // 700이면 범위를 벗어난다
            double phase = channelSeed * 0.37;
            double angle = 2 * Math.PI * DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TestCyclePeriod.TotalSeconds + phase;
            double value = 500 + amplitude * Math.Sin(angle) + Random.Shared.Next(-15, 16);
            return (short)Math.Clamp(value, -200, 1200);
        })];

    //--------------------------------------------------------------------------------//
    // 채널별 경보 판정. 값 저장과 장비 운전 판정(UpdateRunningAsync)이 끝난 뒤에 호출된다.
    //
    // **장비가 운전 중이 아니면 경보 판정을 하지 않는다**(사용자 결정 2026-09-17). 그 장비에 속한
    // 모든 압축기의 모든 채널을 무조건 `정상`으로 두고(대기 타이머도 초기화), 경보 이벤트도 남기지
    // 않는다 — 압축기 1번이 경보발생 상태였더라도 장비가 정지하면 즉시 정상이 된다. 발생 이벤트만
    // 있고 해제 이벤트가 없는 경보가 생길 수 있는데, 이는 사양상 의도된 것이다.
    // 설정상 경보비활성화인 채널도 비운전 중에는 `정상`으로 표시된다("무조건 정상").
    //
    // 운전 중인 장비는 기존대로 판정하되, 이번 사이클에 통신에 실패한 압축기는 값이 갱신되지 않았으므로
    // 판정하지 않고 직전 상태를 유지한다(예전과 동일한 동작).
    // 통신 장애 경보(HasCommunicationAlarm)는 이 규칙과 무관하다 — 통신이 끊기면 운전 판정도 정지가
    // 되므로, 여기에 묶으면 통신 장애 경보가 절대 안 뜨게 된다.
    //--------------------------------------------------------------------------------//
    private static async Task EvaluateAlarmsAsync(
        AppDbContext db, List<Compressor> compressors, IEnumerable<int> successfulIds, CancellationToken stoppingToken)
    {
        var compressorsById = compressors.ToDictionary(c => c.Id);
        var targetIds = compressorsById.Keys.ToList();
        var succeeded = successfulIds.ToHashSet();

        var runningEquipmentIds = (await db.Equipments
            .Where(e => e.IsRunning)
            .Select(e => e.Id)
            .ToListAsync(stoppingToken)).ToHashSet();

        var currents = await db.CompressorSensorCurrents
            .Where(s => targetIds.Contains(s.CompressorId))
            .ToListAsync(stoppingToken);

        var settings = await db.CompressorChannelSettings
            .Where(s => targetIds.Contains(s.CompressorId))
            .ToDictionaryAsync(s => (s.CompressorId, s.ChannelNo), stoppingToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var current in currents)
        {
            if (!runningEquipmentIds.Contains(compressorsById[current.CompressorId].EquipmentId))
            {
                current.AlarmStatus = AlarmStatus.정상;
                current.PendingSince = null;
                continue;
            }

            if (!succeeded.Contains(current.CompressorId)) continue;
            if (!settings.TryGetValue((current.CompressorId, current.ChannelNo), out var setting)) continue;

            var previousAlarmStatus = current.AlarmStatus;
            AlarmEvaluator.Evaluate(current, setting, now);
            await LogAlarmTransitionIfNeededAsync(db, current.CompressorId, current.ChannelNo, setting,
                previousAlarmStatus, current.AlarmStatus, current.Value);
        }
    }

    //--------------------------------------------------------------------------------//
    // CH01~CH07 7개 채널의 최신값을 갱신한다(경보 판정은 EvaluateAlarmsAsync에서 따로 한다).
    // 채널 사용 여부(Enabled)와 무관하게 원시값은 항상 저장한다
    // (overview.md 4.6의 "경보를 꺼도 데이터 수집은 계속한다" 원칙과 동일하게 적용).
    // CompressorSensorCurrent는 압축기·채널당 정확히 1행만 유지하는 최신값 테이블이라, 이 메서드는
    // 누적 INSERT가 아니라 있으면 갱신(UPDATE)·없으면 최초 1회 생성(INSERT)하는 UPSERT로 동작한다.
    //--------------------------------------------------------------------------------//
    // 통신이 끊긴 압축기의 "날짜가 바뀌었는데도 여전히 끊긴" 경우를 처리한다.
    //
    // 기본 사양은 "끊기면 직전 성공값을 그대로 유지"다(값을 아예 갱신하지 않으므로 자연히 유지됨).
    // 그런데 전날 값을 다음 날까지 계속 끌고 가면 "어제 값으로 오늘 하루가 채워지는" 문제가 생겨서,
    // **마지막 측정이 오늘(한국 시간) 00:00 이전이면 전 채널을 0으로 초기화**한다(사용자 결정).
    // 통신이 복구되면 다음 성공 폴링에서 실제 값으로 덮인다.
    //
    // MeasuredAt은 일부러 갱신하지 않는다 — 실제로 측정된 시각이 아니고, 이 값을 그대로 둬야
    // 다음 사이클에도 같은 판정이 나와 0이 유지된다(이미 0이면 EF가 변경 없음으로 처리).
    //--------------------------------------------------------------------------------//
    private static async Task ResetStaleValuesAtDayStartAsync(
        AppDbContext db, IEnumerable<int> failedCompressorIds, CancellationToken stoppingToken)
    {
        var failedIds = failedCompressorIds.ToList();
        if (failedIds.Count == 0) return;

        var kstToday = DateTimeOffset.UtcNow.ToOffset(KoreanTime.Offset).Date;
        var todayStartUtc = new DateTimeOffset(kstToday, KoreanTime.Offset).ToUniversalTime();

        var stale = await db.CompressorSensorCurrents
            .Where(s => failedIds.Contains(s.CompressorId) && s.MeasuredAt < todayStartUtc && s.Value != 0)
            .ToListAsync(stoppingToken);

        foreach (var current in stale)
            current.Value = 0;
    }

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
            }
        }
    }

    //--------------------------------------------------------------------------------//
    // 경보 "발생"(경보발생대기 → 경보발생)과 "해제"(정상복귀대기 → 정상) 확정 순간만 기록한다
    // (사용자 결정). 중간 대기 상태(경보발생대기/정상복귀대기) 자체는 기록하지 않는다.
    //--------------------------------------------------------------------------------//
    private static async Task LogAlarmTransitionIfNeededAsync(
        AppDbContext db, int compressorId, ChannelNo channelNo, CompressorChannelSetting setting,
        AlarmStatus previous, AlarmStatus current, short? value)
    {
        //--------------------------------------------------------------------------------//
        // "발생"은 경보발생대기에서 올라온 것만 인정한다(2026-09-16 수정).
        // 상태 머신에는 `경보발생 → 정상복귀대기 → 경보발생` 경로가 있는데(해제 지연 대기 중
        // 값이 다시 범위를 벗어나면 즉시 경보발생으로 복귀), 이건 해제된 적 없는 **같은 경보의
        // 연장**이라 새 이벤트로 기록하면 안 된다. 이전 조건(previous != 경보발생)은 이 경로까지
        // 발생으로 잡아서, 해제 지연이 길고 값이 경계에서 흔들리는 채널에서 같은 경보가 수십 초
        // 간격으로 수천 건 쌓였다(장비 39 CH05: 하루 이탈 1,557건 / 복귀 0건).
        // 경보발생 진입 경로는 경보발생대기와 정상복귀대기 둘뿐이라 이 조건으로 충분하다.
        //--------------------------------------------------------------------------------//
        bool occurred = previous == AlarmStatus.경보발생대기 && current == AlarmStatus.경보발생;
        bool cleared = previous == AlarmStatus.정상복귀대기 && current == AlarmStatus.정상;
        if (!occurred && !cleared) return;

        var info = await GetCompressorContextAsync(db, compressorId);
        var message = occurred
            ? $"{info.Region} {info.BuildingName}의 {info.EquipmentName}의 압축기 {info.SequenceNo}번 {setting.ChannelName}값이 범위를 벗어났습니다."
            : $"{info.Region} {info.BuildingName}의 {info.EquipmentName}의 압축기 {info.SequenceNo}번 {setting.ChannelName}값이 정상 범위로 복귀했습니다.";

        // 측정값·임계값·단위·소수점을 함께 스냅샷으로 남긴다 — 자료 조회 화면의 이벤트 상세 팝업용.
        await EventLogger.LogAlarmAsync(db, message,
            equipmentId: info.EquipmentId, compressorId: compressorId, channelNo: channelNo,
            value: value, setting: setting);
    }

    //--------------------------------------------------------------------------------//
    // 통신 장애 경보는 켜지는 순간만 기록한다(복구는 기록 안 함, 사용자 결정).
    //--------------------------------------------------------------------------------//
    private static async Task LogCommunicationAlarmAsync(AppDbContext db, int compressorId)
    {
        var info = await GetCompressorContextAsync(db, compressorId);
        var message = $"{info.Region} {info.BuildingName}의 {info.EquipmentName}의 압축기 {info.SequenceNo}번이 통신 장애 상태입니다.";

        await EventLogger.LogAsync(db, EventLogCategory.Communication, message,
            equipmentId: info.EquipmentId, compressorId: compressorId);
    }

    //--------------------------------------------------------------------------------//
    // 경보/통신장애 메시지에 필요한 장비 정보를 조회한다. 전이가 실제로 일어날 때만 호출되는
    // 드문 경로라, 매 폴링 사이클마다 미리 로드해두지 않고 필요한 순간에만 조회한다.
    //--------------------------------------------------------------------------------//
    private static Task<CompressorContext> GetCompressorContextAsync(AppDbContext db, int compressorId) =>
        (from c in db.Compressors
         join e in db.Equipments on c.EquipmentId equals e.Id
         where c.Id == compressorId
         select new CompressorContext(e.Id, e.Region, e.BuildingName, e.Name, c.SequenceNo))
        .FirstAsync();

    private record CompressorContext(int EquipmentId, string Region, string BuildingName, string EquipmentName, int SequenceNo);
}
