using COM_STT_WEB.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class CuttingController : Controller
{
    private readonly CuttingApiService _apiService;

    public CuttingController(CuttingApiService apiService)
    {
        _apiService = apiService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult CheckIn()
    {
        return View();
    }

    public IActionResult CheckOut()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CardInfo([FromQuery] string cardNo)
    {
        var info = await _apiService.GetCardInfoAsync(cardNo);
        if (info == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy PCARD này cho công đoạn cắt (UCT)" });
        }
        return Ok(new { success = true, data = info });
    }
}
