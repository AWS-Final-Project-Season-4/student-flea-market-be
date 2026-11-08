using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// BÁO CÁO VI PHẠM (Reports) — phía người dùng.
///
/// 3 dạng báo cáo:
///   1. Báo cáo BÀI ĐĂNG   — body: { reason, description, itemCode }
///   2. Báo cáo GIAO DỊCH   — body: { reason, description, transactionId, itemCode? }
///   3. Báo cáo HỘI THOẠI  — body: { reason, description, conversationId }
///
/// Endpoint:
///   POST /api/Reports          -> gửi report
///   GET  /api/Reports          -> các report của chính mình (theo dõi kết quả)
///   GET  /api/Reports/{id}     -> chi tiết report của mình
/// </summary>
[ApiController]
[Route("api/Reports")]
[Authorize(Roles = "User, Admin, Moderator")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Gửi report vi phạm lên hệ thống.
    ///
    /// - `reason`: 1 = Bùng hàng, 2 = Trả hàng sai, 3 = Không trả tiền,
    ///   4 = Quấy rối, 5 = Lừa đảo, 6 = Tin giả, 7 = Khác.
    /// - `description`: mô tả chi tiết (bắt buộc, tối thiểu 10 ký tự).
    /// - `itemCode`: mã hàng 8 ký tự của tin đăng bị báo cáo (bắt buộc khi
    ///   báo cáo bài đăng, tuỳ chọn khi báo cáo trong giao dịch).
    /// - `transactionId`: id đơn giao dịch (bắt buộc khi báo cáo bùng hàng /
    ///   tráo hàng / không trả tiền).
    /// - `conversationId`: id hội thoại (bắt buộc khi báo cáo quấy rối).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportDto request,
        CancellationToken ct)
    {
        var result = await _reportService.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Danh sách report do chính bạn gửi — theo dõi kết quả xử lý.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _reportService.ListMineAsync(GetUserId(), page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết một report do chính bạn gửi.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _reportService.GetMineAsync(GetUserId(), id, ct);
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
