using COM_STT_API.Models;
using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.ValidateLoginAsync(request.EmpCd, request.Password);
        if (!result.Success)
            return Unauthorized(new { message = result.ErrorMessage });

        return Ok(result.Employee);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(request.EmpCd, request.OldPassword, request.NewPassword);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok();
    }
}
