using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý vai trò KIỂM DUYỆT VIÊN (Moderator).
///
/// CHỈ Admin được gọi các thao tác này (xem StaffPermission.CanManageRoles).
/// Moderator KHÔNG được cấp / thu hồi role cho người khác — đây là bảo đảm
/// chống leo thang quyền: nếu Moderator tự tạo được Moderator khác thì đường
/// leo thang quyền sẽ quay lại (đúng lý do dự án đã bỏ tính năng cấp quyền
/// quản trị cho Admin).
///
/// Hướng cấp quyền LUÔN ĐI TỪ CAO XUỐNG THẤP: Admin -> Moderator -> User.
/// Không có đường đi ngược lên, nên không tạo ra đường leo thang quyền.
///
/// Mọi thao tác đều ghi lại vết (RoleGrantedByUserId / RoleGrantedAt /
/// RoleRevokedAt) để trả lời được câu hỏi "ai cấp quyền, lúc nào".
/// </summary>
public interface IModeratorService
{
    /// <summary>
    /// Danh sách các tài khoản đang là kiểm duyệt viên (phân trang).
    /// </summary>
    Task<PagedResult<ModeratorDto>> ListAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Admin cấp role kiểm duyệt viên cho một tài khoản thường.
    /// </summary>
    Task<AdminUserDto> AssignAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? note,
        CancellationToken ct = default);

    /// <summary>
    /// Admin thu hồi role kiểm duyệt viên (đưa tài khoản về lại User thường).
    /// </summary>
    Task<AdminUserDto> RevokeAsync(
        Guid adminUserId,
        Guid targetUserId,
        CancellationToken ct = default);
}
