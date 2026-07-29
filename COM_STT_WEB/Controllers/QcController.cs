using COM_STT_WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class QcController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Shared/ComingSoon.cshtml", new ComingSoonViewModel
        {
            Title = "QC",
            Icon = "verified",
            GradientClass = "bg-gradient-success"
        });
    }
}
