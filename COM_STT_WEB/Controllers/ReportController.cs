using COM_STT_WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ReportController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Shared/ComingSoon.cshtml", new ComingSoonViewModel
        {
            Title = "Báo cáo",
            Icon = "bar_chart",
            GradientClass = "bg-gradient-warning"
        });
    }
}
