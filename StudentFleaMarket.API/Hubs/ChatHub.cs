using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Chat;

namespace StudentFleaMarket.API.Hubs;

/// <summary>
/// Hub SignalR cho trò chuyện trực tiếp (chat 1-1 theo món đồ).
///
/// ENDPOINT: /hubs/chat
///
/// XÁC THỰC
/// ---------
/// Browser KHÔNG set được header "Authorization" khi mở WebSocket, nên
/// client phải truyền token qua query string:
///     /hubs/chat?access_token=&lt;jwt&gt;
/// Phần đọc query string này đã được cấu hình trong
/// <c>JwtBearerEvents.OnMessageReceived</c> (Program.cs).
///
/// NHÓM (GROUPS)
/// -------------
///   conversation:{id} : 2 bên của hội thoại — nhận tin nhắn + trạng thái đã đọc.
///   user:{userId}     : 1 người (mọi thiết bị) — nhận tổng số tin chưa đọc.
///
/// SERVER -> CLIENT
/// ----------------
///   ReceiveMessage   : tin nhắn mới (MessageDto)
///   MessageRead      : đối tác đã đọc tin ({ byUserId, readAt })
///   ConversationUpdated : preview hội thoại thay đổi
///   NotifyUnreadCount: tổng số tin chưa đọc (badge toàn app)
/// </summary>
[Authorize(Roles = "User, Admin, Moderator")]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IUserRepository userRepo,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _userRepo = userRepo;
        _logger = logger;
    }

    /// <summary>
    /// Thiết lập khi client kết nối.
    ///
    /// Lưu ý: các middleware HTTP (BannedUserBlockMiddleware, RoleSyncMiddleware)
    /// KHÔNG áp dụng cho WebSocket của SignalR — handshake không đi qua
    /// pipeline HTTP. Vì vậy phải tự kiểm tra tài khoản bị cấm ở đây.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        if (userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        var status = await _userRepo.GetStatusAsync(userId, Context.ConnectionAborted);
        if (status.HasValue && status.Value.Status == (int)Domain.Enums.UserStatus.Banned)
        {
            _logger.LogWarning("Tài khoản bị cấm cố kết nối SignalR: {UserId}", userId);
            Context.Abort();
            return;
        }

        // Group cá nhân: để nhận badge unread dù đang ở màn hình khác.
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();

        // Đồng bộ badge ngay khi mở app.
        var totalUnread = await _chatService.GetTotalUnreadAsync(userId, Context.ConnectionAborted);
        await Clients.Caller.SendAsync("NotifyUnreadCount", new { totalUnread });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR disconnect: {ConnectionId}, lỗi: {Error}",
            Context.ConnectionId,
            exception?.Message ?? "(không)");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Tham gia nhóm của một hội thoại để nhận tin nhắn trực tiếp.
    /// Chỉ 2 bên của hội thoại mới được join.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();

        ConversationDto conversation;
        try
        {
            // GetConversationAsync tự kiểm tra quyền tham gia -> ném 403 nếu
            // user không phải Buyer/Seller của hội thoại này.
            conversation = await _chatService.GetConversationAsync(userId, conversationId, Context.ConnectionAborted);
        }
        catch (AppException ex)
        {
            await Clients.Caller.SendAsync("ChatError", new
            {
                conversationId,
                code = ex.Code,
                message = ex.Message
            });

            throw new HubException(ex.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

        await Clients.Caller.SendAsync("JoinedConversation", new
        {
            conversation.Id,
            conversation.Item,
            conversation.OtherParty,
            conversation.ViewerIsBuyer,
            conversation.LastMessage,
            conversation.LastMessageAt,
            conversation.UnreadCount,
            conversation.IsItemUnavailable
        });
    }

    /// <summary>Rời nhóm của hội thoại (thường khi đóng màn hình chat).</summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    /// <summary>
    /// Gửi 1 tin nhắn vào hội thoại.
    ///
    /// Lưu tin vào DB trước (nguồn sự thật), sau đó đẩy realtime cho cả 2 bên.
    /// Nếu gửi qua REST cũng gọi chung service nên logic không lệch.
    /// </summary>
    public async Task SendMessage(Guid conversationId, string content)
    {
        var userId = GetUserId();

        MessageDto message;
        try
        {
            message = await _chatService.SendMessageAsync(
                userId,
                conversationId,
                new SendMessageDto { Content = content },
                Context.ConnectionAborted);
        }
        catch (AppException ex)
        {
            // SignalR mặc định giấu message lỗi khi client không gửi
            // invocationId -> client chỉ thấy "Unknown error". Vì vậy bắt lỗi
            // nghiệp vụ ở đây và gửi event rõ ràng cho client.
            await Clients.Caller.SendAsync("ChatError", new
            {
                conversationId,
                code = ex.Code,
                message = ex.Message
            });

            throw new HubException(ex.Message);
        }

        // Đẩy tới cả 2 bên (kể cả người gửi, để client khác của họ cập nhật).
        await Clients.Group(ConversationGroup(conversationId)).SendAsync("ReceiveMessage", message);

        await PushConversationStateAsync(conversationId);
    }

    /// <summary>Đánh dấu đã đọc các tin của đối tác trong hội thoại.</summary>
    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = GetUserId();

        var result = await _chatService.MarkAsReadAsync(userId, conversationId, Context.ConnectionAborted);

        // Bên kia thấy trạng thái "đã đọc".
        await Clients.Group(ConversationGroup(conversationId)).SendAsync("MessageRead", new
        {
            conversationId,
            byUserId = userId,
            readAt = DateTime.UtcNow
        });

        await Clients.Caller.SendAsync("NotifyUnreadCount", new { totalUnread = result.TotalUnread });
    }

    /// <summary>
    /// Đánh dấu "đang gõ" — thông báo cho bên kia (không lưu vào DB).
    /// </summary>
    public async Task Typing(Guid conversationId)
    {
        var userId = GetUserId();

        await Clients.OthersInGroup(ConversationGroup(conversationId)).SendAsync("UserTyping", new
        {
            conversationId,
            userId
        });
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Đẩy trạng thái mới của hội thoại tới cả 2 bên:
    ///   - <c>ConversationUpdated</c>: preview (tin mới nhất) cho danh sách hội thoại.
    ///   - <c>NotifyUnreadCount</c>: tổng số tin chưa đọc cho badge toàn app.
    ///
    /// Gộp vào 1 hàm vì cả hai đều cần biết BuyerId / SellerId — tránh gọi
    /// GetConversationAsync 2 lần.
    /// </summary>
    private async Task PushConversationStateAsync(Guid conversationId)
    {
        try
        {
            var conversation = await _chatService.GetConversationAsync(
                GetUserId(), conversationId, Context.ConnectionAborted);

            await Clients.Group(ConversationGroup(conversationId)).SendAsync("ConversationUpdated", new
            {
                conversation.Id,
                conversation.LastMessage,
                conversation.LastMessageAt
            });

            var buyerUnread = await _chatService.GetTotalUnreadAsync(conversation.BuyerId, Context.ConnectionAborted);
            var sellerUnread = await _chatService.GetTotalUnreadAsync(conversation.SellerId, Context.ConnectionAborted);

            await Clients.Group(UserGroup(conversation.BuyerId))
                .SendAsync("NotifyUnreadCount", new { totalUnread = buyerUnread });

            await Clients.Group(UserGroup(conversation.SellerId))
                .SendAsync("NotifyUnreadCount", new { totalUnread = sellerUnread });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không đẩy được trạng thái hội thoại cho {ConversationId}", conversationId);
        }
    }

    private static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

    private static string UserGroup(Guid userId) => $"user:{userId}";

    private Guid GetUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
    }
}
