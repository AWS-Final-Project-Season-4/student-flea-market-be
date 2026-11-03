namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Vòng đời đơn giao dịch (Transaction).
///
/// Nhịp chính của một giao dịch:
///   Pending (Đang xử lý) -> Completed (Hoàn thành)
///                       -> Cancelled (Hủy giao dịch)
///
/// Accepted / Rejected là các trạng thái phụ (đã thỏa thuận xong / đã từ chối)
/// dùng cho các bước xác nhận trước khi vào vòng đời giao dịch.
/// </summary>
public enum TransactionStatus
{
    /// <summary>Đang xử lý — đơn đã khởi tạo, chờ giao dịch hoàn tất.</summary>
    Pending = 1,

    /// <summary>Đã chấp nhận / đã thống nhất điều khoản.</summary>
    Accepted = 2,

    /// <summary>Đã từ chối.</summary>
    Rejected = 3,

    /// <summary>Hủy giao dịch.</summary>
    Cancelled = 4,

    /// <summary>Hoàn thành — hàng đã giao, tiền đã thanh toán.</summary>
    Completed = 5
}
