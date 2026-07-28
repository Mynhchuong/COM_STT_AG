namespace COM_STT_API.Models;

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public EmployeeInfo? Employee { get; set; }

    public static LoginResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static LoginResult Ok(EmployeeInfo employee) => new() { Success = true, Employee = employee };
}
