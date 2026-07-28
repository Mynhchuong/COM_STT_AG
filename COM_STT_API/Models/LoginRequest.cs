namespace COM_STT_API.Models;

public class LoginRequest
{
    public string EmpCd { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
