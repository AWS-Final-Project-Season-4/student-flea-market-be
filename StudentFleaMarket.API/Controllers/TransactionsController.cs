using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trading;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// NHÓM 8 — GIAO DỊCH (Transactions) & THANH TOÁN.
///
/// Luồng:
///   1. Khởi tạo đơn: mua trực tiếp (BuyDirect) hoặc hoàn tất đổi đồ (Exchange).
///   2. Vòng đời đơn: Đang xử lý -> Hoàn thành (Completed) hoặc Hủy (Cancelled).
///   3. Thanh toán: COD (tiền mặt khi gặp) hoặc chuyển khoản QR / cổng thanh toán.
///
/// Endpoint:
///   POST /api/Transactions/purchase      -> khởi tạo đơn mua trực tiếp
///   POST /api/Transactions/exchange       -> khởi tạo đơn hoàn tất đổi đồ
///   GET  /api/Transactions                -> danh sách (lọc status/type/direction)
///   GET  /api/Transactions/{id}           -> chi tiết
///   POST /api/Transactions/{id}/complete  -> hoàn thành giao dịch
///   POST /api/Transactions/{id}/cancel    -> hủy giao dịch
///   POST /api/Transactions/{id}/payment    -> xác nhận đã thanh toán
/// </summary>
[ApiController]
[Route("api/Transactions")]
[Authorize(Roles = "User, Admin")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Khởi tạo đơn giao dịch mua trực tiếp (BuyDirect).
    ///
    /// - `itemCode`: mã hàng (8 ký tự A-Z0-9) của tin đăng muốn mua.
    /// - `meetingLocation`: địa điểm gặp mặt (bỏ trống -> lấy từ tin đăng).
    /// - `paymentMethod`: 1 = COD (tiền mặt khi gặp), 2 = chuyển khoản QR / cổng thanh toán.
    /// - `amount`: số tiền thực trả (bỏ trống -> giá niêm yết của tin đăng).
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> CreatePurchase(
        [FromBody] CreatePurchaseTransactionDto request,
        CancellationToken ct)
    {
        var result = await _service.CreatePurchaseAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Khởi tạo đơn giao dịch hoàn tất đổi đồ (Exchange).
    ///
    /// Yêu cầu đổi đồ phải đã được chấp nhận (accept) trước đó.
    /// </summary>
    [HttpPost("exchange")]
    public async Task<IActionResult> CreateExchange(
        [FromBody] CreateExchangeTransactionDto request,
        CancellationToken ct)
    {
        var result = await _service.CreateExchangeAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Danh sách đơn giao dịch liên quan tới bạn.
    /// Lọc: ?status=1 (đang xử lý) &amp; ?type=1 (mua trực tiếp) &amp; ?direction=incoming|outgoing.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TransactionQueryDto query,
        CancellationToken ct)
    {
        var result = await _service.ListAsync(GetUserId(), query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một đơn giao dịch (chỉ 2 bên liên quan được xem).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hoàn thành giao dịch — chuyển đơn sang trạng thái "Hoàn thành".
    /// Tin đăng tương ứng được đánh dấu "Đã bán".
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _service.CompleteAsync(GetUserId(), id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Hủy giao dịch — chuyển đơn sang trạng thái "Đã hủy giao dịch".
    /// Tin đăng được trả lại trạng thái "Đang bán".
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelTransactionDto? request,
        CancellationToken ct)
    {
        var result = await _service.CancelAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xác nhận đã thanh toán.
    ///
    /// - COD: báo "đã trả tiền mặt" khi gặp nhận đồ.
    /// - BankTransfer: báo "đã chuyển khoản" qua QR ngân hàng / cổng thanh toán.
    ///
    /// Chỉ người trả tiền (Initiator) mới xác nhận được.
    /// </summary>
    [HttpPost("{id:guid}/payment")]
    public async Task<IActionResult> ConfirmPayment(
        Guid id,
        [FromBody] ConfirmPaymentDto? request,
        CancellationToken ct)
    {
        var result = await _service.ConfirmPaymentAsync(GetUserId(), id, request, ct);
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
