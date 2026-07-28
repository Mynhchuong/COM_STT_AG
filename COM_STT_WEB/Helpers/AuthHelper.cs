using System.Security.Claims;
using COM_STT_WEB.Models.Account;

namespace COM_STT_WEB.Helpers;

public static class AuthHelper
{
    public static EmployeeInfo? GetCurrentUser(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;

        var empCd = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(empCd)) return null;

        return new EmployeeInfo
        {
            EmpCd = empCd,
            FullName = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            DeptCd = principal.FindFirst("DeptCd")?.Value ?? string.Empty,
            DeptNm = principal.FindFirst("DeptNm")?.Value,
            LineCd = principal.FindFirst("LineCd")?.Value,
        };
    }
}
