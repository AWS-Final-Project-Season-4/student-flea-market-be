namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Vòng đời đơn giao dịch (Transaction).
///
/// Nhịp chính của một giao dịch:
///   Pending (Đang xử lý) -> Completed (Hoàn tất)
///                       -> Cancelled (Hủy giao dịch)
///
/// Đối với đơn ĐỔI ĐỒ (Exchange) có thêm 1 bước:
///   Pending -> AwaitingConfirmation (chờ bên kia xác nhận) -> Completed
///
/// Lý do thêm bước này:
///   Ở Exchange có 2 món đồ đi ngược chiều. Nếu chỉ 1 bên bấm "Hoàn tất"
///   thì bên đó có thể chiếm đoạt món của mình mà không cần gặp mặt —
///   đồng thời tạo ra một đánh giá 5 sao giả. Bắt buộc cả 2 bên xác nhận
///   thì đơn mới thực sự "hoàn thành" và mới mở khoá đánh giá.
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
    Completed = 5,

    /// <summary>
    /// Chờ xác nhận của bên còn lại (chỉ dùng cho đơn Exchange).
    /// Bên đã bấm "Hoàn tất" đã xác nhận, đang chờ bên kia xác nhận.
    /// </summary>
    AwaitingConfirmation = 6
}
