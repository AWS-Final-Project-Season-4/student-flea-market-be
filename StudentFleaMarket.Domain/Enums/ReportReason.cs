namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Lý do người dùng báo cáo (bài đăng hoặc tài khoản).
///
/// Dùng cho <see cref="Entities.Report.Reason"/> — giá trị int trong DB,
/// tương tự <see cref="ReportStatus"/>.
///
/// Lưu ý: khi thêm giá trị mới, chỉ APPEND vào cuối để không làm lệch
/// các bản ghi đã lưu trong DB.
/// </summary>
public enum ReportReason
{
    /// <summary>Bùng hàng — không ra giao dịch dù đã hẹn.</summary>
    NoShow = 1,

    /// <summary>Trả hàng sai / không đúng mô tả / không đúng hình ảnh đăng.</summary>
    ItemMismatch = 2,

    /// <summary>Không trả tiền (đã nhận hàng nhưng không thanh toán).</summary>
    NotPaid = 3,

    /// <summary>Quấy rối, lăng nhăng, spam tin nhắn.</summary>
    Harassment = 4,

    /// <summary>Lừa đảo, giả mạo, đòi tiền trước rồi biến mất.</summary>
    Fraud = 5,

    /// <summary>Bài đăng sai sự thật (không đúng món đồ thật).</summary>
    FakeListing = 6,

    /// <summary>Vi phạm khác — mô tả chi tiết trong Description.</summary>
    Other = 7
}
