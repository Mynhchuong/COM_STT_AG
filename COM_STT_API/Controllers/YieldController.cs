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

        try
        {
            var savedCount = await _yieldService.SaveYieldBatchAsync(items);
            return Ok(new { success = true, savedCount, message = $"Đã lưu thành công {savedCount} dòng vào TRTB_M_KEYIN_YIELD" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu dữ liệu TRTB_M_KEYIN_YIELD: " + ex.Message });
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday([FromQuery] string? worker)
    {
        try
        {
            var items = await _yieldService.GetTodayAsync(worker);
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
