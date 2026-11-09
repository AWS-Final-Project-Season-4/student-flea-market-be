using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trading;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// NHÓM 7 — YÊU CẦU ĐỔI ĐỒ (Exchange Request).
///
/// Luồng:
///   1. Người mua gửi yêu cầu đổi (POST) — chọn món đồ của bên kia muốn nhận
///      + món đồ của mình đem đổi lấy + tiền bù chênh lệch.
///   2. Chủ món đồ được nhận phản hồi (accept / reject).
///   3. Người gửi có thể hủy yêu cầu đang chờ.
///
/// Endpoint:
///   POST   /api/ExchangeRequests                    -> gửi yêu cầu đổi đồ
///   GET    /api/ExchangeRequests                    -> danh sách (lọc status/direction)
///   GET    /api/ExchangeRequests/{id}               -> chi tiết
///   POST   /api/ExchangeRequests/{id}/accept        -> chấp nhận (chủ món đồ được nhận)
///   POST   /api/ExchangeRequests/{id}/reject        -> từ chối  (chủ món đồ được nhận)
///   POST   /api/ExchangeRequests/{id}/cancel        -> hủy yêu cầu (người gửi)
/// </summary>
[ApiController]
[Route("api/ExchangeRequests")]
[Authorize(Roles = "User, Admin, Moderator")]
[Produces("application/json")]
public class ExchangeRequestsController : ControllerBase
{
    private readonly IExchangeRequestService _service;

    public ExchangeRequestsController(IExchangeRequestService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gửi yêu cầu đổi đồ.
    ///
    /// Người dùng nhập MÃ HÀNG (ItemCode, 8 ký tự A-Z0-9) của 2 món đồ:
    ///   - `requestedItemCode`: món đồ của bên kia muốn nhận (phải bật "cho phép đổi đồ").
    ///   - `offeredItemCode`: món đồ của chính bạn dùng để đề nghị đổi lấy.
    ///
    /// Mã không phân biệt hoa thường. Hệ thống tự tra mã -> tin đăng.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExchangeRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Danh sách yêu cầu đổi đồ liên quan tới bạn.
    /// Lọc: ?status=1 (đang chờ) &amp; ?direction=incoming|outgoing.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ExchangeRequestQueryDto query,
        CancellationToken ct)
    {
        var result = await _service.ListAsync(GetUserId(), query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một yêu cầu đổi đồ (chỉ 2 bên liên quan được xem).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Chấp nhận yêu cầu đổi đồ — chỉ chủ món đồ được nhận.
    /// Sau khi accept, người gửi có thể tạo đơn giao dịch đổi đồ.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromBody] RespondExchangeRequestDto? request,
        CancellationToken ct)
    {
        var result = await _service.AcceptAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Từ chối yêu cầu đổi đồ — chỉ chủ món đồ được nhận.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RespondExchangeRequestDto? request,
        CancellationToken ct)
    {
        var result = await _service.RejectAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Hủy yêu cầu đổi đồ đang chờ — chỉ người gửi.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _service.CancelAsync(GetUserId(), id, ct);
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
