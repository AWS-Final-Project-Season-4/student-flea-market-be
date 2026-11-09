using StudentFleaMarket.Application.DTOs.Chat;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Nghiệp vụ trò chuyện 1-1 giữa người mua và người bán, xoay quanh 1 món đồ.
///
/// Dùng chung cho cả 2 đường:
///   - REST  : POST /api/Chat/conversations/{id}/messages
///   - SignalR: ChatHub.SendMessage
///
/// Nhờ vậy business rule chỉ nằm 1 chỗ, không lệch logic giữa Hub và API.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Mở (hoặc tạo) hội thoại chat về một món đồ.
    ///
    /// - Nếu hội thoại đã tồn tại -> trả về hội thoại cũ (bỏ qua check trạng
    ///   thái món đồ, vì người dùng vẫn nhắn tin được).
    /// - Nếu chưa có -> chỉ tạo mới khi món đồ còn "đang bán".
    /// </summary>
    Task<ConversationDto> GetOrCreateConversationAsync(
        Guid userId,
        CreateConversationDto request,
        CancellationToken ct = default);

    /// <summary>Danh sách hội thoại của người dùng + tổng số tin chưa đọc.</summary>
    Task<ConversationListDto> ListConversationsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Chi tiết 1 hội thoại (chỉ 2 bên liên quan được xem).</summary>
    Task<ConversationDto> GetConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken ct = default);

    /// <summary>Lịch sử tin nhắn của một hội thoại (phân trang, mới nhất trước).</summary>
    Task<MessageListDto> ListMessagesAsync(
        Guid userId,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Gửi 1 tin nhắn mới vào hội thoại.</summary>
    Task<MessageDto> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        SendMessageDto request,
        CancellationToken ct = default);

    /// <summary>Đánh dấu đã đọc các tin của đối tác trong hội thoại.</summary>
    Task<MarkReadResultDto> MarkAsReadAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken ct = default);

    /// <summary>Tổng số tin chưa đọc của người dùng (dùng cho badge toàn app).</summary>
    Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default);
}
