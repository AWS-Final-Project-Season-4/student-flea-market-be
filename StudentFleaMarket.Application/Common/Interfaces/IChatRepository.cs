using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Repository riêng cho Conversation + Message (chat 1-1 theo món đồ).
/// </summary>
public interface IChatRepository : IRepository<Conversation>
{
    /// <summary>
    /// Tìm hội thoại của một người mua về một món đồ.
    /// Dùng cho logic "get-or-create": nếu đã có thì trả về hội thoại cũ.
    /// </summary>
    Task<Conversation?> GetByItemAndBuyerAsync(Guid itemId, Guid buyerId, CancellationToken ct = default);

    /// <summary>
    /// Lấy hội thoại kèm quan hệ (Item, Buyer, Seller, Messages) — dùng để
    /// kiểm tra quyền tham gia và dựng DTO.
    /// </summary>
    Task<Conversation?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Danh sách hội thoại của một người dùng (người mua HOẶC người bán),
    /// sắp xếp theo tin mới nhất, phân trang.
    ///
    /// IgnoreQueryFilters: giữ cả hội thoại gắn với món đồ đã bị gỡ /
    /// soft delete — lịch sử chat phải được giữ lại.
    /// </summary>
    Task<(List<Conversation> Conversations, int TotalCount)> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Tổng số tin chưa đọc của một người dùng trên TẤT CẢ hội thoại
    /// (dùng cho badge toàn app).
    /// </summary>
    Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy lịch sử tin nhắn của một hội thoại, mới nhất trước, phân trang.
    /// </summary>
    Task<(List<Message> Messages, int TotalCount)> ListMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Đánh dấu đã đọc TẤT CẢ tin của đối tác trong hội thoại.
    /// Trả về số tin vừa được đánh dấu.
    /// </summary>
    Task<int> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerId,
        DateTime readAt,
        CancellationToken ct = default);

    /// <summary>
    /// Tăng số tin chưa đọc của phía nhận (atomic — tránh mất update khi
    /// có nhiều tin đến gần như đồng thời).
    /// </summary>
    Task IncrementUnreadAsync(Guid conversationId, Guid recipientId, CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho Message.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    /// <summary>
    /// Lấy tin nhắn kèm quan hệ Sender — dùng để dựng MessageDto.
    /// </summary>
    Task<List<Message>> ListWithSenderAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy tin nhắn vừa tạo kèm Sender (để trả về ngay sau khi gửi).
    /// </summary>
    Task<Message?> GetWithSenderAsync(Guid id, CancellationToken ct = default);
}
