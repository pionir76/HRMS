using HRMS.Common;
using HRMS.Infrastructure;
using HRMS.Modules.InspectionReport.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.InspectionReport;

//--------------------------------------------------------------------------------//
// 점검일지 자동 기록. 사용자가 특정 요일의 점검 결과를 기록하지 않으면, 그 전날 값을 그대로
// 이어서 채운다(사용자 결정 — "매번 확인 안 해도 되는 사항은 어제 기록을 유지"). 주 경계도
// 예외가 아니다 — 일요일이 되면 지난주 토요일 값을 그대로 이어받아 새 주를 시작한다. 그래서
// 한 번 기록이 시작된 항목은 사용자가 몇 달을 손대지 않아도 계속 이어진다(사용자 요구사항 —
// "신경 쓸 게 없어야 한다"). 예를 들어 일요일에 "X"(고장)로 기록했는데 그 뒤로 아무도 손대지
// 않으면, 수리돼서 값이 바뀌기 전까지 그 다음 주, 다다음 주까지도 계속 "X"로 자동 채워진다.
//
// 매일 한국시간 00:00에 한 번 실행되며, 그날까지 비어있는 칸만 채운다 — 사용자가 이미 기록한
// 값은 절대 덮어쓰지 않는다. 결재가 진행 중이더라도 이 자동 기록은 예외적으로 계속 동작한다
// (api-manual.md 10번 "결재 시스템" 참고 — InspectionLogsController의 수정 잠금은 PUT 컨트롤러에만
// 걸려 있어서, 이 백그라운드 서비스는 그 잠금과 무관하게 DbContext에 직접 쓴다).
//--------------------------------------------------------------------------------//
public class InspectionAutoFillService(
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionAutoFillService> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var nowKst = nowUtc.ToOffset(KoreanTime.Offset);
            var nextMidnightKst = new DateTimeOffset(nowKst.Date, KoreanTime.Offset).AddDays(1);
            var delay = nextMidnightKst - nowUtc;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                await FillOnceAsync(DateOnly.FromDateTime(nextMidnightKst.Date), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "점검일지 자동 기록 중 오류 발생");
            }
        }
    }

    private async Task FillOnceAsync(DateOnly today, CancellationToken stoppingToken)
    {
        int todayIndex = (int)today.DayOfWeek; // 일=0 ~ 토=6
        var weekStart = today.AddDays(-todayIndex);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        //--------------------------------------------------------------------------------//
        // 일요일이면 이 주 자체가 아직 없을 수 있다 — 지난주 토요일 값이 있는 항목만, 이 주의
        // 일요일 칸으로 이어받는다(로그/결과 행이 없으면 새로 만든다). 지난주에 기록이 전혀
        // 없던 항목은 이어받을 게 없으니 그대로 둔다 — 최초 기록은 항상 사용자가 직접 해야 한다.
        //--------------------------------------------------------------------------------//
        if (todayIndex == 0)
            await CarryOverFromPreviousWeekAsync(db, weekStart, stoppingToken);

        var logs = await db.InspectionLogs
            .Include(l => l.Results)
            .Where(l => l.WeekStartDate == weekStart)
            .ToListAsync(stoppingToken);

        foreach (var log in logs)
        {
            foreach (var result in log.Results)
            {
                for (int day = 1; day <= todayIndex; day++)
                {
                    if (GetDay(result, day) is not null)
                        continue; // 사용자가 이미 기록한 값은 그대로 둔다

                    var previous = GetDay(result, day - 1);
                    if (previous is not null)
                        SetDay(result, day, previous);
                }
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static async Task CarryOverFromPreviousWeekAsync(AppDbContext db, DateOnly weekStart, CancellationToken stoppingToken)
    {
        var previousWeekLogs = await db.InspectionLogs
            .Include(l => l.Results)
            .Where(l => l.WeekStartDate == weekStart.AddDays(-7))
            .ToListAsync(stoppingToken);

        foreach (var previousLog in previousWeekLogs)
        {
            InspectionLog? thisWeekLog = null;

            foreach (var previousResult in previousLog.Results)
            {
                if (previousResult.Sat is null)
                    continue;

                thisWeekLog ??= await db.InspectionLogs
                    .Include(l => l.Results)
                    .FirstOrDefaultAsync(l => l.EquipmentId == previousLog.EquipmentId && l.WeekStartDate == weekStart, stoppingToken);

                if (thisWeekLog is null)
                {
                    thisWeekLog = new InspectionLog
                    {
                        EquipmentId = previousLog.EquipmentId, WeekStartDate = weekStart,
                        CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    };
                    db.InspectionLogs.Add(thisWeekLog);
                }

                var thisResult = thisWeekLog.Results.FirstOrDefault(r => r.ItemNo == previousResult.ItemNo);
                if (thisResult is null)
                {
                    thisResult = new InspectionResult { ItemNo = previousResult.ItemNo };
                    thisWeekLog.Results.Add(thisResult);
                }

                thisResult.Sun ??= previousResult.Sat;
            }
        }
    }

    private static string? GetDay(InspectionResult r, int day) => day switch
    {
        0 => r.Sun, 1 => r.Mon, 2 => r.Tue, 3 => r.Wed, 4 => r.Thu, 5 => r.Fri, 6 => r.Sat,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };

    private static void SetDay(InspectionResult r, int day, string? value)
    {
        switch (day)
        {
            case 0: r.Sun = value; break;
            case 1: r.Mon = value; break;
            case 2: r.Tue = value; break;
            case 3: r.Wed = value; break;
            case 4: r.Thu = value; break;
            case 5: r.Fri = value; break;
            case 6: r.Sat = value; break;
        }
    }
}
