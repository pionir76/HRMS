using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

//---------------------------------------------------------------------------//
// 압축기 조회/수정 API. 압축기 자체를 새로 등록하거나 제거하는 기능은 없다 — 압축기는
// 장비 등록(POST /api/equipments) 시점에만 같이 생성되고 개수가 영구히 고정된다(사용자 결정).
// 수정(IP/MAC/순번, 채널 설정)은 시스템관리자 또는 그 장비의 담당자만 가능하다(EquipmentAccess).
//---------------------------------------------------------------------------//
[ApiController]
[Route("api/compressors")]
[Authorize]
public class CompressorsController(AppDbContext db) : ControllerBase
{
    //---------------------------------------------------------------------------//
    // GET api/compressors — 압축기 목록(소속 장비명 조인 포함), 대시보드용 평탄화된 목록.
    // 장비 상태가 `운영`인 장비의 압축기만 내려준다(사용자 결정 2026-09-17 — 운영이 아닌 장비는
    // 실시간 현황에 노출하지 않는다). 장비와 짝지을 때 이름 문자열 대신 EquipmentId를 쓰도록 같이 준다.
    //---------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<List<CompressorFlatDto>>> GetAll()
    {
        var rows = await (
            from c in db.Compressors
            join e in db.Equipments on c.EquipmentId equals e.Id
            where e.Status == EquipmentStatus.운영
            orderby e.BuildingName, e.Name, c.SequenceNo
            select new { c, e.BuildingName, e.Name }
        ).ToListAsync();

        return Ok(rows.Select(r => new CompressorFlatDto(
            r.c.Id, r.c.EquipmentId, r.c.SequenceNo, r.BuildingName, r.Name, r.c.IpAddress, r.c.MacAddress,
            r.c.CommunicationStatus.ToString(), r.c.AlarmStatus.IsConfirmedAlarm())).ToList());
    }

    //---------------------------------------------------------------------------//
    // GET api/compressors/{id}/channels — 해당 압축기의 CH01~07 현재값
    // (CompressorPollingService가 3초마다 갱신하는 CompressorSensorCurrent를 그대로 조회)
    //---------------------------------------------------------------------------//
    [HttpGet("{id}/channels")]
    public async Task<ActionResult<List<ChannelValueDto>>> GetChannels(int id)
    {
        if (await db.Compressors.FindAsync(id) is null)
            return NotFound();

        var entities = await db.CompressorSensorCurrents
            .Where(s => s.CompressorId == id)
            .OrderBy(s => s.ChannelNo)
            .ToListAsync();

        return Ok(entities.Select(s => new ChannelValueDto(s.ChannelNo.ToString(), s.Value, s.MeasuredAt)).ToList());
    }

    //---------------------------------------------------------------------------//
    // GET api/compressors/{id}/channel-settings — 해당 압축기의 CH01~07 채널 설정
    // (채널명/단위/경보 상하한 등 — 프론트가 raw 값을 실제 값으로 표시할 때 필요한 정보)
    //---------------------------------------------------------------------------//
    [HttpGet("{id}/channel-settings")]
    public async Task<ActionResult<List<ChannelSettingDto>>> GetChannelSettings(int id)
    {
        if (await db.Compressors.FindAsync(id) is null)
            return NotFound();

        var entities = await db.CompressorChannelSettings
            .Where(s => s.CompressorId == id)
            .OrderBy(s => s.ChannelNo)
            .ToListAsync();

        return Ok(entities.Select(s => new ChannelSettingDto(
            s.ChannelNo.ToString(), s.ChannelName, s.Unit, s.Enabled, s.LowerLimit, s.UpperLimit,
            s.AlarmEnabled, s.AlarmDelaySeconds, s.AlarmClearDelaySeconds, s.DecimalPlaces)).ToList());
    }

    //---------------------------------------------------------------------------//
    // PUT api/compressors/{id} — IP/MAC/순번 수정. 압축기 자체를 추가하거나 지우는 API는 없다.
    //---------------------------------------------------------------------------//
    [HttpPut("{id}")]
    public async Task<ActionResult<CompressorDto>> Update(int id, SaveCompressorRequest request)
    {
        var compressor = await db.Compressors.FindAsync(id);
        if (compressor is null)
            return NotFound();

        if (!await EquipmentAccess.CanManageAsync(db, User, compressor.EquipmentId))
            return Forbid();

        compressor.IpAddress = request.IpAddress;
        compressor.MacAddress = request.MacAddress;
        compressor.SequenceNo = request.SequenceNo;
        await db.SaveChangesAsync();

        return Ok(new CompressorDto(
            compressor.Id, compressor.SequenceNo, compressor.IpAddress, compressor.MacAddress,
            compressor.CommunicationStatus.ToString(), compressor.AlarmStatus.IsConfirmedAlarm()));
    }

    //---------------------------------------------------------------------------//
    // PUT api/compressors/{id}/channel-settings/{channelNo} — 채널 하나의 설정 수정
    // (경보 상/하한, 지연시간, on/off, 채널명/단위, 표시 소수점 자리수).
    //---------------------------------------------------------------------------//
    [HttpPut("{id}/channel-settings/{channelNo}")]
    public async Task<ActionResult<ChannelSettingDto>> UpdateChannelSetting(int id, string channelNo, SaveChannelSettingRequest request)
    {
        var compressor = await db.Compressors.FindAsync(id);
        if (compressor is null)
            return NotFound();

        if (!await EquipmentAccess.CanManageAsync(db, User, compressor.EquipmentId))
            return Forbid();

        if (!Enum.TryParse<ChannelNo>(channelNo, out var channel))
            return BadRequest("올바르지 않은 채널 번호입니다.");

        var setting = await db.CompressorChannelSettings.FindAsync(id, channel);
        if (setting is null)
            return NotFound(); // 압축기당 CH01~07 7행이 항상 있어야 하므로 정상적으로는 발생하지 않음

        setting.ChannelName = request.ChannelName ?? "";
        setting.Unit = request.Unit ?? "";
        setting.Enabled = request.Enabled;
        setting.LowerLimit = request.LowerLimit;
        setting.UpperLimit = request.UpperLimit;
        setting.AlarmEnabled = request.AlarmEnabled;
        setting.AlarmDelaySeconds = request.AlarmDelaySeconds;
        setting.AlarmClearDelaySeconds = request.AlarmClearDelaySeconds;
        setting.DecimalPlaces = request.DecimalPlaces;
        await db.SaveChangesAsync();

        return Ok(new ChannelSettingDto(
            setting.ChannelNo.ToString(), setting.ChannelName, setting.Unit, setting.Enabled,
            setting.LowerLimit, setting.UpperLimit, setting.AlarmEnabled,
            setting.AlarmDelaySeconds, setting.AlarmClearDelaySeconds, setting.DecimalPlaces));
    }
}
