using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Tên tài khoản để đăng nhập (vd: "admin", "tie123", "velina44").
    /// Null nghĩa là tài khoản này chỉ đăng nhập được bằng email.
    /// Khi có giá trị thì đăng nhập được bằng CẢ username lẫn email.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// URL ảnh đại diện (avatar) của người dùng.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Tên trường học / trường trung cấp.
    /// </summary>
    public string? SchoolName { get; set; }

    /// <summary>
    /// Mã sinh viên / mã học sinh.
    /// </summary>
    public string? StudentCode { get; set; }

    /// <summary>
    /// Khu vực thường lui tới (vd: "Khu A - Bắc", "Quận 1").
    /// </summary>
    public string? CommonArea { get; set; }

    /// <summary>
    /// Ký túc xá / nơi ở (vd: "KTX A - Tầng 3, Phòng 302").
    /// </summary>
    public string? Dormitory { get; set; }

    /// <summary>
    /// UserRole: 0 = User, 1 = Admin, 2 = AiService
    /// </summary>
    public int Role { get; set; } = (int)UserRole.User;

    /// <summary>
    /// 1: Active, 2: Inactive, 3: Banned
    /// </summary>
    public int Status { get; set; } = 1;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    //Navigation Properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
}
