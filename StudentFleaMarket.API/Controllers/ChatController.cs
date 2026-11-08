using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Chat;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// NHÓM 10 — TRÒ CHUYỆN (Chat 1-1 theo món đồ).
///
/// Luồng:
///   1. Người mua bấm "Chat với người bán" trên trang chi tiết tin đăng
///      (POST) — hệ thống tạo hoặc trả về hội thoại đã có.
///   2. Xem danh sách hội thoại + badge số tin chưa đọc (GET).
///   3. Xem lịch sử tin nhắn (GET messages).
///   4. Gửi tin nhắn (POST messages) — đường dự phòng khi WebSocket lỗi.
///   5. Đánh dấu đã đọc (POST read) — reset badge.
///
/// Endpoint:
///   POST   /api/Chat/conversations                  -> mở / tạo hội thoại
///   GET    /api/Chat/conversations                  -> danh sách + tổng tin chưa đọc
///   GET    /api/Chat/conversations/{id}             -> chi tiết hội thoại
///   GET    /api/Chat/conversations/{id}/messages    -> lịch sử tin nhắn
///   POST   /api/Chat/conversations/{id}/messages    -> gửi tin nhắn
///   POST   /api/Chat/conversations/{id}/read        -> đánh dấu đã đọc
///
/// Realtime: dùng SignalR tại /hubs/chat (ChatHub) — cùng nghiệp vụ,
/// gọi chung IChatService nên không lệch logic.
/// </summary>
[ApiController]
[Route("api/Chat")]
[Authorize(Roles = "User, Admin, Moderator")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Mở (hoặc tạo) hội thoại chat về một món đồ.
    ///
    /// Gọi lại nhiều lần với cùng 1 món đồ luôn trả về cùng 1 hội thoại —
    /// không tạo trùng. Chỉ tạo MỚI được khi món đồ còn "đang bán".
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationDto request,
        CancellationToken ct)
    {
        var result = await _chatService.GetOrCreateConversationAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách hội thoại của bạn + tổng số tin chưa đọc (dùng cho badge).
    /// Sắp xếp theo tin mới nhất.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> ListConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _chatService.ListConversationsAsync(GetUserId(), page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết 1 hội thoại (chỉ 2 bên liên quan được xem).
    /// </summary>
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken ct)
    {
        var result = await _chatService.GetConversationAsync(GetUserId(), id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lịch sử tin nhắn của một hội thoại — mới nhất trước, phân trang.
    /// </summary>
    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> ListMessages(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _chatService.ListMessagesAsync(GetUserId(), id, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gửi 1 tin nhắn vào hội thoại.
    ///
    /// Đây là đường DỰ PHÒNG cho trường hợp WebSocket không hoạt động.
    /// Tin vẫn được lưu vào DB và vẫn được đẩy realtime tới các client
    /// đang kết nối.
    /// </summary>
    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageDto request,
        CancellationToken ct)
    {
        var result = await _chatService.SendMessageAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu đã đọc các tin của đối tác trong hội thoại.
    /// Reset badge của hội thoại này và trả về tổng số tin chưa đọc.
    /// </summary>
    [HttpPost("conversations/{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await _chatService.MarkAsReadAsync(GetUserId(), id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Tổng số tin chưa đọc của bạn (dùng cho badge toàn app).
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var total = await _chatService.GetTotalUnreadAsync(GetUserId(), ct);
        return Ok(new { totalUnread = total });
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
