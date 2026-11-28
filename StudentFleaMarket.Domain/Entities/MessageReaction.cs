namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Cảm xúc (react emoji) của một người dùng trên một tin nhắn.
/// Mỗi người dùng chỉ giữ tối đa 1 react cho mỗi tin nhắn (đổi emoji sẽ ghi đè,
/// nhấn lại cùng emoji sẽ gỡ bỏ).
/// </summary>
public class MessageReaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = null!;
    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Message Message { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
