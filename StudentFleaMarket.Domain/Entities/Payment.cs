namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Thông tin thanh toán của một đơn giao dịch.
///
/// Mỗi Transaction có tối đa 1 Payment (quan hệ 1-1).
/// - Method = PaymentMethod: 1 = Cod, 2 = BankTransfer.
/// - Method = Cod: Payment được tạo sẵn ở trạng thái Pending, người mua báo
///   "đã thanh toán tiền mặt" thì chuyển sang Paid khi giao dịch hoàn tất.
/// - Method = BankTransfer: người mua dán mã QR / nhập nội dung chuyển khoản,
///   hệ thống sinh TransactionRef để đối soát.
/// </summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }

    /// <summary>Người trả tiền (thường là Initiator của đơn).</summary>
    public Guid PayerId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// PaymentMethod: 1 = Cod (tiền mặt khi gặp), 2 = BankTransfer (QR / cổng thanh toán).
    /// </summary>
    public int Method { get; set; }

    /// <summary>
    /// PaymentStatus: 1 = Pending, 2 = Paid, 3 = Failed, 4 = Refunded.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Mã tham chiếu giao dịch (dùng để đối soát khi chuyển khoản).
    /// Với COD thì là mã đơn nội bộ.
    /// </summary>
    public string TransactionRef { get; set; } = null!;

    /// <summary>Thời điểm thanh toán thành công.</summary>
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Transaction Transaction { get; set; } = null!;
    public ApplicationUser Payer { get; set; } = null!;
}
