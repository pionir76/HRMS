using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

//---------------------------------------------------------------------------//
// 압축기 조회 API.
//---------------------------------------------------------------------------//
[ApiController]
[Route("api/compressors")]
[Authorize]
public class CompressorsController(AppDbContext db) : ControllerBase
{
    //---------------------------------------------------------------------------//
    // GET api/compressors — 전체 압축기 목록(소속 장비명 조인 포함), 대시보드용 평탄화된 목록
    //---------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<List<CompressorFlatDto>>> GetAll()
    {
        var rows = await (
            from c in db.Compressors
            join e in db.Equipments on c.EquipmentId equals e.Id
            select new { c, e.BuildingName, e.Name }
        ).ToListAsync();

        return Ok(rows.Select(r => new CompressorFlatDto(
            r.c.Id, r.BuildingName, r.Name, r.c.IpAddress, r.c.MacAddress,
            r.c.CommunicationStatus.ToString(), r.c.AlarmStatus != AlarmStatus.정상)).ToList());
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
}
