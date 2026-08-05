using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/compstt-set")]
public class CompSttSetController : ControllerBase
{
    private readonly CompSttSetService _service;

    public CompSttSetController(CompSttSetService service)
    {
        _service = service;
    }

    [HttpGet("basket-by-card")]
    public async Task<IActionResult> GetBasketIdByCard([FromQuery] string cardNo)
    {
        if (string.IsNullOrWhiteSpace(cardNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số cardNo" });
        }

        try
        {
            var basketId = await _service.GetBasketIdByCardNoAsync(cardNo.Trim());
            if (basketId == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy basket cho PCard {cardNo}" });
            }
            return Ok(new { success = true, basketId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("header")]
    public async Task<IActionResult> GetHeader([FromQuery] int basketId)
    {
        try
        {
            var rows = await _service.GetBasketHeaderAsync(basketId);
            return Ok(new { success = true, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("cards-by-po")]
    public async Task<IActionResult> GetCardsByPo([FromQuery] string po)
    {
        if (string.IsNullOrWhiteSpace(po))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số po" });
        }

        try
        {
            var rows = await _service.GetHeaderRowsByPoAsync(po.Trim());
            return Ok(new { success = true, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("detail")]
    public async Task<IActionResult> GetDetail([FromQuery] int basketId)
    {
        try
        {
            var rows = await _service.GetBasketDetailAsync(basketId);
            return Ok(new { success = true, data = rows });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("mark-done")]
    public async Task<IActionResult> MarkDone([FromQuery] int basketId, [FromQuery] string partsNo, [FromQuery] string workerId)
    {
        try
        {
            var (success, message) = await _service.MarkBasketDetailDoneAsync(basketId, partsNo, workerId);
            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("update-qty")]
    public async Task<IActionResult> UpdateQty([FromQuery] int basketId, [FromQuery] string partsNo, [FromQuery] int qty, [FromQuery] string workerId)
    {
        try
        {
            var (success, message) = await _service.UpdateBasketDetailQtyAsync(basketId, partsNo, qty, workerId);
            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
