using COM_STT_WEB.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ReportApiService _apiService;
    private readonly ProductionApiService _productionApiService;

    public ReportController(ReportApiService apiService, ProductionApiService productionApiService)
    {
        _apiService = apiService;
        _productionApiService = productionApiService;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Báo cáo theo PO — dựa trên MES.V_COMPSTT_PO_REPORT (Part No cố định 190 trong view).
    // Nhận sẵn ?po=... để bấm từ cột PO ở trang Báo cáo tổng nhảy thẳng qua đây, tự tìm luôn.
    public IActionResult ByPo(string? po)
    {
        ViewData["Po"] = po;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCompSttSetReport([FromQuery] string fromDate, [FromQuery] string toDate, [FromQuery] string? po, [FromQuery] string? part)
    {
        if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn khoảng thời gian." });
        }

        var rows = await _apiService.GetCompSttSetReportAsync(fromDate, toDate, po, part);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet]
    public async Task<IActionResult> GetCompSttPoReport([FromQuery] string po)
    {
        if (string.IsNullOrWhiteSpace(po))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số PO." });
        }

        var rows = await _apiService.GetCompSttPoReportAsync(po.Trim());
        return Ok(new { success = true, data = rows });
    }

    // Danh sách thẻ PCard theo PO, phân loại: đã Out / đang chờ nhận (Pending) / chưa nhận gì.
    [HttpGet]
    public async Task<IActionResult> GetCardsByPo([FromQuery] string po)
    {
        if (string.IsNullOrWhiteSpace(po))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số PO." });
        }

        var rows = await _productionApiService.GetCardsByPoAsync(po.Trim());
        return Ok(new { success = true, data = rows });
    }
}
