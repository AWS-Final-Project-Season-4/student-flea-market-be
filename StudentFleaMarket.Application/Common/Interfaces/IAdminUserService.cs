using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý tài khoản người dùng phía Admin.
///
/// Liên quan tới chức năng CẤM (ban):
///   - Cấm -> UserStatus.Banned: đăng nhập bị chặn, toàn bộ refresh token
///     bị thu hồi, tài khoản không dùng được API nào nữa.
///   - Gỡ cấm -> UserStatus.Active: tài khoản đăng nhập lại được bình thường.
///
/// Admin KHÔNG thể cấm/gỡ cấm tài khoản Admin khác (chỉ quản lý User).
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Danh sách tài khoản người dùng (phân trang + lọc theo trạng thái + tìm kiếm).
    /// </summary>
    Task<PagedResult<AdminUserDto>> ListUsersAsync(
        int? status,
        string? keyword,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Chi tiết một tài khoản.
    /// </summary>
    Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Admin cấm tài khoản. Tài khoản bị chặn đăng nhập ngay lập tức.
    /// </summary>
    Task<AdminUserDto> BanUserAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Admin gỡ cấm — tài khoản hoạt động trở lại.
    /// </summary>
    Task<AdminUserDto> UnbanUserAsync(Guid adminUserId, Guid targetUserId, CancellationToken ct = default);
}
