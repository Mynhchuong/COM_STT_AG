using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/prod-plan")]
public class ProdPlanController : ControllerBase
{
    private readonly ProdPlanService _prodPlanService;

    public ProdPlanController(ProdPlanService prodPlanService)
    {
        _prodPlanService = prodPlanService;
    }

    [HttpGet("{cardNo}")]
    public async Task<IActionResult> GetByCardNo(string cardNo)
    {
        try
        {
            var info = await _prodPlanService.GetProdPlanInfoAsync(cardNo);
            if (info == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy I_CARD_NO" });
            }
            return Ok(new { success = true, data = info });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
