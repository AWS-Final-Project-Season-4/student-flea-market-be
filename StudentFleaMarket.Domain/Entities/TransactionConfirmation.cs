namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Xác nhận bàn giao của một đơn giao dịch (dùng cho đơn ĐỔI ĐỒ).
///
/// VÌ SAO CẦN BẢNG NÀY?
///   Ở đơn Exchange có 2 món đồ đi ngược chiều. Nếu chỉ 1 bên bấm
///   "Hoàn tất" thì bên đó có thể chiếm đoạt món của mình mà không cần gặp
///   mặt — đồng thời tạo ra một lời đánh giá 5 sao giả. Vì vậy đơn Exchange bắt
///   buộc CẢ 2 BÊN phải xác nhận thì đơn mới thực sự "hoàn thành".
///
/// Mỗi bên xác nhận 1 lần (unique index trên TransactionId + UserId).
///
/// <see cref="ItemCodeVerified"/> là cơ chế xác minh RẺ và HIỆU QUẢ nhất:
///   Bên nhận nhập mã hàng (8 ký tự) của món vừa nhận; hệ thống so với mã
///   hàng của món ĐÁNG LẼ phải nhận. Kẻ mang món khác đến (hành vi tráo hàng
///   phổ biến nhất) sẽ bị phát hiện NGAY TẠI CHỖ, không cần Moderator.
///
/// <see cref="ItemReceivedOk"/> cho phép bên nhận báo "món không đúng mô tả"
/// (đúng món nhưng hỏng / sai thông tin) -> gợi ý tạo report.
/// </summary>
public class TransactionConfirmation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Đơn giao dịch được xác nhận.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Bên xác nhận (người mua hoặc người bán của đơn).</summary>
    public Guid UserId { get; set; }

    /// <summary>Thời điểm xác nhận.</summary>
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Món đồ nhận được có đúng với mô tả / ảnh đăng không?
    /// false = có vấn đề -> hệ thống gợi ý tạo report.
    /// </summary>
    public bool ItemReceivedOk { get; set; } = true;

    /// <summary>Ghi chú của bên xác nhận (vd: "nhận sai sách, bìa rách").</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Mã hàng (8 ký tự) của món vừa nhận, do người dùng nhập lúc bàn giao.
    /// Dùng để đối chiếu với món đáng lẽ phải nhận.
    /// </summary>
    public string? ItemCodeVerified { get; set; }

    // Navigation Properties
    public Transaction Transaction { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
