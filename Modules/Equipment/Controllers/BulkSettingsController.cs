using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

//---------------------------------------------------------------------------//
// 설정값 일괄 적용 API(2026-10-01, 2026-09-14 긴급 추가 사양). 장비/압축기마다 하나씩 설정하면
// 오래 걸리는 값을, 관리자가 고른 여러 장비에 같은 값으로 한 번에 적용한다.
//
// 사용자 결정(2026-10-01):
//   - 대상 항목: 채널의 상·하한, 발생/해제 지연시간, 경보 켜기/끄기, 소수점 자리수
//               + 장비의 운전전류 기준값
//   - 범위: 장비를 직접 골라서 지정(전체/지역/시설동 같은 묶음 선택은 없다)
//   - 이미 개별로 다르게 설정된 값도 덮어쓴다(같은 값으로 맞추는 것이 목적)
//   - 시스템관리자 전용(프론트는 관리자 전용 메뉴로 분리 예정). 담당 장비 여부는 보지 않는다
//
// **전부-아니면-전무다.** 결재 일괄 처리(건별 처리)와 반대인데, 설정값은 일부만 바뀐 채 남으면
// 어디까지 바뀌었는지 사람이 알 수 없고 장비마다 기준이 달라진다. 그래서 검증에 하나라도 걸리면
// 아무것도 바꾸지 않고 400을 돌려준다.
//
// 바뀐 값은 다음 폴링 사이클(3초)부터 경보 판정·운전 판정에 반영된다(폴링이 매 사이클 DB에서
// 설정을 다시 읽는다).
//---------------------------------------------------------------------------//
[ApiController]
[Route("api/bulk-settings")]
[Authorize(Roles = nameof(UserRole.시스템관리자))]
public class BulkSettingsController(AppDbContext db) : ControllerBase
{
    //---------------------------------------------------------------------------//
    // PUT api/bulk-settings/channels — 선택한 장비들의 소속 압축기 전체에서, 지정한 채널의
    // 설정을 바꾼다. 값 필드 중 null인 것은 그대로 둔다.
    //
    // 사용 안 함(Enabled=false) 채널은 건너뛴다 — 센서가 없는 자리다(예: LG 냉동기의 CH03
    // 오일온도·CH06 오일압력). 건너뛴 개수는 응답의 skippedChannelCount로 알려준다.
    //---------------------------------------------------------------------------//
    [HttpPut("channels")]
    public async Task<ActionResult<BulkChannelSettingsResult>> ApplyChannelSettings(BulkChannelSettingsRequest request)
    {
        var equipmentIds = request.EquipmentIds?.Distinct().ToList() ?? [];
        if (equipmentIds.Count == 0)
            return BadRequest("적용할 장비를 하나 이상 선택하세요.");

        if (request.ChannelNos is null || request.ChannelNos.Count == 0)
            return BadRequest("적용할 채널을 하나 이상 선택하세요.");

        var channels = new List<ChannelNo>();
        foreach (string channelNo in request.ChannelNos.Distinct())
        {
            if (!TryParseChannel(channelNo, out var channel))
                return BadRequest($"올바르지 않은 채널 번호입니다: {channelNo}");
            channels.Add(channel);
        }

        bool changesLimits = request.LowerLimit is not null || request.UpperLimit is not null;
        if (!changesLimits && request.AlarmEnabled is null && request.AlarmDelaySeconds is null
            && request.AlarmClearDelaySeconds is null && request.DecimalPlaces is null)
            return BadRequest("변경할 값을 하나 이상 입력하세요.");

        //---------------------------------------------------------------------------//
        // 값 자체의 검증 — 개별 수정 API(CompressorsController.UpdateChannelSetting)와 같은 규칙.
        //---------------------------------------------------------------------------//
        if (request.LowerLimit is { } lower && request.UpperLimit is { } upper && lower > upper)
            return BadRequest("경보 하한은 상한보다 클 수 없습니다.");

        if (request.AlarmDelaySeconds < 0 || request.AlarmClearDelaySeconds < 0)
            return BadRequest("경보 지연시간은 0초 이상이어야 합니다.");

        if (request.DecimalPlaces is < 0 or > 4)
            return BadRequest("표시 소수점 자리수는 0~4 사이여야 합니다.");

        if (await FindMissingEquipmentAsync(equipmentIds) is { Count: > 0 } missing)
            return BadRequest($"존재하지 않는 장비가 포함되어 있습니다: {string.Join(", ", missing)}");

        var targets = await db.CompressorChannelSettings
            .Join(db.Compressors, s => s.CompressorId, c => c.Id,
                (s, c) => new { Setting = s, c.EquipmentId, c.SequenceNo })
            .Where(x => equipmentIds.Contains(x.EquipmentId) && channels.Contains(x.Setting.ChannelNo))
            .ToListAsync();

        var enabled = targets.Where(x => x.Setting.Enabled).ToList();
        if (enabled.Count == 0)
            return BadRequest("선택한 장비에는 사용 중인 해당 채널이 없습니다.");

        //---------------------------------------------------------------------------//
        // 상·하한은 raw 값이라, 단위나 소수점 자리수가 다른 채널에 같은 숫자를 넣으면 실제 의미가
        // 달라진다(같은 50이 온도 채널에선 5.0℃, 압력 채널에선 0.50MPa). 그래서 상·하한을 바꿀
        // 때는 대상 채널의 단위가 모두 같아야 하고, 소수점 자리수도 같아야 한다. 소수점 자리수를
        // 이번에 같이 바꾸는 경우에는 적용 후 전부 같아지므로 기존 값의 차이는 문제 삼지 않는다.
        //---------------------------------------------------------------------------//
        if (changesLimits)
        {
            if (enabled.Select(x => x.Setting.Unit).Distinct().Count() > 1)
                return BadRequest("단위가 다른 채널이 섞여 있어 같은 상·하한을 적용할 수 없습니다. 채널을 나눠서 적용하세요.");

            if (request.DecimalPlaces is null && enabled.Select(x => x.Setting.DecimalPlaces).Distinct().Count() > 1)
                return BadRequest("소수점 자리수가 다른 채널이 섞여 있어 같은 상·하한을 적용할 수 없습니다. 소수점 자리수를 함께 지정하거나 채널을 나눠서 적용하세요.");
        }

        //---------------------------------------------------------------------------//
        // 한쪽만 바꾸면 기존 반대쪽 값과 뒤집힐 수 있다(예: 하한만 50으로 올렸는데 어떤 압축기의
        // 상한이 40). 적용 후 값 기준으로 하나라도 뒤집히면 전부 거부한다 — 뒤집힌 채널은 영구
        // 경보가 된다.
        //---------------------------------------------------------------------------//
        var inverted = enabled
            .Where(x => (request.LowerLimit ?? x.Setting.LowerLimit) is { } lo
                     && (request.UpperLimit ?? x.Setting.UpperLimit) is { } hi
                     && lo > hi)
            .ToList();

        if (inverted.Count > 0)
        {
            var first = inverted[0];
            return BadRequest(
                $"적용하면 하한이 상한보다 커지는 채널이 {inverted.Count}개 있습니다" +
                $"(예: 장비 {first.EquipmentId}의 {first.SequenceNo}번 압축기 {first.Setting.ChannelNo}). " +
                "하한과 상한을 함께 입력하세요.");
        }

        foreach (var setting in enabled.Select(x => x.Setting))
        {
            if (request.LowerLimit is { } newLower) setting.LowerLimit = newLower;
            if (request.UpperLimit is { } newUpper) setting.UpperLimit = newUpper;
            if (request.AlarmEnabled is { } alarmEnabled) setting.AlarmEnabled = alarmEnabled;
            if (request.AlarmDelaySeconds is { } delay) setting.AlarmDelaySeconds = delay;
            if (request.AlarmClearDelaySeconds is { } clearDelay) setting.AlarmClearDelaySeconds = clearDelay;
            if (request.DecimalPlaces is { } decimalPlaces) setting.DecimalPlaces = decimalPlaces;
        }

        EventLogger.Add(db, EventLogCategory.System,
            $"{User.Identity?.Name}님이 장비 {equipmentIds.Count}대의 {string.Join("·", channels)} 채널 설정을 일괄 변경했습니다" +
            $"(채널 {enabled.Count}개, {DescribeChannelChanges(request)}).",
            User.Identity?.Name);

        await db.SaveChangesAsync();

        return Ok(new BulkChannelSettingsResult(equipmentIds.Count, enabled.Count, targets.Count - enabled.Count));
    }

