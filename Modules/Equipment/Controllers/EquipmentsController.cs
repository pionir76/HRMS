using HRMS.Infrastructure;
using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

//---------------------------------------------------------------------------//
// 장비 조회/등록/수정 API. 서비스/리포지토리 계층 없이 DbContext를 직접 써서 단순하게 유지한다
// (이 규모의 시스템에서는 매 요청 DB 직접 조회로 충분 — overview.md 4.4 참고).
// 등록/수정(장비관리 영역)은 시스템관리자 또는 그 장비의 담당자(UserEquipment)만 가능하다
// (EquipmentAccess 참고) — 점검일지/운전일지 등 다른 API에는 이 제약이 없다.
//---------------------------------------------------------------------------//
[ApiController]
[Route("api/equipments")]
[Authorize]
public class EquipmentsController(AppDbContext db) : ControllerBase
{
    // GET api/equipments — 장비 전체 목록. 시설동명 -> 장비명 오름차순으로 안정 정렬해서 내려간다
    // (호출할 때마다 순서가 흔들리면 프론트가 지역/시설동/설비 선택 드롭다운을 구성하기 어렵다).
    [HttpGet]
    public async Task<ActionResult<List<EquipmentDto>>> GetAll()
    {
        var entities = await db.Equipments.OrderBy(e => e.BuildingName).ThenBy(e => e.Name).ToListAsync();
        var ids = entities.Select(e => e.Id);
        var decimalPlaces = await GetRunningCurrentDecimalPlacesAsync(ids);
        var photoFlags = await GetPhotoFlagsAsync(ids);
        return Ok(entities.Select(e => ToDto(e, DecimalPlacesOrNull(decimalPlaces, e.Id), photoFlags[e.Id])).ToList());
    }

    // GET api/equipments/status — 실시간 현황 5초 갱신용 경량 상태(장비 + 소속 압축기 상태만).
    // 장비 상태가 `운영`인 장비만 내려준다(사용자 결정 2026-09-17 — 운영이 아닌 장비는 수집도
    // 노출도 하지 않는다). 정렬은 GET api/equipments와 같고, 압축기는 SequenceNo 오름차순.
    // 고정 경로라 아래 "{id}" 라우트보다 우선 매칭된다.
    [HttpGet("status")]
    public async Task<ActionResult<List<EquipmentStatusDto>>> GetStatuses()
    {
        var equipments = await db.Equipments
            .Where(e => e.Status == EquipmentStatus.운영)
            .OrderBy(e => e.BuildingName).ThenBy(e => e.Name)
            .ToListAsync();

        var ids = equipments.Select(e => e.Id).ToList();
        var compressorsByEquipment = (await db.Compressors
                .Where(c => ids.Contains(c.EquipmentId))
                .OrderBy(c => c.SequenceNo)
                .ToListAsync())
            .ToLookup(c => c.EquipmentId);

        return Ok(equipments.Select(e => new EquipmentStatusDto(
            e.Id, e.Region, e.BuildingName, e.Name, e.Status.ToString(),
            e.IsRunning, e.CommunicationStatus.ToString(), e.AlarmStatus != AlarmStatus.정상,
            compressorsByEquipment[e.Id]
                .Select(c => new CompressorStatusDto(
                    c.Id, c.SequenceNo, c.CommunicationStatus.ToString(), c.AlarmStatus != AlarmStatus.정상))
                .ToList()
        )).ToList());
    }

    // GET api/equipments/{id} — 장비 단건
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentDto>> GetOne(int id)
    {
        var entity = await db.Equipments.FindAsync(id);
        if (entity is null)
            return NotFound();

        var decimalPlaces = await GetRunningCurrentDecimalPlacesAsync([id]);
        var photoFlags = await GetPhotoFlagsAsync([id]);
        return Ok(ToDto(entity, DecimalPlacesOrNull(decimalPlaces, id), photoFlags[id]));
    }

