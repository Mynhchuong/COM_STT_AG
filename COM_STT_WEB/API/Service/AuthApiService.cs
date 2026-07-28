using System.Net.Http.Json;
using COM_STT_WEB.Models.Account;

namespace COM_STT_WEB.API.Service;

public class LoginApiResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public EmployeeInfo? Employee { get; set; }
}

public class AuthApiService
{
    private readonly HttpClient _httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginApiResult> LoginAsync(string empCd, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { EmpCd = empCd, Password = password });

        if (response.IsSuccessStatusCode)
        {
            var employee = await response.Content.ReadFromJsonAsync<EmployeeInfo>();
            return new LoginApiResult { Success = true, Employee = employee };
        }

        string? message = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            error?.TryGetValue("message", out message);
        }
        catch
        {
            // ignore parse errors, fall back to generic message
        }

        return new LoginApiResult
        {
            Success = false,
            ErrorMessage = message ?? "Không thể đăng nhập, vui lòng thử lại."
        };
    }

    public async Task<ChangePasswordApiResult> ChangePasswordAsync(string empCd, string oldPassword, string newPassword)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", new
        {
            EmpCd = empCd,
            OldPassword = oldPassword,
            NewPassword = newPassword
        });

        if (response.IsSuccessStatusCode)
            return new ChangePasswordApiResult { Success = true };

        string? message = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            error?.TryGetValue("message", out message);
        }
        catch
        {
            // ignore parse errors, fall back to generic message
        }

        return new ChangePasswordApiResult
        {
            Success = false,
            ErrorMessage = message ?? "Không thể đổi mật khẩu, vui lòng thử lại."
        };
    }
}

public class ChangePasswordApiResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
