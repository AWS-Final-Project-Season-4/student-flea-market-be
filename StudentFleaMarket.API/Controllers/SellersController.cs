using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// HỒ SƠ NGƯỜI BÁN (public) — endpoint công khai.
///
/// Endpoint:
///   GET /api/Sellers/{sellerId}/profile -> hồ sơ năng lực bán hàng
///         (badge uy tín, điểm đánh giá, số giao dịch hoàn thành,
///          số vi phạm đã xác nhận, đánh giá gần đây)
///
/// VÌ SAO PUBLIC?
///   Người MUA cần biết "người này có đáng tin không" TRƯỚC khi gặp mặt —
///   kể cả khi họ chưa đăng nhập. Nếu bắt buộc đăng nhập thì người lạ chưa
///   có tài khoản sẽ không xem được thông tin quan trọng nhất.
///
/// Route dùng "Sellers" (số nhiều) để nhất quán với "Items" — endpoint
/// profile này là API công khai dùng bởi frontend, khác với "api/Seller"
/// (quản lý quyền bán của chính mình).
/// </summary>
[ApiController]
[Route("api/Sellers")]
[Authorize(Roles = "User, Admin, Moderator")]
[Produces("application/json")]
public class SellersController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public SellersController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    /// <summary>
    /// Hồ sơ năng lực bán hàng của một người bán.
    ///
    /// Trả về:
    ///   - `trust.level` / `trust.badge` / `trust.color`: mức uy tín.
    ///   - `trust.completedTransactionsCount`: số giao dịch đã hoàn thành.
    ///   - `trust.ratingAverage` / `trust.ratingCount`: điểm đánh giá.
    ///   - `trust.confirmedReportCount`: số vi phạm đã được xác nhận.
    ///   - `recentReviews`: các đánh giá gần đây.
    /// </summary>
    [HttpGet("{sellerId:guid}/profile")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(Guid sellerId, CancellationToken ct)
    {
        var result = await _sellerService.GetSellerProfileAsync(sellerId, GetOptionalUserId(), ct);
        return Ok(result);
    }

    private Guid? GetOptionalUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
