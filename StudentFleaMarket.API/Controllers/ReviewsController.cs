using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// ĐÁNH GIÁ UY TÍN (Reviews) — phía người dùng.
///
/// Endpoint:
///   POST /api/Reviews/transactions/{transactionId}   -> viết đánh giá
///   PUT  /api/Reviews/{reviewId}                      -> sửa đánh giá (trong 14 ngày)
///   GET  /api/Reviews/{reviewId}                      -> xem đánh giá
///   GET  /api/Reviews/users/{userId}                  -> đánh giá nhận được của 1 người
///   GET  /api/Reviews/transactions/{transactionId}/eligibility -> có được đánh giá không
///   GET  /api/Reviews/pending                         -> các giao dịch chờ đánh giá
///
/// PHẠM VI ĐÁNH GIÁ:
///   - Đơn mua trực tiếp / nhận miễn phí : CHỈ người MUA đánh giá người BÁN.
///   - Đơn đổi đồ                      : CẢ 2 bên cùng đánh giá nhau.
/// </summary>
[ApiController]
[Route("api/Reviews")]
[Authorize(Roles = "User, Admin, Moderator")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Viết đánh giá cho một giao dịch đã hoàn thành.
    ///
    /// - `rating`: 1 → 5 sao.
    /// - `comment`: bình luận (không bắt buộc, tối đa 500 ký tự).
    /// </summary>
    [HttpPost("transactions/{transactionId:guid}")]
    public async Task<IActionResult> Create(
        Guid transactionId,
        [FromBody] CreateReviewDto request,
        CancellationToken ct)
    {
        var result = await _reviewService.CreateAsync(GetUserId(), transactionId, request, ct);
        return CreatedAtAction(nameof(GetById), new { reviewId = result.Id }, result);
    }

    /// <summary>
    /// Sửa đánh giá đã viết. Chỉ sửa được trong 14 ngày kể từ khi viết.
    /// </summary>
    [HttpPut("{reviewId:guid}")]
    public async Task<IActionResult> Update(
        Guid reviewId,
        [FromBody] UpdateReviewDto request,
        CancellationToken ct)
    {
        var result = await _reviewService.UpdateAsync(GetUserId(), reviewId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một đánh giá.
    /// </summary>
    [HttpGet("{reviewId:guid}")]
    public async Task<IActionResult> GetById(Guid reviewId, CancellationToken ct)
    {
        var result = await _reviewService.GetByIdAsync(reviewId, GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách đánh giá nhận được của một tài khoản (hiển thị trên profile).
    /// Chỉ trả về đánh giá hợp lệ (đánh giá bị vô hiệu do vi phạm không hiển thị).
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListForUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _reviewService.ListForUserAsync(userId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra bạn có được đánh giá giao dịch này không, và lý do nếu không.
    /// Dùng để frontend bật/tắt nút "Viết đánh giá".
    /// </summary>
    [HttpGet("transactions/{transactionId:guid}/eligibility")]
    public async Task<IActionResult> GetEligibility(Guid transactionId, CancellationToken ct)
    {
        var result = await _reviewService.GetEligibilityAsync(GetUserId(), transactionId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Các giao dịch đã hoàn thành mà bạn chưa đánh giá — dùng cho màn hình
    /// "Cần đánh giá" sau khi giao dịch xong.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> ListPending(CancellationToken ct)
    {
        var result = await _reviewService.ListPendingReviewsAsync(GetUserId(), ct);
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
