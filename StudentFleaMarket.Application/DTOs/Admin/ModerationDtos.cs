using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Admin;

// ============================================================ MODERATION — REPORT

/// <summary>
/// Một report trong danh sách kiểm duyệt của nhân viên.
/// </summary>
public class ModerationReportDto
{
    public Guid Id { get; init; }

    /// <summary>ReportReason.</summary>
    public int Reason { get; init; }
    public string ReasonName { get; init; } = null!;

    /// <summary>ReportStatus: 1 = Pending, 2 = Reviewing, 3 = Resolved, 4 = Rejected.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    public string Description { get; init; } = null!;

    // Người gửi report
    public Guid ReporterId { get; init; }
    public string ReporterName { get; init; } = null!;

    // Người bị báo cáo
    public Guid? ReportedUserId { get; init; }
    public string? ReportedUserName { get; init; }

    // Đối tượng liên quan
    public Guid? ItemId { get; init; }
    public string? ItemTitle { get; init; }
    public string? ItemCode { get; init; }
    public Guid? TransactionId { get; init; }
    public Guid? ConversationId { get; init; }

    /// <summary>Đơn giao dịch đã hoàn thành chưa (ảnh hưởng tới việc có thể vô hiệu đánh giá).</summary>
    public bool? TransactionCompleted { get; init; }

    /// <summary>Ghi chú xử lý.</summary>
    public string? AdminNote { get; init; }

    public Guid? ResolvedById { get; init; }
    public string? ResolvedByName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

/// <summary>
/// Xử lý một report.
///
/// `actions` quyết định hệ thống làm gì:
///   - "reject"          : bác report (không có vi phạm) — KHÔNG tính vào uy tín.
///   - "hide_item"       : ẩn tin đăng bị báo cáo.
///   - "ban_user"        : cấm tài khoản bị báo cáo.
///   - "revoke_seller"   : thu hồi quyền bán của tài khoản bị báo cáo.
///   - "void_review"     : vô hiệu hoá đánh giá mà bên bị báo cáo đã viết
///                         trong đơn giao dịch liên quan (chống kẻ lừa đảo
///                         tự chấm 5 sao rồi bùng hàng / tráo hàng).
///   - "none"            : xác nhận có vi phạm nhưng không cần hành động gì thêm
///                         (vẫn tính 1 report xác nhận vào uy tín → badge ⚠).
///
/// Có thể chọn NHIỀU hành động cùng lúc (vd: ẩn tin + cấm tài khoản).
/// </summary>
public class ResolveReportDto
{
    /// <summary>Danh sách hành động — xem mô tả class.</summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>Ghi chú của nhân viên (hiển thị cho người báo cáo).</summary>
    public string? AdminNote { get; set; }
}

public class ResolveReportDtoValidator : AbstractValidator<ResolveReportDto>
{
    private static readonly string[] Allowed =
    {
        "reject", "hide_item", "ban_user", "revoke_seller", "void_review", "none"
    };

    public ResolveReportDtoValidator()
    {
        RuleFor(x => x.Actions)
            .NotEmpty().WithMessage("Phải chọn ít nhất một hành động xử lý.")
            .Must(a => a.All(x => Allowed.Contains(x.Trim().ToLowerInvariant())))
            .WithMessage("Có hành động xử lý không hợp lệ.");

        RuleFor(x => x.AdminNote)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.AdminNote));
    }
}

// ============================================================ MODERATION — DASHBOARD

/// <summary>
/// Thống kê tổng quan cho bảng điều khiển của Admin / Moderator.
/// </summary>
public class ModerationDashboardDto
{
    /// <summary>Số report đang chờ xử lý.</summary>
    public int PendingReportCount { get; init; }

    /// <summary>Số report đang được xem xét.</summary>
    public int ReviewingReportCount { get; init; }

    /// <summary>Số report đã xử lý (có hành động).</summary>
    public int ResolvedReportCount { get; init; }

    /// <summary>Số report bị bác.</summary>
    public int RejectedReportCount { get; init; }

    /// <summary>Số tin đăng đang bị ẩn do kiểm duyệt.</summary>
    public int HiddenItemCount { get; init; }

    /// <summary>Số tài khoản đang bị cấm.</summary>
    public int BannedUserCount { get; init; }

    /// <summary>Số danh mục đang mở / tổng số danh mục.</summary>
    public int ActiveCategoryCount { get; init; }
    public int TotalCategoryCount { get; init; }

    /// <summary>Số đánh giá đã bị vô hiệu hoá.</summary>
    public int VoidedReviewCount { get; init; }
}
