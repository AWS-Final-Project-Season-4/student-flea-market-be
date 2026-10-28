using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Admin;

// ============================================================ USER (màn hình quản lý)

// <summary>
// Một tài khoản người dùng trong danh sách quản lý của Admin.
// </summary>
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Username { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SchoolName { get; set; }
    public string? StudentCode { get; set; }
    public string? CommonArea { get; set; }
    public string? Dormitory { get; set; }

    /// <summary>
    /// 1 = Active, 2 = Inactive, 3 = Banned
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Tên trạng thái: "Hoạt động", "Chưa kích hoạt", "Đã bị cấm".
    /// </summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Tài khoản đang bị cấm hay không.
    /// </summary>
    public bool IsBanned { get; set; }

    /// <summary>
    /// Lý do Admin cấm tài khoản (nếu đang bị cấm).
    /// </summary>
    public string? BanReason { get; set; }

    public DateTime? BannedAt { get; set; }
    public DateTime? UnbannedAt { get; set; }

    // ----- Seller (năng lực bán hàng) -----
    public int SellerStatus { get; set; }
    public string SellerStatusName { get; set; } = string.Empty;
    public bool CanSell { get; set; }

    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Số tin đăng hiện có của tài khoản này.
    /// </summary>
    public int ItemCount { get; set; }

    // ----- Role (quyền quản trị) -----
    /// <summary>
    /// 0 = User, 1 = Admin, 2 = AiService
    /// </summary>
    public int Role { get; set; }

    /// <summary>
    /// Tên vai trò: "User", "Admin", "AiService".
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Tài khoản này đang là quản trị viên hay không.
    /// </summary>
    public bool IsAdmin { get; set; }
}

// <summary>
// Admin cấm tài khoản. Lý do nên được ghi rõ để user biết vì sao bị khoá.
// </summary>
public class BanUserRequestDto
{
    /// <summary>
    /// Lý do cấm tài khoản (hiển thị khi user đăng nhập bị từ chối).
    /// </summary>
    public string? Reason { get; set; }
}

public class BanUserRequestDtoValidator : AbstractValidator<BanUserRequestDto>
{
    public BanUserRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
