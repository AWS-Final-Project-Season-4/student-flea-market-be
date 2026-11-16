using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// KIỂM DUYỆT VIÊN (Moderation) — phía Admin & Moderator.
///
/// Xử lý report vi phạm + bảng điều khiển kiểm duyệt.
///
/// Endpoint:
///   GET  /api/Admin/moderation/dashboard             -> thống kê tổng quan
///   GET  /api/Admin/moderation/reports              -> danh sách report
///   GET  /api/Admin/moderation/reports/{id}         -> chi tiết report
///   POST /api/Admin/moderation/reports/{id}/reviewing -> đánh dấu đang xem xét
///   POST /api/Admin/moderation/reports/{id}/resolve   -> xử lý report
///
/// HÀNH ĐỘNG XỬ LÝ (body `actions`):
///   - "reject"        : bác report (không có vi phạm) — không tính vào uy tín.
///   - "hide_item"     : ẩn tin đăng bị báo cáo.
///   - "ban_user"      : cấm tài khoản bị báo cáo.
///   - "revoke_seller" : thu hồi quyền bán.
///   - "void_review"   : vô hiệu đánh giá không hợp lệ trong đơn giao dịch.
///   - "none"          : xác nhận có vi phạm nhưng không cần hành động thêm.
///
/// KHÔNG được chọn "reject" cùng các hành động khác (mâu thuẫn logic).
/// </summary>
[ApiController]
[Route("api/Admin/moderation")]
[Authorize(Roles = "Admin, Moderator")]
[Produces("application/json")]
public class AdminModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public AdminModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// Thống kê tổng quan cho bảng điều khiển kiểm duyệt.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _moderationService.GetDashboardAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách report cho bảng kiểm duyệt.
    ///
    /// Lọc:
    ///   - `status`: 1 = Chờ xử lý, 2 = Đang xem xét, 3 = Đã xử lý, 4 = Đã bác.
    ///   - `reason`: 1 = Bùng hàng, 2 = Trả hàng sai, 3 = Không trả tiền,
    ///     4 = Quấy rối, 5 = Lừa đảo, 6 = Tin giả, 7 = Khác.
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> ListReports(
        [FromQuery] int? status,
        [FromQuery] int? reason,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _moderationService.ListReportsAsync(status, reason, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một report.
    /// </summary>
    [HttpGet("reports/{id:guid}")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var result = await _moderationService.GetReportAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu report đang được xem xét — để nhiều nhân viên không xử lý
    /// trùng cùng 1 report.
    /// </summary>
    [HttpPost("reports/{id:guid}/reviewing")]
    public async Task<IActionResult> MarkReviewing(Guid id, CancellationToken ct)
    {
        var result = await _moderationService.MarkReviewingAsync(GetUserId(), id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xử lý một report với các hành động được chọn.
    ///
    /// Body:
    /// {
    ///   "actions": ["hide_item", "void_review", "revoke_seller"],
    ///   "adminNote": "Đã liên hệ cả hai bên xác nhận người báo đúng."
    /// }
    /// </summary>
    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveReportDto request,
        CancellationToken ct)
    {
        var result = await _moderationService.ResolveAsync(GetUserId(), id, request, ct);
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
