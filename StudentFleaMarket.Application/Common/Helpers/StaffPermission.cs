using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// PHẠM VI QUYỀN CỦA NHÂN VIÊN (Admin + Moderator) — nguồn sự thật duy nhất.
///
/// VÌ SAO CẦN HELPER NÀY?
///   Luật "ai được làm gì" được áp dụng ở nhiều nơi: duyệt quyền bán, cấm tài
///   khoản, gỡ tin đăng vi phạm... Nếu mỗi nơi tự viết một lần thì rất dễ lệch
///   logic (ví dụ một nơi quên mất trường hợp Moderator). Gom vào đây để đổi
///   luật chỉ sửa 1 chỗ — đúng tinh thần <see cref="SellerPermission"/>.
///
/// PHÂN TẦNG QUYỀN:
///   Admin      = toàn quyền quản trị.
///   Moderator  = cấp dưới Admin: chia sẻ công việc lặp lại (duyệt quyền bán,
///                cấm tài khoản thường, gỡ tin vi phạm) nhưng KHÔNG chạm vào
///                những việc nhạy cảm (quản lý tài khoản dịch vụ AI, cấp role,
///                cấm tài khoản Moderator/Admin).
///
/// LƯU Ý QUAN TRỌNG:
///   Trạng thái được đọc TRỰC TIẾP từ DB (không từ JWT), nên việc cấp / thu
///   hồi quyền có hiệu lực tức thì, không cần đăng nhập lại.
/// </summary>
public static class StaffPermission
{
    /// <summary>
    /// Tài khoản này có thuộc nhóm nhân viên (Admin hoặc Moderator) không?
    /// </summary>
    public static bool IsStaff(ApplicationUser user)
        => user.Role == (int)UserRole.Admin
           || user.Role == (int)UserRole.Moderator;

    /// <summary>
    /// Tài khoản này có phải Moderator không?
    /// </summary>
    public static bool IsModerator(ApplicationUser user)
        => user.Role == (int)UserRole.Moderator;

    /// <summary>
    /// Tài khoản này có phải Admin không?
    /// </summary>
    public static bool IsAdmin(ApplicationUser user)
        => user.Role == (int)UserRole.Admin;

    /// <summary>
    /// Tài khoản này có quyền DUYỆT quyền bán không? (Admin và Moderator đều được)
    /// </summary>
    public static bool CanApproveSeller(ApplicationUser user) => IsStaff(user);

    /// <summary>
    /// Tài khoản này có quyền CẤM / GỠ CẤM tài khoản thường không?
    /// (Admin và Moderator đều được)
    /// </summary>
    public static bool CanBanRegularUsers(ApplicationUser user) => IsStaff(user);

    /// <summary>
    /// Tài khoản này có quyền GỠ tin đăng vi phạm (tin không thuộc về mình) không?
    /// (Admin và Moderator đều được)
    /// </summary>
    public static bool CanRemoveAnyItem(ApplicationUser user) => IsStaff(user);

    /// <summary>
    /// Tài khoản này có quyền XỬ LÝ BÁO CÁO VI PHẠM không?
    /// (Admin và Moderator đều được)
    /// </summary>
    public static bool CanHandleReports(ApplicationUser user) => IsStaff(user);

    /// <summary>
    /// Tài khoản này có quyền QUẢN LÝ DANH MỤC SẢN PHẨM không?
    /// (Admin và Moderator đều được)
    ///
    /// Phạm vi: bật/tắt danh mục, thêm / sửa / xoá danh mục, sắp xếp lại thứ tự.
    /// </summary>
    public static bool CanManageCategories(ApplicationUser user) => IsStaff(user);

    /// <summary>
    /// Tài khoản này có quyền QUẢN LÝ tài khoản dịch vụ AI không? (chỉ Admin)
    /// </summary>
    public static bool CanManageServiceAccounts(ApplicationUser user) => IsAdmin(user);

    /// <summary>
    /// Tài khoản này có quyền CẤP / THU HỒI role cho người khác không? (chỉ Admin)
    ///
    /// Đây là bảo đảm chống leo thang quyền: Moderator KHÔNG được tạo ra
    /// Moderator khác, nếu không đường leo thang quyền sẽ quay lại.
    /// </summary>
    public static bool CanManageRoles(ApplicationUser user) => IsAdmin(user);
}
