namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Báo cáo vi phạm (người dùng gửi lên hệ thống).
///
/// CÓ 3 DẠNG BÁO CÁO:
///
/// 1) BÁO CÁO BÀI ĐĂNG (tin đăng sai sự thật / lừa đảo):
///    ItemId = bắt buộc, TransactionId = null, ReportedUserId = null (hoặc có).
///    -> KHÔNG cần giao dịch.
///
/// 2) BÁO CÁO TÀI KHOẢN trong 1 giao dịch (bùng hàng / tráo hàng):
///    TransactionId = bắt buộc, ReportedUserId = bắt buộc (phải là bên còn
///    lại của đơn), ItemId = tuỳ chọn (món nào bị vấn đề).
///    -> Bắt buộc có giao dịch để CHỐNG REPORT OAN.
///
/// 3) BÁO CÁO TÀI KHOẢN qua hội thoại chat (quấy rối, lăng nhăng):
///    ConversationId = bắt buộc, ReportedUserId = bắt buộc (phải là bên còn
///    lại của hội thoại), TransactionId = null.
///    -> Dùng hội thoại làm bằng chứng liên hệ.
///
/// Vì sao nhánh (2) bắt buộc có giao dịch nhưng nhánh (3) thì không?
///   Nhánh (2) là hành vi phá hỏng giao dịch → phải có giao dịch thật.
///   Nhánh (3) là hành vi gây khó chịu qua chat → quan hệ chat đã đủ làm
///   bằng chứng, và không nên bắt kẻ quấy rối phải "mua" hàng trước.
///
/// BẢNG `Reports` CẦN CÓ INDEX RIÊNG cho từng dạng để chống spam:
///   - (ReporterId, TransactionId) : 1 người chỉ report 1 lần cho 1 đơn.
///   - (ReporterId, ItemId)        : 1 người chỉ report 1 lần cho 1 tin đăng.
///   - (ReporterId, ConversationId): 1 người chỉ report 1 lần cho 1 hội thoại.
/// </summary>
public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Người gửi báo cáo.</summary>
    public Guid ReporterId { get; set; }

    /// <summary>Tài khoản bị báo cáo (bắt buộc với dạng 2 và 3).</summary>
    public Guid? ReportedUserId { get; set; }

    /// <summary>Bài đăng bị báo cáo (bắt buộc với dạng 1).</summary>
    public Guid? ItemId { get; set; }

    /// <summary>Đơn giao dịch liên quan (bắt buộc với dạng 2).</summary>
    public Guid? TransactionId { get; set; }

    /// <summary>Hội thoại chat liên quan (bắt buộc với dạng 3).</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>ReportReason: 1 = Bùng hàng, 2 = Trả hàng sai, 3 = Không trả tiền, ...</summary>
    public int Reason { get; set; }

    /// <summary>Mô tả chi tiết (bắt buộc) — tối đa 1000 ký tự.</summary>
    public string Description { get; set; } = null!;

    /// <summary>ReportStatus: 1 = Pending, 2 = Reviewing, 3 = Resolved, 4 = Rejected.</summary>
    public int Status { get; set; }

    /// <summary>Ghi chú của nhân viên khi xử lý.</summary>
    public string? AdminNote { get; set; }

    /// <summary>Nhân viên đã xử lý report này.</summary>
    public Guid? ResolvedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    // Navigation Properties
    public ApplicationUser Reporter { get; set; } = null!;
    public ApplicationUser? ReportedUser { get; set; } = null!;
    public Item? Item { get; set; }
    public Transaction? Transaction { get; set; }
    public Conversation? Conversation { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }
}
