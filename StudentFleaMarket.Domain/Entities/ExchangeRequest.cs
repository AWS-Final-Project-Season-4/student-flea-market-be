namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Yêu cầu đổi đồ (Exchange Request).
///
/// Ý nghĩa:
///   - Requester là người MUỐN đổi (người gửi yêu cầu).
///   - RequestedItem là món đồ của BÊN KIA mà requester muốn nhận.
///   - OfferedItem là món đồ của CHÍNH REQUESTER dùng để đề nghị đổi lấy.
///
/// Ví dụ: Minh muốn đổi lấy "Bàn phím cơ" của Tu.
///   Requester     = Tu  (chủ của Bàn phím cơ, nhận yêu cầu)
///   RequestedItem = Bàn phím cơ  (của Tu)
///   OfferedItem   = Sách Lập trình C#  (của Minh — thứ Minh đem đổi)
///
/// Người gửi yêu cầu (Minh) là chủ của OfferedItem.
/// Người nhận yêu cầu (Tu) là chủ của RequestedItem -> chỉ Tu mới accept/reject.
/// </summary>
public class ExchangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Người gửi yêu cầu đổi đồ (chủ của OfferedItem).</summary>
    public Guid RequesterId { get; set; }

    /// <summary>Món đồ của bên kia mà người gửi muốn nhận (chủ: người nhận yêu cầu).</summary>
    public Guid RequestedItemId { get; set; }

    /// <summary>Món đồ của người gửi dùng để đề nghị đổi lấy.</summary>
    public Guid OfferedItemId { get; set; }

    /// <summary>
    /// Số tiền bù chênh lệch. Người gửi trả thêm cho người nhận khi
    /// OfferedItem có giá trị thấp hơn RequestedItem.
    /// </summary>
    public decimal AdditionalAmount { get; set; } = 0;

    /// <summary>
    /// Ghi chú thương lượng của người gửi (vd: "Tôi có thể giao lúc 17h ở KTX A").
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Ghi chú phản hồi của người nhận yêu cầu (khi accept hoặc reject).
    /// </summary>
    public string? ResponseNote { get; set; }

    /// <summary>
    /// ExchangeRequestStatus: 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Expired.
    /// </summary>
    public int Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm người nhận phản hồi (accept / reject) — null nếu còn chờ.</summary>
    public DateTime? RespondedAt { get; set; }

    // Navigation Properties
    public ApplicationUser Requester { get; set; } = null!;
    public Item RequestedItem { get; set; } = null!;
    public Item OfferedItem { get; set; } = null!;
}
