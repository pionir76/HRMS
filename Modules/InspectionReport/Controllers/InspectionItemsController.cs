using HRMS.Modules.InspectionReport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Modules.InspectionReport.Controllers;

//--------------------------------------------------------------------------------//
// 점검항목 고정 양식 조회. 장비/주차와 무관하게 항상 같은 10개 항목을 반환한다
// (InspectionItemCatalog 참고) — 프론트가 항목 목록을 직접 하드코딩하지 않아도 되게 하기 위함.
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/inspection-items")]
[Authorize]
public class InspectionItemsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<InspectionItemDefinition>> GetAll() => Ok(InspectionItemCatalog.Items);
}