    //---------------------------------------------------------------------------//
    // PUT api/bulk-settings/running-current-threshold — 선택한 장비들의 운전전류 기준값을 바꾼다.
    // 이 값은 CH07(운전전류) raw 값과 직접 비교되므로(EquipmentStatusAggregator.IsRunning),
    // 대상 장비들의 CH07 소수점 자리수가 같아야 같은 숫자가 같은 전류를 뜻한다.
    //---------------------------------------------------------------------------//
    [HttpPut("running-current-threshold")]
    public async Task<ActionResult<BulkRunningCurrentThresholdResult>> ApplyRunningCurrentThreshold(
        BulkRunningCurrentThresholdRequest request)
    {
        var equipmentIds = request.EquipmentIds?.Distinct().ToList() ?? [];
        if (equipmentIds.Count == 0)
            return BadRequest("적용할 장비를 하나 이상 선택하세요.");

        if (request.RunningCurrentThreshold is not { } threshold)
            return BadRequest("운전전류 기준값을 입력하세요.");

        if (threshold < 0)
            return BadRequest("운전전류 기준값은 0 이상이어야 합니다.");

        if (await FindMissingEquipmentAsync(equipmentIds) is { Count: > 0 } missing)
            return BadRequest($"존재하지 않는 장비가 포함되어 있습니다: {string.Join(", ", missing)}");

        var ch07DecimalPlaces = await db.CompressorChannelSettings
            .Join(db.Compressors, s => s.CompressorId, c => c.Id, (s, c) => new { s.ChannelNo, s.DecimalPlaces, c.EquipmentId })
            .Where(x => equipmentIds.Contains(x.EquipmentId) && x.ChannelNo == ChannelNo.CH07)
            .Select(x => x.DecimalPlaces)
            .Distinct()
            .ToListAsync();

        if (ch07DecimalPlaces.Count > 1)
            return BadRequest("운전전류(CH07) 소수점 자리수가 다른 장비가 섞여 있어 같은 기준값을 적용할 수 없습니다. 장비를 나눠서 적용하세요.");

        var equipments = await db.Equipments.Where(e => equipmentIds.Contains(e.Id)).ToListAsync();
        foreach (var equipment in equipments)
            equipment.RunningCurrentThreshold = threshold;

        EventLogger.Add(db, EventLogCategory.System,
            $"{User.Identity?.Name}님이 장비 {equipments.Count}대의 운전전류 기준값을 {threshold}(raw)로 일괄 변경했습니다.",
            User.Identity?.Name);

        await db.SaveChangesAsync();

        return Ok(new BulkRunningCurrentThresholdResult(equipments.Count));
    }