    // GET api/equipments/{id}/compressors — 해당 장비 소속 압축기 목록(통신/경보 상태 포함)
    [HttpGet("{id}/compressors")]
    public async Task<ActionResult<List<CompressorDto>>> GetCompressors(int id)
    {
        if (await db.Equipments.FindAsync(id) is null)
            return NotFound();

        var entities = await db.Compressors.Where(c => c.EquipmentId == id).OrderBy(c => c.SequenceNo).ToListAsync();
        return Ok(entities.Select(c => new CompressorDto(
            c.Id, c.SequenceNo, c.IpAddress, c.MacAddress, c.CommunicationStatus.ToString(), c.AlarmStatus != AlarmStatus.정상)).ToList());
    }

    //---------------------------------------------------------------------------//
    // POST api/equipments — 신규 장비 등록. 시스템관리자 전용이다(신규 장비는 아직 담당자가
    // 없으므로 "소속 장비" 개념이 성립하지 않는다 — 사용자 결정).
    // 압축기는 이 시점에만 같이 등록할 수 있다 — 등록 이후에는 압축기를 추가/제거하는 API
    // 자체가 없어서 개수가 영구히 고정된다. 압축기마다 CH01~07 채널 설정 7행을 기존 시드와
    // 동일한 기본값으로 같이 생성한다.
    //---------------------------------------------------------------------------//
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create(CreateEquipmentRequest request)
    {
        if (!Enum.TryParse<EquipmentStatus>(request.Equipment.Status, out var status))
            return BadRequest("올바르지 않은 상태값입니다.");

        if (request.Compressors.Count == 0)
            return BadRequest("압축기를 1대 이상 등록해야 합니다.");

        if (await db.Equipments.AnyAsync(e => e.BuildingName == request.Equipment.BuildingName && e.Name == request.Equipment.Name))
            return Conflict("같은 시설동에 동일한 장비명이 이미 있습니다.");

        var equipment = ToEntity(new Models.Equipment { Region = request.Equipment.Region, BuildingName = request.Equipment.BuildingName, Name = request.Equipment.Name }, request.Equipment, status);

        int sequenceNo = 1;
        foreach (var c in request.Compressors)
        {
            var compressor = new Compressor
            {
                SequenceNo = sequenceNo++,
                IpAddress = c.IpAddress,
                MacAddress = c.MacAddress,
                CommunicationStatus = CommunicationStatus.끊김, // 아직 폴링된 적 없음
                AlarmStatus = AlarmStatus.정상
            };
            compressor.ChannelSettings = ChannelDefaults.CreateAll();
            equipment.Compressors.Add(compressor);
        }

        db.Equipments.Add(equipment);
        await db.SaveChangesAsync();

        var decimalPlaces = await GetRunningCurrentDecimalPlacesAsync([equipment.Id]);
        var photoFlags = await GetPhotoFlagsAsync([equipment.Id]);
        return Ok(ToDto(equipment, DecimalPlacesOrNull(decimalPlaces, equipment.Id), photoFlags[equipment.Id]));
    }

    //---------------------------------------------------------------------------//
    // PUT api/equipments/{id} — 장비 속성 수정. 압축기 목록은 이 API로 건드릴 수 없다(고정).
    // "삭제"는 별도 API가 아니라 Status를 철거로 바꿔서 표현한다 — 기존 데이터는 그대로 남는다.
    //---------------------------------------------------------------------------//
    [HttpPut("{id}")]
    public async Task<ActionResult<EquipmentDto>> Update(int id, SaveEquipmentRequest request)
    {
        if (!await EquipmentAccess.CanManageAsync(db, User, id))
            return Forbid();

        var equipment = await db.Equipments.FindAsync(id);
        if (equipment is null)
            return NotFound();

        if (!Enum.TryParse<EquipmentStatus>(request.Status, out var status))
            return BadRequest("올바르지 않은 상태값입니다.");

        if (await db.Equipments.AnyAsync(e => e.Id != id && e.BuildingName == request.BuildingName && e.Name == request.Name))
            return Conflict("같은 시설동에 동일한 장비명이 이미 있습니다.");

        ToEntity(equipment, request, status);
        await db.SaveChangesAsync();

        var decimalPlaces = await GetRunningCurrentDecimalPlacesAsync([id]);
        var photoFlags = await GetPhotoFlagsAsync([id]);
        return Ok(ToDto(equipment, DecimalPlacesOrNull(decimalPlaces, id), photoFlags[id]));
    }

