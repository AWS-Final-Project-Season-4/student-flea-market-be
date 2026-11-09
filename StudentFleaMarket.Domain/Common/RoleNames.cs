using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Common;

/// <summary>
/// TÊN VAI TRÒ — nguồn sự thật duy nhất cho việc quy đổi giữa mã số role
/// (cột <c>Users."Role"</c>) và tên role dùng trong <c>[Authorize(Roles = "...")]</c>.
///
/// VÌ SAO CẦN HELPER NÀY?
///   Trước đây logic này bị nhân bản ở 4 nơi (JwtTokenService, RoleSyncMiddleware,
///   AuthService, AdminUserService). Khi thêm vai trò mới (Moderator), chỉ cần
///   quên 1 chỗ là role đó sẽ bị ghi thành "User" và mọi [Authorize] sẽ hỏng.
///   Gom về 1 chỗ -> đổi luật chỉ sửa 1 dòng.
///
/// LƯU Ý:
///   - Tên role phải KHỚP CHÍNH XÁC với chuỗi trong [Authorize(Roles = "...")].
///   - Không thêm / bỏ khoảng trắng, không đổi chữ hoa chữ thường.
/// </summary>
public static class RoleNames
{
    /// <summary>Người dùng thường (sinh viên).</summary>
    public const string User = "User";

    /// <summary>Quản trị viên — toàn quyền.</summary>
    public const string Admin = "Admin";

    /// <summary>Tài khoản dịch vụ AI — xác thực bằng API key.</summary>
    public const string AiService = "AiService";

    /// <summary>Kiểm duyệt viên — chia sẻ công việc với Admin.</summary>
    public const string Moderator = "Moderator";

    /// <summary>
    /// Quy đổi mã số role -> tên role.
    /// Role không xác định -> coi như <see cref="User"/> (an toàn: quyền tối thiểu).
    /// </summary>
    public static string Of(int role) => role switch
    {
        (int)UserRole.Admin => Admin,
        (int)UserRole.AiService => AiService,
        (int)UserRole.Moderator => Moderator,
        _ => User
    };
}