    private async Task<List<int>> FindMissingEquipmentAsync(List<int> equipmentIds)
    {
        var existing = await db.Equipments.Where(e => equipmentIds.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        return equipmentIds.Except(existing).ToList();
    }

    // 개별 수정 API와 같은 규칙 — "CH04"만 받고 "4" 같은 숫자 표기는 거부한다.
    private static bool TryParseChannel(string value, out ChannelNo channel) =>
        Enum.TryParse(value, out channel) && Enum.IsDefined(channel) && !char.IsDigit(value[0]);

    // 이벤트 로그에 "무엇을 무엇으로 바꿨는지" 남기기 위한 요약(raw 값 그대로).
    private static string DescribeChannelChanges(BulkChannelSettingsRequest r)
    {
        var parts = new List<string>();
        if (r.LowerLimit is { } lower) parts.Add($"하한 {lower}");
        if (r.UpperLimit is { } upper) parts.Add($"상한 {upper}");
        if (r.AlarmEnabled is { } enabled) parts.Add($"경보 {(enabled ? "켜기" : "끄기")}");
        if (r.AlarmDelaySeconds is { } delay) parts.Add($"발생지연 {delay}초");
        if (r.AlarmClearDelaySeconds is { } clearDelay) parts.Add($"해제지연 {clearDelay}초");
        if (r.DecimalPlaces is { } decimalPlaces) parts.Add($"소수점 {decimalPlaces}자리");
        return string.Join(", ", parts);
    }
}
