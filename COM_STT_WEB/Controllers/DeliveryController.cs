using COM_STT_WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class DeliveryController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Shared/ComingSoon.cshtml", new ComingSoonViewModel
        {
            Title = "Nhân viên giao hàng",
            Icon = "local_shipping",
            GradientClass = "bg-gradient-secondary"
        });
    }
}
