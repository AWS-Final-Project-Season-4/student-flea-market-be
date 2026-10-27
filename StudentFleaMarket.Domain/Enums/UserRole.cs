namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Vai trò của tài khoản trong hệ thống.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Người dùng (sinh viên / học sinh) — có tài khoản đăng nhập, đăng ký được.
    /// </summary>
    User = 0,

    /// <summary>
    /// Quản trị viên — có tài khoản đăng nhập, được tạo qua seed hoặc bởi Admin khác.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Dịch vụ AI (kiểm duyệt, gợi ý sản phẩm, hỗ trợ hội thoại...).
    /// Không đăng nhập bằng email/password — xác thực bằng API key.
    /// </summary>
    AiService = 2
}