    //---------------------------------------------------------------------------//
    // 장비별로 "1번 압축기"(SequenceNo 최소, 운전일지의 운전전류 규칙과 동일 기준) CH07 채널
    // 설정의 DecimalPlaces를 찾는다. RunningCurrentThreshold는 raw 값이라 그 자체로는 소수점
    // 자리수를 알 수 없어서, 프론트가 raw<->실제값 변환에 쓸 힌트로 같이 내려주기 위함이다.
    // 압축기가 없는 장비는 결과 딕셔너리에서 빠진다 — Dictionary<int,int>.GetValueOrDefault는
    // 없는 키에 0을 돌려줘서 "0자리"와 "정보 없음"이 구분이 안 되므로, DecimalPlacesOrNull로
    // 감싸서 없으면 진짜 null이 나가게 한다.
    //---------------------------------------------------------------------------//
    private static int? DecimalPlacesOrNull(Dictionary<int, int> decimalPlaces, int equipmentId) =>
        decimalPlaces.TryGetValue(equipmentId, out var dp) ? dp : null;

    private async Task<Dictionary<int, int>> GetRunningCurrentDecimalPlacesAsync(IEnumerable<int> equipmentIds)
    {
        var ids = equipmentIds.ToList();
        var compressors = await db.Compressors.Where(c => ids.Contains(c.EquipmentId)).ToListAsync();

        var representativeIdByEquipment = compressors
            .GroupBy(c => c.EquipmentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SequenceNo).First().Id);

        var representativeIds = representativeIdByEquipment.Values.ToList();
        var decimalPlacesByCompressor = await db.CompressorChannelSettings
            .Where(s => representativeIds.Contains(s.CompressorId) && s.ChannelNo == ChannelNo.CH07)
            .ToDictionaryAsync(s => s.CompressorId, s => s.DecimalPlaces);

