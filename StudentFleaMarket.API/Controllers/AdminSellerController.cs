using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Seller;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// Quản lý quyền BÁN HÀNG (Seller) — phía quản trị viên.
///
/// Admin là bên cấp quyền đăng bán cho người dùng. Trước khi được duyệt,
/// tài khoản đó vẫn mua / yêu thích bình thường nhưng không đăng tin được.
///
/// Endpoint:
///   GET  /api/Admin/seller-requests                     -> danh sách yêu cầu
///   POST /api/Admin/seller-requests/{userId}/approve    -> cấp quyền bán
///   POST /api/Admin/seller-requests/{userId}/reject     -> từ chối
///   POST /api/Admin/seller-requests/{userId}/revoke     -> thu hồi quyền bán
/// </summary>
[ApiController]
[Route("api/Admin/seller-requests")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminSellerController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public AdminSellerController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    /// <summary>
    /// Danh sách yêu cầu trở thành người bán.
    /// Lọc theo status: 1 = Pending, 2 = Approved, 3 = Rejected (bỏ trống = tất cả).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListRequests(
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sellerService.ListRequestsAsync(status, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cấp quyền đăng bán cho một tài khoản.
    /// </summary>
    [HttpPost("{userId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid userId, CancellationToken ct)
    {
        var result = await _sellerService.ApproveAsync(GetUserId(), userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Từ chối yêu cầu trở thành người bán (có thể kèm lý do).
    /// </summary>
    [HttpPost("{userId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid userId,
        [FromBody] RejectSellerRequestDto? request,
        CancellationToken ct)
    {
        var result = await _sellerService.RejectAsync(GetUserId(), userId, request?.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Thu hồi quyền đăng bán đã cấp (tài khoản về trạng thái chưa được cấp quyền).
    /// </summary>
    [HttpPost("{userId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid userId, CancellationToken ct)
    {
        var result = await _sellerService.RevokeAsync(GetUserId(), userId, ct);
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
