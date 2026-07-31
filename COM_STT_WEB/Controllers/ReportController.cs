using COM_STT_WEB.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ReportApiService _apiService;

    public ReportController(ReportApiService apiService)
    {
        _apiService = apiService;
    }

    public IActionResult Index()
    {
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
}
