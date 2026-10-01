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
            s.AlarmEnabled, s.AlarmDelaySeconds, s.AlarmClearDelaySeconds, s.DecimalPlaces, s.RegisterAddress)).ToList());
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

        //---------------------------------------------------------------------------//
        // 순번 검증(2026-09-28 추가). 순번은 "1번 압축기" 판정의 근거라 중복/0/음수가 들어가면
        // 운전일지 자동기록의 대표 압축기, 장비 DTO의 소수점 힌트, 경보 메시지가 전부 흔들린다.
        //---------------------------------------------------------------------------//
        if (request.SequenceNo < 1)
            return BadRequest("압축기 순번은 1 이상이어야 합니다.");

        if (await db.Compressors.AnyAsync(c => c.EquipmentId == compressor.EquipmentId
                                            && c.Id != compressor.Id
                                            && c.SequenceNo == request.SequenceNo))
            return Conflict("같은 장비에 동일한 순번의 압축기가 이미 있습니다.");

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

        //---------------------------------------------------------------------------//
        // Enum.TryParse는 정의되지 않은 숫자 문자열("9" 등)도 통과시키므로 IsDefined로 한 번 더 막는다.
        // 그냥 두면 "9"가 404(FindAsync 실패)로 나가 문서(400)와 다르고, "3"처럼 숫자 표기가
        // CH03으로 조용히 동작하는 문서 외 경로도 열린다.
        //---------------------------------------------------------------------------//
        if (!Enum.TryParse<ChannelNo>(channelNo, out var channel) || !Enum.IsDefined(channel)
            || char.IsDigit(channelNo[0]))
            return BadRequest("올바르지 않은 채널 번호입니다.");

        //---------------------------------------------------------------------------//
        // 값 검증(2026-09-28 추가). 그 전에는 아래 값들이 그대로 저장되어 조용히 고장났다:
        //   - 상/하한 뒤집힘: AlarmEvaluator의 inRange가 항상 false -> 그 채널 영구 경보
        //   - 음수 지연시간: TimeSpan.FromSeconds(음수)라 지연 없이 즉시 확정(오탐 방지 무력화)
        //   - 과도한 소수점 자리수: 프론트 toFixed()가 RangeError로 화면을 못 그림
        //   - 범위 밖 레지스터 주소: D-Register는 4자리(0~9999)다
        //---------------------------------------------------------------------------//
        if (request.LowerLimit is { } lower && request.UpperLimit is { } upper && lower > upper)
            return BadRequest("경보 하한은 상한보다 클 수 없습니다.");

        if (request.AlarmDelaySeconds < 0 || request.AlarmClearDelaySeconds < 0)
            return BadRequest("경보 지연시간은 0초 이상이어야 합니다.");

        if (request.DecimalPlaces is < 0 or > 4)
            return BadRequest("표시 소수점 자리수는 0~4 사이여야 합니다.");

        if (request.RegisterAddress is < 0 or > 9999)
            return BadRequest("레지스터 주소는 0~9999 사이여야 합니다.");

        //---------------------------------------------------------------------------//
        // "사용 여부"와 "레지스터 주소"는 결국 같은 것(이 채널을 읽을지)을 가리키므로 한 쌍으로 묶는다
        // (사용자 결정 2026-09-18). 모순된 조합을 아예 저장하지 못하게 한다:
        //   - 사용 안 함  -> 주소를 지운다(읽지도 저장하지도 않는다)
        //   - 사용        -> 주소가 반드시 있어야 한다(없으면 400)
        // 경보만 끄고 값은 계속 수집하려면 AlarmEnabled를 쓴다.
        //---------------------------------------------------------------------------//
        if (request.Enabled && request.RegisterAddress is null)
            return BadRequest("채널을 사용하려면 레지스터 주소를 입력해야 합니다.");

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
        setting.RegisterAddress = request.Enabled ? request.RegisterAddress : null;
        await db.SaveChangesAsync();

        return Ok(new ChannelSettingDto(
            setting.ChannelNo.ToString(), setting.ChannelName, setting.Unit, setting.Enabled,
            setting.LowerLimit, setting.UpperLimit, setting.AlarmEnabled,
            setting.AlarmDelaySeconds, setting.AlarmClearDelaySeconds, setting.DecimalPlaces,
            setting.RegisterAddress));
    }
}