        return representativeIdByEquipment
            .Where(kv => decimalPlacesByCompressor.ContainsKey(kv.Value))
            .ToDictionary(kv => kv.Key, kv => decimalPlacesByCompressor[kv.Value]);
    }

    //---------------------------------------------------------------------------//
    // 장비별 사진 등록 여부(장비 사진/설치 사진)만 가볍게 조회한다. 사진 원본은
    // Modules/Attachment의 별도 API로 받는다 — 여기서는 "있다/없다"만 필요하다.
    //---------------------------------------------------------------------------//
    private async Task<Dictionary<int, (bool HasEquipmentPhoto, bool HasInstallationPhoto)>> GetPhotoFlagsAsync(IEnumerable<int> equipmentIds)
    {
        var ids = equipmentIds.ToList();
        var slotsByEquipment = await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.EquipmentPhoto && ids.Contains(a.OwnerId))
            .Select(a => new { a.OwnerId, a.Slot })
            .ToListAsync();

        return ids.ToDictionary(id => id, id => (
            slotsByEquipment.Any(s => s.OwnerId == id && s.Slot == "equipment"),
            slotsByEquipment.Any(s => s.OwnerId == id && s.Slot == "installation")));
    }

    // 요청 DTO의 필드를 엔티티에 그대로 옮겨 담는다(자동 계산값 3개는 건드리지 않음).
    private static Models.Equipment ToEntity(Models.Equipment e, SaveEquipmentRequest r, EquipmentStatus status)
    {
        e.Region = r.Region;
        e.BuildingNumber = r.BuildingNumber;
        e.BuildingName = r.BuildingName;
        e.Location = r.Location;
        e.Name = r.Name;
        e.Status = status;
        e.ModelName = r.ModelName;
        e.Manufacturer = r.Manufacturer;
        e.PermitNumber = r.PermitNumber;
        e.ManagementNumber = r.ManagementNumber;
        e.LegalRefrigerationCapacity = r.LegalRefrigerationCapacity;
        e.UsRefrigerationCapacity = r.UsRefrigerationCapacity;
        e.Refrigerant = r.Refrigerant;
        e.ChargeAmount = r.ChargeAmount;
        e.CompressorManufacturer = r.CompressorManufacturer;
        e.CompressorType = r.CompressorType;
        e.CompressorCount = r.CompressorCount;
        e.CompressorCapacity = r.CompressorCapacity;
        e.CoolingTowerManufacturer = r.CoolingTowerManufacturer;
        e.CoolingTowerType = r.CoolingTowerType;
        e.CoolingTowerCount = r.CoolingTowerCount;
        e.CoolingTowerCapacity = r.CoolingTowerCapacity;
        e.CondenserType = r.CondenserType;
        e.EvaporatorType = r.EvaporatorType;
        e.DesignPressure = r.DesignPressure;
        e.OverPressureCutoff = r.OverPressureCutoff;
        e.RatedVoltage = r.RatedVoltage;
        e.RatedCurrent = r.RatedCurrent;
        e.SafetyValve = r.SafetyValve;
        e.Notes = r.Notes;
        e.HasCoolingWater = r.HasCoolingWater;
        e.CoolingWaterInletMin = r.CoolingWaterInletMin;
        e.CoolingWaterInletMax = r.CoolingWaterInletMax;
        e.CoolingWaterOutletMin = r.CoolingWaterOutletMin;
        e.CoolingWaterOutletMax = r.CoolingWaterOutletMax;
        e.HasBrine = r.HasBrine;
        e.BrineInletMin = r.BrineInletMin;
        e.BrineInletMax = r.BrineInletMax;
        e.BrineOutletMin = r.BrineOutletMin;
        e.BrineOutletMax = r.BrineOutletMax;
        e.HasVoltage = r.HasVoltage;
        e.VoltageMin = r.VoltageMin;
        e.VoltageMax = r.VoltageMax;
        e.RunningCurrentThreshold = r.RunningCurrentThreshold;
        return e;
    }

    // enum -> 문자열 변환은 항상 엔티티를 메모리로 가져온 뒤(ToListAsync 등) 수행한다.
    // EF Core가 enum.ToString()을 SQL로 안정적으로 번역하지 못할 수 있어서다.
    private static EquipmentDto ToDto(Models.Equipment e, int? runningCurrentThresholdDecimalPlaces, (bool HasEquipmentPhoto, bool HasInstallationPhoto) photoFlags) => new(
        e.Id, e.Region, e.BuildingNumber, e.BuildingName, e.Location, e.Name, e.Status.ToString(),
        e.ModelName, e.Manufacturer, e.PermitNumber, e.ManagementNumber,
        e.LegalRefrigerationCapacity, e.UsRefrigerationCapacity, e.Refrigerant, e.ChargeAmount,
        e.CompressorManufacturer, e.CompressorType, e.CompressorCount, e.CompressorCapacity,
        e.CoolingTowerManufacturer, e.CoolingTowerType, e.CoolingTowerCount, e.CoolingTowerCapacity,
        e.CondenserType, e.EvaporatorType, e.DesignPressure, e.OverPressureCutoff,
        e.RatedVoltage, e.RatedCurrent, e.SafetyValve, e.Notes,
        e.HasCoolingWater, e.CoolingWaterInletMin, e.CoolingWaterInletMax, e.CoolingWaterOutletMin, e.CoolingWaterOutletMax,
        e.HasBrine, e.BrineInletMin, e.BrineInletMax, e.BrineOutletMin, e.BrineOutletMax,
        e.HasVoltage, e.VoltageMin, e.VoltageMax,
        e.RunningCurrentThreshold, runningCurrentThresholdDecimalPlaces,
        e.IsRunning, e.CommunicationStatus.ToString(), e.AlarmStatus != AlarmStatus.정상,
        photoFlags.HasEquipmentPhoto, photoFlags.HasInstallationPhoto);
}
