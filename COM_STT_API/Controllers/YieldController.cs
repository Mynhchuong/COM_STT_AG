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
            var (savedCount, partYieldMessage, basketId) = await _yieldService.SaveYieldBatchAsync(items);
            var message = $"Đã lưu thành công {savedCount} dòng vào TRTB_M_KEYIN_YIELD";
            if (!string.IsNullOrWhiteSpace(partYieldMessage))
            {
                message += $" — {partYieldMessage}";
            }
            return Ok(new { success = true, savedCount, partYieldMessage, basketId, message });
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

    [HttpGet("size-pivot")]
    public async Task<IActionResult> GetSizePivot([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo" });
        }

        try
        {
            var rows = await _yieldService.GetSizePivotByOrderAsync(ordNo.Trim());
            return Ok(new { success = true, total = rows.Count, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("complete-order")]
    public async Task<IActionResult> CompleteOrder([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo" });
        }

        try
        {
            var updatedRows = await _yieldService.CompleteOrderAsync(ordNo.Trim());
            return Ok(new { success = true, updatedRows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("routing-exists")]
    public async Task<IActionResult> RoutingExists([FromQuery] string style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số style" });
        }

        try
        {
            var exists = await _yieldService.RoutingExistsAsync(style.Trim());
            return Ok(new { success = true, exists });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("order-complete-status")]
    public async Task<IActionResult> GetOrderCompleteStatus([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo" });
        }

        try
        {
            var isComplete = await _yieldService.IsOrderCompleteAsync(ordNo.Trim());
            return Ok(new { success = true, isComplete });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("part-mark-done")]
    public async Task<IActionResult> MarkPartDone([FromQuery] string ordNo, [FromQuery] string size, [FromQuery] string partsNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo) || string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(partsNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo/size/partsNo" });
        }

        try
        {
            var (success, message) = await _yieldService.MarkPartYieldDoneAsync(ordNo, size, partsNo);
            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("part-update-qty")]
    public async Task<IActionResult> UpdatePartQty([FromQuery] string ordNo, [FromQuery] string size, [FromQuery] string partsNo, [FromQuery] int qty)
    {
        if (string.IsNullOrWhiteSpace(ordNo) || string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(partsNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo/size/partsNo" });
        }

        try
        {
            var (success, message) = await _yieldService.UpdatePartYieldQtyAsync(ordNo, size, partsNo, qty);
            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("part-status")]
    public async Task<IActionResult> GetPartStatus([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số ordNo" });
        }

        try
        {
            var rows = await _yieldService.GetPartYieldStatusByOrderAsync(ordNo.Trim());
            return Ok(new { success = true, total = rows.Count, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
