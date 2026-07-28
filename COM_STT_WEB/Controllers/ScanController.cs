using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ScanController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
