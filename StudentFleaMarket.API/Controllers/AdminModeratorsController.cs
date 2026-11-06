using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// QUẢN LÝ VAI TRÒ KIỂM DUYỆT VIÊN (Moderator) — CHỈ Admin.
///
/// Moderator là kiểm duyệt viên, chia sẻ công việc với Admin khi Admin bận:
/// duyệt quyền bán, cấm/gỡ cấm tài khoản thường, gỡ tin đăng vi phạm.
///
/// CHỈ Admin được cấp / thu hồi role Moderator. Moderator KHÔNG được cấp
/// role cho người khác — đây là bảo đảm chống leo thang quyền (đúng lý do
/// dự án đã bỏ tính năng cấp quyền quản trị cho Admin).
///
/// Hướng cấp quyền LUÔN ĐI TỪ CAO XUỐNG THẤP: Admin -> Moderator -> User.
///
/// Endpoint:
///   GET    /api/Admin/moderators                    -> danh sách kiểm duyệt viên
///   POST   /api/Admin/moderators/{userId}           -> cấp role kiểm duyệt viên
///   DELETE /api/Admin/moderators/{userId}           -> thu hồi role kiểm duyệt viên
///
/// Hiệu lực tức thì: RoleSyncMiddleware đọc Role từ DB mỗi request, nên tài
/// khoản vừa được cấp quyền KHÔNG cần đăng nhập lại.
/// </summary>
[ApiController]
[Route("api/Admin/moderators")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminModeratorsController : ControllerBase
{
    private readonly IModeratorService _moderatorService;

    public AdminModeratorsController(IModeratorService moderatorService)
    {
        _moderatorService = moderatorService;
    }

    /// <summary>
    /// Danh sách các tài khoản đang là kiểm duyệt viên.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _moderatorService.ListAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cấp role kiểm duyệt viên cho một tài khoản thường.
    /// Body (tuỳ chọn): { "note": "Ghi chú về việc cấp quyền" }
    /// </summary>
    [HttpPost("{userId:guid}")]
    public async Task<IActionResult> Assign(
        Guid userId,
        [FromBody] AssignModeratorRequestDto? request,
        CancellationToken ct = default)
    {
        var result = await _moderatorService.AssignAsync(GetUserId(), userId, request?.Note, ct);
        return Ok(result);
    }

    /// <summary>
    /// Thu hồi role kiểm duyệt viên — tài khoản trở lại User thường.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Revoke(Guid userId, CancellationToken ct = default)
    {
        var result = await _moderatorService.RevokeAsync(GetUserId(), userId, ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("Token không hợp lệ.");

        return userId;
    }
}
