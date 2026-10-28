using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG — phía quản trị viên.
///
/// Chức năng chính: CẤM (ban) và GỠ CẤM (unban) tài khoản.
///
/// Khi cấm:
///   - Tài khoản bị chặn đăng nhập ngay (401 `ACCOUNT_BANNED`).
///   - Toàn bộ phiên đăng nhập (refresh token) bị thu hồi.
///   - Quyền bán (nếu đang có) cũng bị thu hồi.
///
/// Endpoint:
///   GET  /api/Admin/users                            -> danh sách tài khoản
///   GET  /api/Admin/users/{userId}                   -> chi tiết tài khoản
///   POST /api/Admin/users/{userId}/ban               -> cấm tài khoản
///   POST /api/Admin/users/{userId}/unban             -> gỡ cấm
///
/// LƯU Ý: dự án KHÔNG có chức năng cấp / gỡ quyền quản trị.
/// Vai trò Admin được gán trực tiếp trong DB (tài khoản admin mặc định
/// được seed khi khởi tạo) — không có đường leo thang quyền qua API.
/// </summary>
[ApiController]
[Route("api/Admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>
    /// Danh sách tài khoản người dùng.
    /// Lọc theo status: 1 = Hoạt động, 2 = Chưa kích hoạt, 3 = Đã bị cấm.
    /// Tìm kiếm bằng keyword (họ tên / email / username).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _adminUserService.ListUsersAsync(status, keyword, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một tài khoản.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken ct)
    {
        var result = await _adminUserService.GetUserAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cấm tài khoản. Tài khoản bị chặn đăng nhập ngay lập tức.
    /// Body: { "reason": "Lý do cấm" }
    /// </summary>
    [HttpPost("{userId:guid}/ban")]
    public async Task<IActionResult> Ban(
        Guid userId,
        [FromBody] BanUserRequestDto? request,
        CancellationToken ct)
    {
        var result = await _adminUserService.BanUserAsync(GetUserId(), userId, request?.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gỡ cấm — tài khoản hoạt động trở lại và đăng nhập được bình thường.
    /// </summary>
    [HttpPost("{userId:guid}/unban")]
    public async Task<IActionResult> Unban(Guid userId, CancellationToken ct)
    {
        var result = await _adminUserService.UnbanUserAsync(GetUserId(), userId, ct);
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
