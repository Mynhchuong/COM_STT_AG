using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly ReportService _service;

    public ReportController(ReportService service)
    {
        _service = service;
    }

    [HttpGet("compstt-set")]
    public async Task<IActionResult> GetCompSttSetReport(
        [FromQuery] string fromDate, [FromQuery] string toDate,
        [FromQuery] string? po, [FromQuery] string? part)
    {
        if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var from) ||
            !DateTime.TryParseExact(toDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var to))
        {
            return BadRequest(new { success = false, message = "fromDate/toDate phải định dạng yyyyMMdd" });
        }

        try
        {
            var rows = await _service.GetCompSttSetReportAsync(from, to.AddDays(1).AddSeconds(-1), po, part);
            return Ok(new { success = true, total = rows.Count, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
