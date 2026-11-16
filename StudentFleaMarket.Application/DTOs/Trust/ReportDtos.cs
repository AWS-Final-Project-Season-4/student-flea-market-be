using FluentValidation;
using StudentFleaMarket.Application.Common.Helpers;

namespace StudentFleaMarket.Application.DTOs.Trust;

// ============================================================ REPORT (phía người dùng)

/// <summary>
/// Gửi report vi phạm lên hệ thống.
///
/// CÓ 3 DẠNG — mỗi dạng dùng đúng bộ tham số riêng:
///
/// 1) BÁO CÁO BÀI ĐĂNG (không cần giao dịch):
///    ItemCode = mã hàng 8 ký tự. Dùng cho: tin sai sự thật, lừa đảo, ảnh giả.
///
/// 2) BÁO CÁO TRONG GIAO DỊCH (bắt buộc có đơn):
///    TransactionId + ItemCode (tuỳ chọn). Dùng cho: bùng hàng, tráo hàng,
///    không trả tiền.
///
/// 3) BÁO CÁO QUA HỘI THOẠI (quấy rối):
///    ConversationId. Dùng cho: quấy rối, lăng nhăng, spam.
///
/// Ràng buộc được kiểm tra ở tầng service (ReportService):
///   - Dạng 2: người bị report BẮT BUỘC là bên còn lại của đơn, và người báo
///     phải là 1 trong 2 bên. Chống report oan người vô tội.
///   - Dạng 3: người bị report phải là bên còn lại của hội thoại.
///   - Không được report chính mình.
/// </summary>
public class CreateReportDto
{
    /// <summary>
    /// ReportReason: 1 = Bùng hàng, 2 = Trả hàng sai, 3 = Không trả tiền,
    /// 4 = Quấy rối, 5 = Lừa đảo, 6 = Tin giả, 7 = Khác.
    /// </summary>
    public int Reason { get; set; }

    /// <summary>Mô tả chi tiết (bắt buộc) — tối đa 1000 ký tự.</summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Mã hàng (8 ký tự) của tin đăng bị báo cáo.
    /// Bắt buộc khi báo cáo bài đăng; tuỳ chọn khi báo cáo trong giao dịch.
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>Id đơn giao dịch liên quan — bắt buộc khi báo cáo trong giao dịch.</summary>
    public Guid? TransactionId { get; set; }

    /// <summary>Id hội thoại chat liên quan — bắt buộc khi báo cáo quấy rối qua chat.</summary>
    public Guid? ConversationId { get; set; }
}

public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportDtoValidator()
    {
        RuleFor(x => x.Reason)
            .Must(r => Enum.IsDefined(typeof(Domain.Enums.ReportReason), r))
            .WithMessage("Lý do báo cáo không hợp lệ.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Vui lòng mô tả chi tiết vấn đề.")
            .MinimumLength(10).WithMessage("Mô tả phải có ít nhất 10 ký tự để nhân viên xử lý được.")
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");

        RuleFor(x => x.ItemCode)
            .Must(c => string.IsNullOrWhiteSpace(c) || ItemCodeHelper.IsValidFormat(c))
            .WithMessage("Mã hàng phải gồm đúng 8 ký tự chữ cái và chữ số.")
            .When(x => !string.IsNullOrWhiteSpace(x.ItemCode));

        // Phải có ít nhất một "đích" để báo cáo.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ItemCode)
                        || x.TransactionId.HasValue
                        || x.ConversationId.HasValue)
            .WithMessage("Phải chỉ rõ bài đăng, giao dịch hoặc hội thoại cần báo cáo.");
    }
}

/// <summary>
/// Report do người dùng gửi — hiển thị cho chính người đó theo dõi.
/// </summary>
public class MyReportDto
{
    public Guid Id { get; init; }

    /// <summary>ReportReason.</summary>
    public int Reason { get; init; }
    public string ReasonName { get; init; } = null!;

    /// <summary>ReportStatus: 1 = Pending, 2 = Reviewing, 3 = Resolved, 4 = Rejected.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    public string Description { get; init; } = null!;

    /// <summary>Ghi chú phản hồi từ nhân viên (nếu đã xử lý).</summary>
    public string? AdminNote { get; init; }

    /// <summary>Tên món đồ bị báo cáo (nếu có).</summary>
    public string? ItemTitle { get; init; }

    /// <summary>Tên tài khoản bị báo cáo (nếu có).</summary>
    public string? ReportedUserName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
