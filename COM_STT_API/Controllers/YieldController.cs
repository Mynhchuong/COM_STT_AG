using COM_STT_API.Models;
using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/yield")]
public class YieldController : ControllerBase
{
    private readonly YieldService _yieldService;

    public YieldController(YieldService yieldService)
    {
        _yieldService = yieldService;
    }

    [HttpPost("save-batch")]
    public async Task<IActionResult> SaveBatch([FromBody] List<KeyinYieldItemDto> items)
    {
        if (items == null || !items.Any())
        {
            return BadRequest(new { success = false, message = "Danh sách thẻ lưu rỗng." });
        }

        var distinctOrders = items.Select(i => i.COrdNo ?? string.Empty).Distinct().ToList();
        if (distinctOrders.Count > 1)
        {
            return BadRequest(new { success = false, message = "Tất cả thẻ trong 1 lượt lưu phải cùng 1 Order (PO). Đang có nhiều Order khác nhau: " + string.Join(", ", distinctOrders) });
        }

        try
        {
            var (savedCount, partYieldMessage, basketId, outputErrors) = await _yieldService.SaveYieldBatchAsync(items);
            var message = $"Đã lưu thành công {savedCount} dòng vào TRTB_M_KEYIN_YIELD";
            if (!string.IsNullOrWhiteSpace(partYieldMessage))
            {
                message += $" — {partYieldMessage}";
            }
            if (outputErrors.Count > 0)
            {
                message += " | " + string.Join(" | ", outputErrors);
            }
            return Ok(new { success = true, savedCount, partYieldMessage, basketId, outputErrors, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu dữ liệu TRTB_M_KEYIN_YIELD: " + ex.Message });
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday([FromQuery] string? worker, [FromQuery] string? date)
    {
        try
        {
            var items = await _yieldService.GetTodayAsync(worker, date);
            return Ok(new { success = true, total = items.Count, data = items });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{dGather}")]
    public async Task<IActionResult> Delete(string dGather)
    {
        try
        {
            var ok = await _yieldService.DeleteAsync(dGather);
            if (!ok)
            {
                return NotFound(new { success = false, message = "Không tìm thấy dòng này để xoá" });
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

}
