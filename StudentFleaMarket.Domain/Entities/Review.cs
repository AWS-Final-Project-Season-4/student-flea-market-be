namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Đánh giá uy tín sau giao dịch.
///
/// MÔ HÌNH ĐÁNH GIÁ (đã chốt cho Feature 5):
///   - Purchase / Free : 1 CHIỀU — chỉ người MUA (Initiator của đơn) đánh giá
///                       người BÁN (Counterparty).
///   - Exchange        : 2 CHIỀU — cả 2 bên cùng đánh giá, vì cả hai đều mang
///                       hàng ra và nhận hàng về; bên chịu rủi ro mất món đắt
///                       phải có quyền phát biểu.
///
/// Vì vậy quan hệ với <see cref="Transaction"/> là 1-N (một đơn có tối đa 2
/// đánh giá), đảm bảo bằng unique index (TransactionId, ReviewerId) —
/// mỗi người chỉ đánh giá 1 lần cho mỗi đơn.
///
/// VÔ HIỆU HOÁ (IsVoided):
///   Khi Moderator/Admin xác nhận một report là có lý do (người bị report thực
///   sự sai), lời đánh giá viết trong đơn giao dịch đó bị đánh dấu vô hiệu
///   và không còn được tính vào điểm uy tín. Đánh dấu thay vì xoá để giữ
///   vết chứng (audit) và có thể bỏ vô hiệu nếu xử lý nhầm.
/// </summary>
public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Đơn giao dịch được đánh giá.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Người viết đánh giá (người mua, hoặc 1 trong 2 bên với Exchange).</summary>
    public Guid ReviewerId { get; set; }

    /// <summary>Người được đánh giá (người bán, hoặc bên còn lại với Exchange).</summary>
    public Guid RevieweeId { get; set; }

    /// <summary>Số sao: 1 → 5.</summary>
    public int Rating { get; set; }

    /// <summary>Bình luận (không bắt buộc) — tối đa 500 ký tự.</summary>
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm sửa đánh giá gần nhất (cho phép sửa trong 14 ngày).</summary>
    public DateTime? UpdatedAt { get; set; }

    // ------------------------------------------------------- VÔ HIỆU HOÁ

    /// <summary>Đánh giá đã bị vô hiệu sau khi report được xác nhận là có lý do.</summary>
    public bool IsVoided { get; set; } = false;

    /// <summary>Report nào đã vô hiệu đánh giá này.</summary>
    public Guid? VoidedByReportId { get; set; }

    /// <summary>Nhân viên nào đã vô hiệu đánh giá này (để truy vết).</summary>
    public Guid? VoidedByUserId { get; set; }

    /// <summary>Thời điểm vô hiệu hoá.</summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>Lý do vô hiệu hoá (copy từ ghi chú xử lý của report).</summary>
    public string? VoidReason { get; set; }

    // Navigation Properties
    public Transaction Transaction { get; set; } = null!;
    public ApplicationUser Reviewer { get; set; } = null!;
    public ApplicationUser Reviewee { get; set; } = null!;
    public Report? VoidedByReport { get; set; }
}
