namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Trạng thái năng lực BÁN HÀNG của một tài khoản người dùng.
///
/// Đây KHÔNG phải "vai trò" (Role) — user vẫn là User/Admin và vẫn mua,
/// yêu thích, cập nhật hồ sơ bình thường. Seller chỉ là một quyền
/// ("năng lực") do Admin cấp để mở khóa chức năng đăng tin bán.
///
/// Vì sao tách khỏi <see cref="UserRole"/>?
///   - Role là thuộc tính định danh của tài khoản (User / Admin / AiService).
///   - Seller là trạng thái có thể thay đổi theo thời gian và do Admin quyết định.
///   - Tách ra giúp không phải sửa lại JWT, [Authorize(Roles = ...)] trên toàn bộ
///     controller, và không ảnh hưởng quyền mua/yêu thích của người bán.
/// </summary>
public enum SellerStatus
{
    /// <summary>
    /// Mặc định — tài khoản người dùng thường, chưa được cấp quyền bán.
    /// Không thể đăng tin bán.
    /// </summary>
    None = 0,

    /// <summary>
    /// Đã gửi yêu cầu trở thành người bán, đang chờ Admin duyệt.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Được Admin cấp quyền bán — có thể đăng tin mới.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Đã bị Admin từ chối yêu cầu. Có thể gửi lại yêu cầu sau.
    /// </summary>
    Rejected = 3
}
