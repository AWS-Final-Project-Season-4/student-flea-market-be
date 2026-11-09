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

///<summary>
/// Danh sách các lý do vi phạm tiêu chuẩn trên SFM
/// FE có thể dùng list này để render dropdown cho Admin chọn cho nhanh
/// </summary>

public static class StandardBanReasons
{
    public const string FraudScam = "Engaging in fraud, deception or the misapprociation of deposit money belonging to other students";
    public const string Counterfeit = "Listing counterfeit or imitation goods, or providing false product information.";
    public const string ProhibitedItem = "Trading in restricted goods (weapons, prohibited substances, wildlife, non-prescription pharmaceuticals, etc.).";
    public const string Harassment = "Engaging in behavior that harasses, threatens, offends or insults other users.";
    public const string SpamBot = "Constant spamming of junk posts";
    public const string FakeStudentId = "Forging student ID cards, impersonating students from other universities.";
    public const string PolicyViolation = "Serious violation of the platform's operational regulations and terms of service.";
}

/// <summary>
/// DTO requests an account ban
/// </summary>

public class BanUserRequestDto
{
    /// <summary>
    /// Lý do cấm tài khoản (Bắt buộc nhập, tối thiểu 10 ký tự, tối đa 500 ký tự).
    /// </summary>
    public string Reason { get; set; } = null!;
}

public class BanUserRequestDtoValidator : AbstractValidator<BanUserRequestDto>
{
    public BanUserRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
             .Cascade(CascadeMode.Stop)
             .NotEmpty().WithMessage("Providing a reason for an account ban is mandatory.")
             .MinimumLength(10).WithMessage("The reason for an account ban must be at least 10 characters long to clearly state the violation.")
             .MaximumLength(500).WithMessage("The reason for an account ban must not be exceed 500 characters.");
    }
}