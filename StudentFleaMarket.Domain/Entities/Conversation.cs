namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Cuộc trò chuyện 1-1 giữa người mua và người bán, xoay quanh 1 món đồ.
///
/// Ý nghĩa:
///   - Buyer  = người bấm "Chat với người bán" (luôn là người mua tiềm năng).
///   - Seller = CHỦ của món đồ (luôn suy ra từ Item.SellerId, không lấy từ
///     request để tránh chat nhầm người).
///
/// Bất biến:
///   - 1 món đồ + 1 người mua = ĐÚNG 1 hội thoại (unique index trên
///     ItemId + BuyerId). Bấm "Chat" nhiều lần vẫn trả về cùng 1 hội thoại.
///   - Chat KHÔNG bị đóng khi món đồ hết trạng thái "đang bán" — người dùng
///     vẫn nhắn tin được để hỏi lý do / thương lượng tiếp. Trạng thái
///     "khả dụng" được suy ra lúc đọc từ Item.Status + Item.IsDeleted.
///   - Lịch sử chat được giữ lại kể cả khi món đồ bị gỡ / soft delete
///     (FK Item dùng DeleteBehavior.Restrict).
///
/// Đếm tin chưa đọc:
///   - BuyerUnreadCount  = số tin NGƯỜI BÁN gửi mà Buyer chưa đọc.
///   - SellerUnreadCount = số tin NGƯỜI MUA gửi mà Seller chưa đọc.
///   Hai cột tách biệt vì đếm theo HƯỚNG NHẬN, không phải theo người gửi.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }

    /// <summary>
    /// Nội dung xem trước của tin nhắn mới nhất (bị cắt ngắn ~120 ký tự).
    /// Dùng cho danh sách hội thoại mà không cần join bảng Messages.
    /// </summary>
    public string? LastMessage { get; set; }

    /// <summary>Thời điểm tin nhắn mới nhất được gửi — dùng để sắp xếp danh sách.</summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>Số tin của người bán mà người mua chưa đọc.</summary>
    public int BuyerUnreadCount { get; set; }

    /// <summary>Số tin của người mua mà người bán chưa đọc.</summary>
    public int SellerUnreadCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Item Item { get; set; } = null!;
    public ApplicationUser Buyer { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
