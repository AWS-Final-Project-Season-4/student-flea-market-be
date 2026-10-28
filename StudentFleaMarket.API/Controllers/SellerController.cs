using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Seller;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// NĂNG LỰC BÁN HÀNG (Seller) — phía người dùng.
///
/// "Trở thành người bán" KHÔNG phải là đăng ký tài khoản mới: người dùng
/// vẫn giữ tài khoản User và vẫn mua / yêu thích / sửa hồ sơ bình thường.
/// Chỉ khi Admin duyệt yêu cầu thì chức năng đăng tin bán mới được mở.
///
/// Luồng phía client:
///   1. GET  /api/Seller/status        -> xem trạng thái hiện tại
///   2. POST /api/Seller/request       -> gửi yêu cầu trở thành người bán
///   3. POST /api/Seller/request/cancel -> hủy yêu cầu đang chờ duyệt
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User, Admin")]
[Produces("application/json")]
public class SellerController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public SellerController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    /// <summary>
    /// Trạng thái quyền bán hàng của chính mình.
    /// Trả về canSell = true nghĩa là đã được phép đăng tin.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await _sellerService.GetStatusAsync(GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gửi yêu cầu trở thành người bán. Admin sẽ duyệt và cấp quyền đăng bán.
    /// Có thể gửi lại yêu cầu nếu trước đó bị từ chối.
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> SubmitRequest([FromBody] SellerRequestDto request, CancellationToken ct)
    {
        var result = await _sellerService.RequestAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Hủy yêu cầu trở thành người bán đang chờ Admin duyệt.
    /// </summary>
    [HttpPost("request/cancel")]
    public async Task<IActionResult> CancelRequest(CancellationToken ct)
    {
        var result = await _sellerService.CancelRequestAsync(GetUserId(), ct);
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
