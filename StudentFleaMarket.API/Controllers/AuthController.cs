using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng ký tài khoản mới (sinh viên / học sinh). Hỗ trợ email thông thường hoặc email trường.
    /// Không trả token — cần xác thực email qua OTP trước khi đăng nhập.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đăng nhập, trả về Access Token + Refresh Token.
    /// Định danh đăng nhập nhận cả username ("admin") lẫn email ("admin@gmail.com").
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đổi Access Token bằng Refresh Token (token rotation).
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đăng xuất — thu hồi Refresh Token.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return Ok(new { Message = "Logged out successfully" });
    }

    /// <summary>
    /// Gửi mã OTP xác thực email.
    /// </summary>
    [HttpPost("send-verification-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequestDto request, CancellationToken ct)
    {
        var result = await _authService.SendVerificationCodeAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gửi lại mã OTP xác thực email.
    /// </summary>
    [HttpPost("resend-verification-code")]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeRequestDto request, CancellationToken ct)
    {
        var result = await _authService.ResendVerificationCodeAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xác thực email bằng mã OTP 6 chữ số.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken ct)
    {
        var result = await _authService.VerifyEmailAsync(request, ct);
        return Ok(result);
    }
}
