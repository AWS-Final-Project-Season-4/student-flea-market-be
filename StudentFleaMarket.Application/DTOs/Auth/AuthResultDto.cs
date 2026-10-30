namespace StudentFleaMarket.Application.DTOs.Auth;

public class UserDto
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
    public int Role { get; set; }

    /// <summary>
    /// Tên vai trò: "User", "Admin" hoặc "AiService".
    /// </summary>
    public string RoleName { get; set; } = "User";

    public int Status { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ------------------------------------------------------------ SELLER
    // "Seller" là năng lực được Admin cấp quyền, KHÔNG phải UserRole.
    // Frontend dùng CanSell để bật/tắt nút "Đăng tin bán".

    /// <summary>
    /// Trạng thái quyền bán: 0 = None, 1 = Pending, 2 = Approved, 3 = Rejected.
    /// </summary>
    public int SellerStatus { get; set; }

    /// <summary>
    /// Tên trạng thái dễ đọc: "Chưa đăng ký bán", "Đang chờ duyệt",
    /// "Đã được duyệt", "Bị từ chối".
    /// </summary>
    public string SellerStatusName { get; set; } = "Chưa đăng ký bán";

    /// <summary>
    /// Tài khoản đã được phép đăng tin bán hay chưa.
    /// </summary>
    public bool CanSell { get; set; }

    // ------------------------------------------------------------ BAN
    // Admin cấm tài khoản -> user không đăng nhập lại được cho tới khi được gỡ cấm.

    /// <summary>
    /// Lý do Admin cấm tài khoản (nếu đang bị cấm).
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Tài khoản đang bị cấm hay không.
    /// </summary>
    public bool IsBanned { get; set; }
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Kết quả đăng ký. Chưa sinh token: người dùng phải xác thực email trước khi đăng nhập.
/// </summary>
public class RegisterResultDto
{
    public string Message { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    public string VerificationEmail { get; set; } = null!;
}
