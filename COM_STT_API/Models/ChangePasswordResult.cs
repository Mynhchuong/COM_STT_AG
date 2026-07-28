namespace COM_STT_API.Models;

public class ChangePasswordResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static ChangePasswordResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static ChangePasswordResult Ok() => new() { Success = true };
}
