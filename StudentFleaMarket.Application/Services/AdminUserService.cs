using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Quản lý tài khoản người dùng phía Admin: xem danh sách, cấm, gỡ cấm.
///
/// Khi cấm một tài khoản, ta thực hiện 3 việc:
///   1. Status = Banned  -> login bị chặn (ACCOUNT_BANNED).
///   2. Thu hồi toàn bộ refresh token -> không thể dùng token cũ để lấy token mới.
///   3. Ghi lại lý do + admin đã cấm + thời điểm cấm (để truy vết & hiển thị).
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;
    private readonly IEmailSender _emailSender;

    public AdminUserService(
        IUserRepository userRepo,
        IItemRepository itemRepo,
        IRepository<RefreshToken> refreshTokenRepo,
        IEmailSender emailSender)
    {
        _userRepo = userRepo;
        _itemRepo = itemRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _emailSender = emailSender;
    }

    // =====================================================================
    // LIST / DETAIL
    // =====================================================================

    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        int? status,
        string? keyword,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (users, total) = await _userRepo.ListForAdminAsync(status, keyword, page, pageSize, ct);

        var result = new List<AdminUserDto>(users.Count);
        foreach (var u in users)
        {
            var itemCount = await _itemRepo.CountBySellerAsync(u.Id, ct);
            result.Add(MapUser(u, itemCount));
        }

        return new PagedResult<AdminUserDto>
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

    public async Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId, ct);
        var itemCount = await _itemRepo.CountBySellerAsync(user.Id, ct);
        return MapUser(user, itemCount);
    }

    // =====================================================================
    // BAN / UNBAN
    // =====================================================================

    public async Task<AdminUserDto> BanUserAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var target = await FindUserOrThrowAsync(targetUserId, ct);

        // Admin không cấm được Admin (kể cả chính mình) — tránh khoá nhầm hệ thống.
        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Không thể cấm tài khoản quản trị viên.",
                "ADMIN_CANNOT_BE_BANNED");

        if (target.Status == (int)UserStatus.Banned)
            throw new ConflictException(
                "Tài khoản này đã bị cấm.",
                "USER_ALREADY_BANNED");

        target.Status = (int)UserStatus.Banned;
        target.BanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        target.BannedByUserId = adminUserId;
        target.BannedAt = DateTime.UtcNow;
        target.UnbannedAt = null;
        target.UpdatedAt = DateTime.UtcNow;

        // Cấm user = thu hồi luôn quyền bán (nếu đang có).
        // Lý do: tài khoản bị khoá không được đăng tin mới.
        if (target.SellerStatus == (int)SellerStatus.Approved)
        {
            target.SellerStatus = (int)SellerStatus.None;
            target.SellerApprovedAt = null;
            target.SellerApprovedByUserId = null;
        }

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // Cấm tài khoản -> GỠ TOÀN BỘ tin đăng của tài khoản đó.
        // Tin của người bị cấm không nên tiếp tục hiển thị trên sàn.
        // ---------------------------------------------------------------
        var removedCount = await _itemRepo.RemoveAllBySellerAsync(target.Id, ct);

        // Thu hồi toàn bộ phiên đăng nhập -> refresh token cũ không dùng được nữa.
        await RevokeAllSessionsAsync(target.Id, "ACCOUNT_BANNED", ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        var reasonHtml = target.BanReason is null
            ? string.Empty
            : $"<p>Lý do: <i>{System.Net.WebUtility.HtmlEncode(target.BanReason)}</i></p>";

        var removedHtml = removedCount > 0
            ? $"<p><b>{removedCount}</b> tin đăng của bạn đã được gỡ khỏi sàn.</p>"
            : string.Empty;

        await SendMailSafeAsync(
            target.Email,
            target.FullName,
            "Tài khoản của bạn đã bị khoá",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(target.FullName)}</b>,</p>
                <p>Tài khoản của bạn đã bị quản trị viên khoá.</p>
                {reasonHtml}
                {removedHtml}
                <p>Nếu cho rằng đây là nhầm lẫn, vui lòng liên hệ quản trị viên.</p>",
            ct);

        return MapUser(target, itemCount);
    }

    public async Task<AdminUserDto> UnbanUserAsync(
        Guid adminUserId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var target = await FindUserOrThrowAsync(targetUserId, ct);

        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Không thể gỡ cấm tài khoản quản trị viên.",
                "ADMIN_CANNOT_BE_BANNED");

        if (target.Status != (int)UserStatus.Banned)
            throw new ValidationException(
                "Tài khoản này hiện không bị cấm.",
                "USER_NOT_BANNED");

        target.Status = (int)UserStatus.Active;
        target.UnbannedAt = DateTime.UtcNow;
        // Giữ lại lý do & thời điểm cấm để lưu vết (không xoá lịch sử).
        target.BannedByUserId = null;
        target.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        await SendMailSafeAsync(
            target.Email,
            target.FullName,
            "Tài khoản của bạn đã được mở khoá",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(target.FullName)}</b>,</p>
                <p>Tài khoản của bạn đã được mở khoá. Bạn có thể đăng nhập lại bình thường.</p>",
            ct);

        return MapUser(target, itemCount);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task<ApplicationUser> FindUserOrThrowAsync(Guid userId, CancellationToken ct)
    {
        return await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");
    }

    /// <summary>
    /// Thu hồi toàn bộ refresh token của tài khoản.
    /// Access token đã cấp vẫn còn hiệu lực tối đa 30 phút (JWT stateless),
    /// nhưng user sẽ không lấy được access token mới -> buộc phải đăng nhập lại.
    /// </summary>
    private async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct)
    {
        var tokens = await _refreshTokenRepo.ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct);

        if (tokens.Count == 0)
            return;

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = reason;
            _refreshTokenRepo.Update(t);
        }

        await _refreshTokenRepo.SaveChangesAsync(ct);
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
        StatusName = StatusNameOf(user.Status),
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
        // ----- Role (quyền quản trị) -----
        Role = user.Role,
        RoleName = RoleNameOf(user.Role),
        IsAdmin = user.Role == (int)UserRole.Admin
    };

    /// <summary>
    /// Tên vai trò dễ đọc cho frontend.
    /// </summary>
    public static string RoleNameOf(int role) => role switch
    {
        (int)UserRole.Admin => "Admin",
        (int)UserRole.AiService => "AiService",
        _ => "User"
    };

    /// <summary>
    /// Tên trạng thái tài khoản dễ đọc cho frontend.
    /// </summary>
    public static string StatusNameOf(int status) => status switch
    {
        (int)UserStatus.Active => "Hoạt động",
        (int)UserStatus.Inactive => "Chưa kích hoạt",
        (int)UserStatus.Banned => "Đã bị cấm",
        _ => "Không xác định"
    };

    private async Task SendMailSafeAsync(
        string to,
        string fullName,
        string subject,
        string body,
        CancellationToken ct)
    {
        try
        {
            await _emailSender.SendAsync(to, subject, body, true, ct);
        }
        catch (Exception)
        {
            // Lỗi gửi mail không được làm hỏng thao tác quản trị.
        }
    }
}
