using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Seller;

// ============================================================ USER (phía người bán)

// <summary>
// Yêu cầu trở thành người bán do người dùng tự gửi.
// Admin sẽ duyệt yêu cầu này để mở khóa chức năng đăng tin bán.
// </summary>
public class SellerRequestDto
{
    /// <summary>
    /// Ghi chú giới thiệu ngắn (không bắt buộc) — giúp Admin nhanh chóng
    /// đánh giá nên cấp quyền bán hay không.
    /// </summary>
    public string? Note { get; set; }
}

public class SellerRequestDtoValidator : AbstractValidator<SellerRequestDto>
{
    public SellerRequestDtoValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

// <summary>
// Trạng thái quyền bán hàng của chính người đang đăng nhập.
// </summary>
public class SellerStatusDto
{
    /// <summary>
    /// 0 = None, 1 = Pending, 2 = Approved, 3 = Rejected
    /// </summary>
    public int SellerStatus { get; set; }

    /// <summary>
    /// Tên trạng thái dễ đọc: "Chưa đăng ký bán", "Đang chờ duyệt",
    /// "Đã được duyệt", "Bị từ chối".
    /// </summary>
    public string SellerStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Người dùng đã được phép đăng tin bán hay chưa.
    /// </summary>
    public bool CanSell { get; set; }

    public DateTime? RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

// ============================================================ ADMIN (phía quản trị)

// <summary>
// Một yêu cầu trở thành người bán trong danh sách Admin duyệt.
// </summary>
public class AdminSellerRequestDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Username { get; set; }
    public string? SchoolName { get; set; }
    public string? StudentCode { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 0 = None, 1 = Pending, 2 = Approved, 3 = Rejected
    /// </summary>
    public int SellerStatus { get; set; }
    public string SellerStatusName { get; set; } = string.Empty;

    public DateTime? RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Số tin đăng hiện có của tài khoản này.
    /// </summary>
    public int ItemCount { get; set; }
}

// <summary>
// Từ chối yêu cầu trở thành người bán — Admin nên nêu lý do.
// </summary>
public class RejectSellerRequestDto
{
    /// <summary>
    /// Lý do từ chối (hiển thị cho người dùng).
    /// </summary>
    public string? Reason { get; set; }
}

public class RejectSellerRequestDtoValidator : AbstractValidator<RejectSellerRequestDto>
{
    public RejectSellerRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
