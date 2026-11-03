namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Đơn giao dịch (Transaction).
///
/// 2 loại giao dịch chính:
///   - Purchase (Type = 1): mua trực tiếp — người mua trả tiền, nhận món đồ.
///   - Exchange (Type = 2): hoàn tất đổi đồ — sinh ra từ một ExchangeRequest được Accept.
///   - Free (Type = 3): nhận miễn phí (giá 0).
///
/// Vòng đời: Pending -> Completed | Cancelled (xem TransactionStatus).
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Người khởi tạo đơn (người mua / người gửi yêu cầu đổi).</summary>
    public Guid InitiatorId { get; set; }

    /// <summary>Bên còn lại của giao dịch (người bán / người nhận yêu cầu đổi).</summary>
    public Guid CounterpartyId { get; set; }

    /// <summary>Món đồ chính của giao dịch (tin đăng được mua / tin được đổi).</summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Yêu cầu đổi đồ gốc (nếu đơn được tạo từ luồng đổi đồ — Type = Exchange).
    /// Null với đơn mua trực tiếp.
    /// </summary>
    public Guid? ExchangeRequestId { get; set; }

    /// <summary>
    /// TransactionType: 1 = Purchase, 2 = Exchange, 3 = Free.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// Tổng số tiền của đơn.
    /// - Purchase: bằng giá bán của tin đăng.
    /// - Exchange: bằng AdditionalAmount của yêu cầu đổi đồ (tiền bù chênh lệch).
    /// - Free: 0.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// TransactionStatus: 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Completed.
    /// </summary>
    public int Status { get; set; }

    /// <summary>Địa điểm gặp mặt để giao dịch trực tiếp (vd: "KTX Khu A").</summary>
    public string MeetingLocation { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm hoàn thành giao dịch.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Thời điểm hủy giao dịch.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Lý do hủy giao dịch (do bên hủy cung cấp, không bắt buộc).
    /// </summary>
    public string? CancelReason { get; set; }

    // Navigation Properties
    public ApplicationUser Initiator { get; set; } = null!;
    public ApplicationUser Counterparty { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public ExchangeRequest? ExchangeRequest { get; set; }
    public Payment? Payment { get; set; }
    public Review? Review { get; set; }
}
