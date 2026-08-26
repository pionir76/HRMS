using HRMS.Infrastructure;
using HRMS.Modules.Equipment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

// 장비 조회 API. 서비스/리포지토리 계층 없이 DbContext를 직접 써서 단순하게 유지한다
// (이 규모의 시스템에서는 매 요청 DB 직접 조회로 충분 — overview.md 4.4 참고).
[ApiController]
[Route("api/equipments")]
[Authorize]
public class EquipmentsController(AppDbContext db) : ControllerBase
{
    // GET api/equipments — 장비 전체 목록
    [HttpGet]
    public async Task<ActionResult<List<EquipmentDto>>> GetAll()
    {
        var entities = await db.Equipments.ToListAsync();
        return Ok(entities.Select(ToDto).ToList());
    }

    // GET api/equipments/{id} — 장비 단건
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentDto>> GetOne(int id)
    {
        var entity = await db.Equipments.FindAsync(id);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    // GET api/equipments/{id}/compressors — 해당 장비 소속 압축기 목록(통신/경보 상태 포함)
    [HttpGet("{id}/compressors")]
    public async Task<ActionResult<List<CompressorDto>>> GetCompressors(int id)
    {
        if (await db.Equipments.FindAsync(id) is null)
            return NotFound();

        var entities = await db.Compressors.Where(c => c.EquipmentId == id).ToListAsync();
        return Ok(entities.Select(c => new CompressorDto(
            c.Id, c.IpAddress, c.MacAddress, c.CommunicationStatus.ToString(), c.AlarmStatus.ToString())).ToList());
    }

    // enum -> 문자열 변환은 항상 엔티티를 메모리로 가져온 뒤(ToListAsync 등) 수행한다.
    // EF Core가 enum.ToString()을 SQL로 안정적으로 번역하지 못할 수 있어서다.
    private static EquipmentDto ToDto(Models.Equipment e) =>
        new(e.Id, e.Region, e.BuildingName, e.Name, e.Status.ToString());
}
