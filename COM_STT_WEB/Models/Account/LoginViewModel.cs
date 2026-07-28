using System.ComponentModel.DataAnnotations;

namespace COM_STT_WEB.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
    [Display(Name = "Mã nhân viên")]
    public string EmpCd { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập trên thiết bị này")]
    public bool RememberMe { get; set; } = true;
}
