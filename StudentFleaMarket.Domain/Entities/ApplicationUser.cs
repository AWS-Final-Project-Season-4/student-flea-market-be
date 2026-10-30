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

    // ------------------------------------------------------------ ADMIN
    // Vai trò Admin được gán TRỰC TIẾP trong DB (tài khoản admin mặc định
    // được seed khi khởi tạo). Dự án KHÔNG có API cấp / gỡ quyền quản trị
    // -> không có đường leo thang quyền qua API.
    //
    // Role trong JWT chỉ là "gợi ý" — nguồn sự thật luôn là DB (xem RoleSyncMiddleware),
    // nên nếu role được đổi trực tiếp trong DB thì có hiệu lực tức thì,
    // không cần đăng nhập lại.

    // ------------------------------------------------------------ SELLER
    // "Seller" là NĂNG LỰC được Admin cấp quyền, KHÔNG phải một UserRole riêng.
    // Nhờ vậy người bán vẫn là User và vẫn mua / yêu thích / sửa hồ sơ bình thường.
    // Xem SellerStatus.cs để hiểu ý nghĩa từng giá trị.

    /// <summary>
    /// Trạng thái quyền bán hàng:
    /// 0 = None (chưa được cấp quyền), 1 = Pending (đang chờ duyệt),
    /// 2 = Approved (được phép đăng bán), 3 = Rejected (bị từ chối).
    /// </summary>
    public int SellerStatus { get; set; } = (int)Enums.SellerStatus.None;

    /// <summary>
    /// Thời điểm gửi yêu cầu trở thành người bán (gần nhất).
    /// </summary>
    public DateTime? SellerRequestedAt { get; set; }

    /// <summary>
    /// Thời điểm Admin cấp quyền bán (gần nhất).
    /// </summary>
    public DateTime? SellerApprovedAt { get; set; }

    /// <summary>
    /// Admin đã duyệt yêu cầu của tài khoản này.
    /// </summary>
    public Guid? SellerApprovedByUserId { get; set; }

    /// <summary>
    /// Lý do Admin từ chối yêu cầu (nếu bị từ chối).
    /// </summary>
    public string? SellerRejectionReason { get; set; }

    /// <summary>
    /// 1: Active, 2: Inactive, 3: Banned
    /// </summary>
    public int Status { get; set; } = 1;

    // -------------------------------------------------- BAN (Admin khoá tài khoản)
    // Admin cấm user -> Status = Banned (3). Login bị chặn ngay (ACCOUNT_BANNED).
    // Toàn bộ refresh token bị thu hồi, nên user phải đăng nhập lại để tiếp tục.

    /// <summary>
    /// Lý do Admin cấm tài khoản này (hiển thị cho user khi đăng nhập bị chặn).
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Admin đã cấm tài khoản này (nếu có).
    /// </summary>
    public Guid? BannedByUserId { get; set; }

    /// <summary>
    /// Thời điểm bị cấm.
    /// </summary>
    public DateTime? BannedAt { get; set; }

    /// <summary>
    /// Thời điểm gỡ cấm (nếu đã bị cấm rồi được mở lại).
    /// </summary>
    public DateTime? UnbannedAt { get; set; }

    public bool IsEmailVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    //Navigation Properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();

    /// <summary>
    /// Admin đã cấp quyền bán cho tài khoản này (nếu có).
    /// </summary>
    public ApplicationUser? SellerApprovedBy { get; set; }

    /// <summary>
    /// Admin đã cấm tài khoản này (nếu có).
    /// </summary>
    public ApplicationUser? BannedBy { get; set; }
}
