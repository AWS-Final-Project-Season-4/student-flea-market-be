using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Quản lý vai trò kiểm duyệt viên (Moderator).
///
/// CHỈ Admin được gọi — xem <see cref="StaffPermission.CanManageRoles"/>.
/// Moderator không được cấp / thu hồi role cho người khác (chống leo thang quyền).
///
/// LƯU Ý VỀ HIỆU LỰC:
///   Role được đọc từ DB mỗi request bởi RoleSyncMiddleware, nên khi Admin
///   cấp / thu hồi role Moderator thì hiệu lực có NGAY LẬP TỨC — tài khoản
///   vừa được cấp không cần đăng nhập lại để dùng quyền kiểm duyệt.
/// </summary>
public class ModeratorService : IModeratorService
{
    private readonly IUserRepository _userRepo;
    private readonly IItemRepository _itemRepo;

    public ModeratorService(IUserRepository userRepo, IItemRepository itemRepo)
    {
        _userRepo = userRepo;
        _itemRepo = itemRepo;
    }

    // =====================================================================
    // LIST
    // =====================================================================

    public async Task<PagedResult<ModeratorDto>> ListAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (users, total) = await _userRepo.ListModeratorsAsync(page, pageSize, ct);

        var result = new List<ModeratorDto>(users.Count);
        foreach (var u in users)
        {
            var grantor = u.RoleGrantedByUserId.HasValue
                ? await _userRepo.GetByIdAsync(u.RoleGrantedByUserId.Value, ct)
                : null;

            result.Add(new ModeratorDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username,
                GrantedAt = u.RoleGrantedAt,
                GrantedByUserId = u.RoleGrantedByUserId,
                GrantedByFullName = grantor?.FullName
            });
        }

        return new PagedResult<ModeratorDto>
        {
            Items = result,
            TotalCount = total,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => pageSize
            }
        };
    }

    // =====================================================================
    // ASSIGN (Admin cấp role Moderator)
    // =====================================================================

    public async Task<AdminUserDto> AssignAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? note,
        CancellationToken ct = default)
    {
        var actor = await GetUserOrThrowAsync(adminUserId, ct);
        EnsureCanManageRoles(actor);

        var target = await GetUserOrThrowAsync(targetUserId, ct);

        if (target.Id == actor.Id)
            throw new ValidationException(
                "Bạn không thể tự cấp quyền cho chính mình.",
                "CANNOT_ASSIGN_ROLE_TO_SELF");

        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Tài khoản quản trị viên đã có toàn quyền, không cần cấp quyền kiểm duyệt.",
                "ADMIN_ALREADY_HAS_FULL_PERMISSIONS");

        if (target.Role == (int)UserRole.Moderator)
            throw new ConflictException(
                "Tài khoản này đã là kiểm duyệt viên.",
                "MODERATOR_ALREADY_ASSIGNED");

        // Tài khoản dịch vụ AI không phải user thật -> không cấp role cho được.
        if (target.Role == (int)UserRole.AiService)
            throw new ValidationException(
                "Không thể cấp quyền cho tài khoản dịch vụ AI.",
                "AI_SERVICE_CANNOT_BE_MODERATOR");

        // Tài khoản đang bị cấm thì không cấp quyền được — tránh tạo ra
        // tài khoản "kiểm duyệt viên" nhưng không dùng được do đã bị khoá.
        if (target.Status == (int)UserStatus.Banned)
            throw new ValidationException(
                "Không thể cấp quyền cho tài khoản đang bị cấm. Hãy gỡ cấm trước.",
                "BANNED_USER_CANNOT_BE_MODERATOR");

        target.Role = (int)UserRole.Moderator;
        target.RoleGrantedByUserId = adminUserId;
        target.RoleGrantedAt = DateTime.UtcNow;
        target.RoleRevokedAt = null;
        target.UpdatedAt = DateTime.UtcNow;

        // KHÔNG đụng tới SellerStatus ở đây.
        // Moderator được quyền đăng tin nhờ vai trò (SellerPermission.CanSell),
        // không phải nhờ SellerStatus -> giữ nguyên trạng thái quyền bán sẵn có
        // để khi bị hạ cấp về User thì không mất quyền đã được duyệt trước đó.

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        return MapUser(target, itemCount);
    }

    // =====================================================================
    // REVOKE (Admin thu hồi role Moderator)
    // =====================================================================

    public async Task<AdminUserDto> RevokeAsync(
        Guid adminUserId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var actor = await GetUserOrThrowAsync(adminUserId, ct);
        EnsureCanManageRoles(actor);

        var target = await GetUserOrThrowAsync(targetUserId, ct);

        if (target.Role != (int)UserRole.Moderator)
            throw new ValidationException(
                "Tài khoản này không phải kiểm duyệt viên.",
                "USER_IS_NOT_MODERATOR");

        // Hạ cấp về User thường -> quyền bán phải trở về trạng thái bình thường
        // (không còn được miễn vì là nhân viên).
        target.Role = (int)UserRole.User;
        target.RoleRevokedAt = DateTime.UtcNow;
        target.UpdatedAt = DateTime.UtcNow;

        // Hạ cấp về User thường -> quyền bán phải được xét lại.
        // Moderator được phép bán nhờ vai trò; khi hạ cấp, tài khoản về lại User
        // nên chỉ được bán nếu SellerStatus = Approved.
        // Nếu trước đó chưa từng được duyệt bán -> về None để phải xin lại.
        if ((SellerStatus)target.SellerStatus != SellerStatus.Approved)
        {
            target.SellerStatus = (int)SellerStatus.None;
            target.SellerApprovedAt = null;
            target.SellerApprovedByUserId = null;
        }

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        return MapUser(target, itemCount);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task<ApplicationUser> GetUserOrThrowAsync(Guid userId, CancellationToken ct)
    {
        return await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");
    }

    /// <summary>
    /// Kiểm tra người thực hiện có quyền cấp / thu hồi role không.
    /// CHỈ Admin — Moderator không được cấp role cho người khác.
    /// </summary>
    private static void EnsureCanManageRoles(ApplicationUser actor)
    {
        if (!StaffPermission.CanManageRoles(actor))
            throw new ForbiddenException(
                "Chỉ quản trị viên mới cấp hoặc thu hồi được vai trò kiểm duyệt viên.",
                "FORBIDDEN");
    }

    private static AdminUserDto MapUser(ApplicationUser user, int itemCount) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Username = user.Username,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        SchoolName = user.SchoolName,
        StudentCode = user.StudentCode,
        CommonArea = user.CommonArea,
        Dormitory = user.Dormitory,
        Status = user.Status,
        StatusName = AdminUserService.StatusNameOf(user.Status),
        IsBanned = user.Status == (int)UserStatus.Banned,
        BanReason = user.BanReason,
        BannedAt = user.BannedAt,
        UnbannedAt = user.UnbannedAt,
        SellerStatus = user.SellerStatus,
        SellerStatusName = SellerService.StatusNameOf(user.SellerStatus),
        CanSell = SellerPermission.CanSell(user),
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        ItemCount = itemCount,
        Role = user.Role,
        RoleName = AdminUserService.RoleNameOf(user.Role),
        IsAdmin = user.Role == (int)UserRole.Admin,
        IsModerator = user.Role == (int)UserRole.Moderator,
        IsStaff = StaffPermission.IsStaff(user)
    };
}
