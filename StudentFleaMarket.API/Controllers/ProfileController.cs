using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Profile;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// Chỉ tài khoản đã đăng nhập (sinh viên / admin) mới xem & cập nhật hồ sơ.
// Tài khoản dịch vụ AI (role AiService, xác thực bằng API key) bị loại trừ.
[Authorize(Roles = "User, Admin")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Lấy hồ sơ sinh viên hiện tại.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId, ct);
        return Ok(profile);
    }

    /// <summary>
    /// Cập nhật hồ sơ: avatar, số điện thoại, tên trường, mã sinh viên, khu vực & ký túc xá.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken ct)
    {
        var userId = GetUserId();
        var profile = await _profileService.UpdateProfileAsync(userId, request, ct);
        return Ok(profile);
    }

    /// <summary>
    /// Đổi mật khẩu (sẽ thu hồi toàn bộ phiên đăng nhập).
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _profileService.ChangePasswordAsync(userId, request, ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("Invalid Token.");

        return userId;
    }
}
