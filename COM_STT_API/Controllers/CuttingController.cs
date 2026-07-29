using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/cutting")]
public class CuttingController : ControllerBase
{
    private readonly CuttingService _service;

    public CuttingController(CuttingService service)
    {
        _service = service;
    }

    [HttpGet("card/{cardNo}")]
    public async Task<IActionResult> GetCardInfo(string cardNo)
    {
        try
        {
            var info = await _service.GetCardInfoAsync(cardNo);
            if (info == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy PCARD này cho công đoạn cắt (UCT)" });
            }
            return Ok(new { success = true, data = info });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
