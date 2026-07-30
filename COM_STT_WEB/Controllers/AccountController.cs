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

        // RememberMe = false -> cookie chỉ tồn tại trong phiên trình duyệt, và bị ép hết hạn lúc
        //   nửa đêm hôm nay (không gia hạn theo hoạt động) -> bắt đăng nhập lại mỗi ngày.
        // RememberMe = true  -> cookie tồn tại 30 ngày kể từ lúc đăng nhập, kể cả khi đóng/mở lại
        //   trình duyệt, không cần đăng nhập lại mỗi ngày trong khoảng thời gian đó.
        var expiresUtc = model.RememberMe
            ? DateTimeOffset.UtcNow.AddDays(30)
            : DateTimeOffset.Now.Date.AddDays(1).ToUniversalTime();

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = expiresUtc,
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

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var empCd = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authApiService.ChangePasswordAsync(empCd, model.OldPassword, model.NewPassword);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đổi mật khẩu thất bại.");
            return View(model);
        }

        // Đổi mật khẩu xong bắt đăng nhập lại bằng mật khẩu mới — vừa an toàn hơn, vừa xác nhận luôn mật khẩu mới đúng.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
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
