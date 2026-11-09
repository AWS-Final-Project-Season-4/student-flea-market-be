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
    AiService = 2,

    /// <summary>
    /// Kiểm duyệt viên — nhân viên trực, chia sẻ công việc với Admin khi Admin bận.
    ///
    /// PHẠM VI QUYỀN (xem thêm <c>StaffPermission</c> ở tầng Application):
    ///   - Được duyệt / từ chối / thu hồi quyền bán (giống Admin).
    ///   - Được cấm / gỡ cấm tài khoản THƯỜNG.
    ///   - Được gỡ tin đăng vi phạm (không phải tin của chính mình).
    ///   - KHÔNG được cấm tài khoản Moderator khác (chỉ Admin được).
    ///   - KHÔNG được cấm tài khoản Admin (không ai được).
    ///   - KHÔNG được quản lý tài khoản dịch vụ AI (chỉ Admin được).
    ///   - KHÔNG được cấp / thu hồi role cho người khác (chỉ Admin được).
    /// </summary>
    Moderator = 3
}
