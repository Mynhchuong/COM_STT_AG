using System.Security.Claims;
using COM_STT_WEB.API.Service;
using COM_STT_WEB.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiService _authApiService;

    public AccountController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.LoginAsync(model.EmpCd, model.Password);
        if (!result.Success || result.Employee == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng nhập thất bại.");
            return View(model);
        }

        var employee = result.Employee;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.EmpCd),
            new(ClaimTypes.Name, employee.FullName),
            new("DeptCd", employee.DeptCd),
            new("DeptNm", employee.DeptNm ?? string.Empty),
            new("LineCd", employee.LineCd ?? string.Empty),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Chỉ cần đăng nhập 1 lần/ngày: phiên hết hạn đúng nửa đêm hôm nay, không gia hạn theo hoạt động.
        var expireAtMidnight = DateTimeOffset.Now.Date.AddDays(1);

        // RememberMe = false -> cookie chỉ tồn tại trong phiên trình duyệt (mất khi đóng trình duyệt).
        // RememberMe = true  -> cookie tồn tại lại kể cả khi đóng/mở lại trình duyệt.
        // Dù chọn kiểu nào, phiên vẫn bị ép hết hạn lúc nửa đêm (ExpiresUtc) để bắt đăng nhập lại mỗi ngày.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = expireAtMidnight.ToUniversalTime(),
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && IsSafeReturnUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // Nếu phiên đã hết hạn và người dùng bấm "Đăng xuất", Logout ([Authorize], chỉ nhận POST)
    // sẽ tự bounce về Login kèm ReturnUrl=/Account/Logout. Đăng nhập lại xong mà redirect thẳng
    // vào ReturnUrl đó thì dính 404 (Logout không có GET). Chặn các action Account trong ReturnUrl.
    private bool IsSafeReturnUrl(string returnUrl)
    {
        var path = returnUrl.Split('?', '#')[0];
        var loginPath = Url.Action(nameof(Login), "Account");
        var logoutPath = Url.Action(nameof(Logout), "Account");

        return !string.Equals(path, loginPath, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(path, logoutPath, StringComparison.OrdinalIgnoreCase);
    }
}
