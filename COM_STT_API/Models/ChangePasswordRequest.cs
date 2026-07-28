namespace COM_STT_API.Models;

public class ChangePasswordRequest
{
    public string EmpCd { get; set; } = string.Empty;
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
