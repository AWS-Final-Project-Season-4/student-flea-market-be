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

    /// <summary>Tin nhắn gốc được trích dẫn khi trả lời (nullable).</summary>
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>Thời điểm chỉnh sửa nội dung lần cuối (null = chưa chỉnh sửa).</summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>Đánh dấu tin nhắn đang được ghim trong hội thoại.</summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>Thời điểm ghim tin nhắn.</summary>
    public DateTime? PinnedAt { get; set; }

    /// <summary>Người thực hiện ghim tin nhắn.</summary>
    public Guid? PinnedById { get; set; }

    // Navigation Properties
    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
    public Message? ReplyToMessage { get; set; }
    public ApplicationUser? PinnedBy { get; set; }
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}
