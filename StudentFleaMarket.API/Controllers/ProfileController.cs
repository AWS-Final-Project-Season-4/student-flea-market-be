using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Profile;
using StudentFleaMarket.Application.Media;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// Chỉ tài khoản đã đăng nhập (sinh viên / admin) mới xem & cập nhật hồ sơ.
// Tài khoản dịch vụ AI (role AiService, xác thực bằng API key) bị loại trừ.
[Authorize(Roles = "User, Admin, Moderator")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IAvatarUploadService _avatarUploadService;

    public ProfileController(IProfileService profileService, IAvatarUploadService avatarUploadService)
    {
        _profileService = profileService;
        _avatarUploadService = avatarUploadService;
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

    /// <summary>
    /// Xin presigned POST để upload ảnh đại diện thẳng lên S3 (jpg/png/webp, tối đa 5 MB).
    /// Lambda ImageProcessor cắt vuông và tạo <c>large.webp</c> (512px) + <c>thumbnail.webp</c> (128px).
    /// </summary>
    [HttpPost("avatar/presign")]
    public async Task<IActionResult> PresignAvatar([FromBody] PresignAvatarRequest request, CancellationToken ct)
    {
        var result = await _avatarUploadService.CreatePresignedUploadAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xác nhận ảnh đại diện đã upload xong và lưu URL vào hồ sơ.
    /// Body: <c>{ "objectKey": "raw/users/{userId}/avatars/{uploadId}.jpg" }</c> — lấy từ response presign.
    /// </summary>
    [HttpPost("avatar/confirm")]
    public async Task<IActionResult> ConfirmAvatar([FromBody] ConfirmAvatarRequest request, CancellationToken ct)
    {
        var profile = await _avatarUploadService.ConfirmUploadAsync(GetUserId(), request, ct);
        return Ok(profile);
    }

    /// <summary>Gỡ ảnh đại diện.</summary>
    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct)
    {
        var profile = await _avatarUploadService.RemoveAsync(GetUserId(), ct);
        return Ok(profile);
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
