using COM_STT_WEB.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        var employee = AuthHelper.GetCurrentUser(User);
        return View(employee);
    }
}
