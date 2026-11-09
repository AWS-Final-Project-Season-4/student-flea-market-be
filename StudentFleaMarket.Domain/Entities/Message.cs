namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Một tin nhắn trong cuộc trò chuyện (Conversation).
///
/// Lưu ý:
///   - <see cref="IsRead"/> là nguồn sự thật chính cho trạng thái đã đọc.
///   - <see cref="ReadAt"/> là thời điểm đọc (nullable) — dùng để hiển thị
///     "đã xem lúc HH:mm"; có thể bỏ trống nếu không cần.
///   - <see cref="IsDeleted"/> cho soft delete — xoá tin không mất lịch sử
///     hội thoại, và <c>Conversation.LastMessage</c> phải hiển thị lại là
///     "Tin nhắn đã bị xoá" thay vì để lộ nội dung.
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    /// <summary>Thời điểm tin nhắn được đánh dấu đã đọc (null = chưa đọc).</summary>
    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}
