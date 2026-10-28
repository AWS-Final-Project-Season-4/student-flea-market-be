using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Seller;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Xử lý năng lực bán hàng (Seller).
///
/// Người bán vẫn giữ UserRole = User, nên mọi quyền mua / yêu thích /
/// xem hồ sơ của user thường vẫn hoạt động bình thường.
/// Chỉ chức năng ĐĂNG TIN BÁN mới bị khoá cho tới khi Admin duyệt.
/// </summary>
public class SellerService : ISellerService
{
    private readonly IUserRepository _userRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IEmailSender _emailSender;

    public SellerService(
        IUserRepository userRepo,
        IItemRepository itemRepo,
        IEmailSender emailSender)
    {
        _userRepo = userRepo;
        _itemRepo = itemRepo;
        _emailSender = emailSender;
    }

    // =====================================================================
    // PHÍA NGƯỜI DÙNG
    // =====================================================================

    public async Task<SellerStatusDto> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);
        return MapStatus(user);
    }

    public async Task<SellerStatusDto> RequestAsync(
        Guid userId,
        SellerRequestDto request,
        CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        // Admin không cần xin quyền bán — đã có sẵn toàn bộ quyền.
        if (user.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Tài khoản quản trị viên đã có đầy đủ quyền đăng bán.",
                "ADMIN_ALREADY_CAN_SELL");

        switch ((SellerStatus)user.SellerStatus)
        {
            case SellerStatus.Pending:
                throw new ConflictException(
                    "Yêu cầu đang chờ Admin duyệt. Vui lòng đợi kết quả.",
                    "SELLER_REQUEST_PENDING");

            case SellerStatus.Approved:
                throw new ConflictException(
                    "Tài khoản của bạn đã được cấp quyền bán hàng.",
                    "SELLER_ALREADY_APPROVED");
        }

        // None hoặc Rejected -> gửi lại yêu cầu (cho phép gửi lại sau khi bị từ chối).
        user.SellerStatus = (int)SellerStatus.Pending;
        user.SellerRequestedAt = DateTime.UtcNow;
        user.SellerApprovedAt = null;
        user.SellerApprovedByUserId = null;
        user.SellerRejectionReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await NotifyAdminsAsync(
            user,
            $"Yêu cầu trở thành người bán mới: {user.FullName}",
            BuildAdminNotificationBody(user, request.Note),
            ct);

        return MapStatus(user);
    }

    public async Task<SellerStatusDto> CancelRequestAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        if ((SellerStatus)user.SellerStatus != SellerStatus.Pending)
            throw new ValidationException(
                "Không có yêu cầu nào đang chờ duyệt để hủy.",
                "SELLER_NO_PENDING_REQUEST");

        user.SellerStatus = (int)SellerStatus.None;
        user.SellerRequestedAt = null;
        user.SellerRejectionReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        return MapStatus(user);
    }

    // =====================================================================
    // PHÍA ADMIN
    // =====================================================================

    public async Task<PagedResult<AdminSellerRequestDto>> ListRequestsAsync(
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (users, total) = await _userRepo.ListSellerRequestsAsync(status, page, pageSize, ct);

        var result = new List<AdminSellerRequestDto>(users.Count);
        foreach (var u in users)
        {
            var itemCount = await _itemRepo.CountBySellerAsync(u.Id, ct);
            result.Add(MapAdminRequest(u, itemCount));
        }

        return new PagedResult<AdminSellerRequestDto>
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

    public async Task<AdminSellerRequestDto> ApproveAsync(
        Guid adminUserId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var target = await GetUserAsync(targetUserId, ct);

        if ((SellerStatus)target.SellerStatus == SellerStatus.Approved)
            throw new ConflictException(
                "Tài khoản này đã được cấp quyền bán hàng.",
                "SELLER_ALREADY_APPROVED");

        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Tài khoản quản trị viên không cần cấp quyền bán.",
                "ADMIN_ALREADY_CAN_SELL");

        // Tài khoản đang bị cấm thì không cấp quyền bán được — tránh tạo ra
        // tài khoản "được duyệt bán" nhưng không thể dùng do đã bị khoá.
        if (target.Status == (int)UserStatus.Banned)
            throw new ValidationException(
                "Không thể cấp quyền bán cho tài khoản đang bị cấm. Hãy gỡ cấm trước.",
                "BANNED_USER_CANNOT_BE_SELLER");

        target.SellerStatus = (int)SellerStatus.Approved;
        target.SellerApprovedAt = DateTime.UtcNow;
        target.SellerApprovedByUserId = adminUserId;
        target.SellerRejectionReason = null;
        target.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        await SendMailSafeAsync(
            target.Email,
            target.FullName,
            "Yêu cầu trở thành người bán đã được duyệt",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(target.FullName)}</b>,</p>
                <p>Yêu cầu trở thành người bán của bạn đã được <b>Admin duyệt</b>.</p>
                <p>Bạn đã có thể đăng tin bán đồ cũ trên Campus Thrift &amp; Exchange Hub.</p>",
            ct);

        return MapAdminRequest(target, itemCount);
    }

    public async Task<AdminSellerRequestDto> RejectAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var target = await GetUserAsync(targetUserId, ct);

        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Không thể từ chối quyền bán của tài khoản quản trị viên.",
                "ADMIN_CANNOT_REJECT");

        if ((SellerStatus)target.SellerStatus == SellerStatus.Rejected)
            throw new ConflictException(
                "Yêu cầu này đã bị từ chối trước đó.",
                "SELLER_ALREADY_REJECTED");

        // Đang ĐƯỢC duyệt rồi thì phải dùng "revoke" (thu hồi quyền bán),
        // không phải "reject" (từ chối yêu cầu). Nếu không chặn, sẽ tạo ra
        // trạng thái mâu thuẫn: vừa "được duyệt" vừa "bị từ chối".
        if ((SellerStatus)target.SellerStatus == SellerStatus.Approved)
            throw new ValidationException(
                "Tài khoản này đang có quyền bán. " +
                "Hãy dùng thao tác thu hồi quyền bán thay vì từ chối.",
                "SELLER_USE_REVOKE_INSTEAD");

        target.SellerStatus = (int)SellerStatus.Rejected;
        target.SellerRejectionReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();
        target.SellerApprovedAt = null;
        target.SellerApprovedByUserId = null;
        target.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // Từ chối / mất quyền bán -> GỠ TOÀN BỘ tin đăng của tài khoản.
        // Nếu không, tin vẫn hiển thị công khai dù chủ tin không còn quyền
        // đăng -> vi phạm kỳ vọng của người mua.
        // ---------------------------------------------------------------
        var removedCount = await _itemRepo.RemoveAllBySellerAsync(target.Id, ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        var reasonHtml = target.SellerRejectionReason is null
            ? string.Empty
            : $"<p>Lý do: <i>{System.Net.WebUtility.HtmlEncode(target.SellerRejectionReason)}</i></p>";

        var removedHtml = removedCount > 0
            ? $"<p><b>{removedCount}</b> tin đăng của bạn đã được gỡ khỏi sàn khi quyền bán bị thu hồi.</p>"
            : string.Empty;

        await SendMailSafeAsync(
            target.Email,
            target.FullName,
            "Yêu cầu trở thành người bán chưa được duyệt",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(target.FullName)}</b>,</p>
                <p>Yêu cầu trở thành người bán của bạn chưa được duyệt.</p>
                {reasonHtml}
                {removedHtml}
                <p>Bạn có thể gửi lại yêu cầu bất cứ lúc nào.</p>",
            ct);

        return MapAdminRequest(target, itemCount);
    }

    public async Task<AdminSellerRequestDto> RevokeAsync(
        Guid adminUserId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var target = await GetUserAsync(targetUserId, ct);

        if (target.Role == (int)UserRole.Admin)
            throw new ValidationException(
                "Không thể thu hồi quyền bán của tài khoản quản trị viên.",
                "ADMIN_CANNOT_REVOKE");

        if ((SellerStatus)target.SellerStatus != SellerStatus.Approved)
            throw new ValidationException(
                "Tài khoản này hiện không có quyền bán để thu hồi.",
                "SELLER_NOT_APPROVED");

        target.SellerStatus = (int)SellerStatus.None;
        target.SellerApprovedAt = null;
        target.SellerApprovedByUserId = null;
        target.SellerRejectionReason = null;
        target.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // Thu hồi quyền bán -> GỠ TOÀN BỘ tin đăng của người bán.
        // Nếu không, tin vẫn hiển thị công khai dù người bán không còn
        // quyền đăng tin -> vi phạm kỳ vọng của người mua.
        // ---------------------------------------------------------------
        var removedCount = await _itemRepo.RemoveAllBySellerAsync(target.Id, ct);

        var itemCount = await _itemRepo.CountBySellerAsync(target.Id, ct);

        var removedHtml = removedCount > 0
            ? $"<p><b>{removedCount}</b> tin đăng của bạn đã được gỡ khỏi sàn khi quyền bán bị thu hồi.</p>"
            : string.Empty;

        await SendMailSafeAsync(
            target.Email,
            target.FullName,
            "Quyền đăng bán đã bị thu hồi",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(target.FullName)}</b>,</p>
                <p>Quyền đăng bán của bạn đã bị thu hồi. Bạn vẫn có thể mua, yêu thích và tham gia bình thường.</p>
                {removedHtml}",
            ct);

        return MapAdminRequest(target, itemCount);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task<ApplicationUser> GetUserAsync(Guid userId, CancellationToken ct)
    {
        return await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");
    }

    private static SellerStatusDto MapStatus(ApplicationUser user) => new()
    {
        SellerStatus = user.SellerStatus,
        SellerStatusName = StatusNameOf(user.SellerStatus),
        CanSell = SellerPermission.CanSell(user),
        RequestedAt = user.SellerRequestedAt,
        ApprovedAt = user.SellerApprovedAt,
        RejectionReason = user.SellerRejectionReason
    };

    private static AdminSellerRequestDto MapAdminRequest(ApplicationUser user, int itemCount) => new()
    {
        UserId = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Username = user.Username,
        SchoolName = user.SchoolName,
        StudentCode = user.StudentCode,
        PhoneNumber = user.PhoneNumber,
        SellerStatus = user.SellerStatus,
        SellerStatusName = StatusNameOf(user.SellerStatus),
        RequestedAt = user.SellerRequestedAt,
        ApprovedAt = user.SellerApprovedAt,
        RejectionReason = user.SellerRejectionReason,
        ItemCount = itemCount
    };

    /// <summary>
    /// Tên trạng thái dễ đọc cho frontend.
    /// </summary>
    public static string StatusNameOf(int status) => status switch
    {
        (int)SellerStatus.Pending => "Đang chờ duyệt",
        (int)SellerStatus.Approved => "Đã được duyệt",
        (int)SellerStatus.Rejected => "Bị từ chối",
        _ => "Chưa đăng ký bán"
    };

    private static string BuildAdminNotificationBody(ApplicationUser user, string? note)
    {
        var noteHtml = string.IsNullOrWhiteSpace(note)
            ? string.Empty
            : $"<p><b>Ghi chú:</b> {System.Net.WebUtility.HtmlEncode(note.Trim())}</p>";

        return $@"<p>Có tài khoản mới yêu cầu trở thành người bán.</p>
            <ul>
                <li><b>Họ tên:</b> {System.Net.WebUtility.HtmlEncode(user.FullName)}</li>
                <li><b>Email:</b> {System.Net.WebUtility.HtmlEncode(user.Email)}</li>
                <li><b>Trường:</b> {System.Net.WebUtility.HtmlEncode(user.SchoolName ?? "-")}</li>
                <li><b>Mã SV:</b> {System.Net.WebUtility.HtmlEncode(user.StudentCode ?? "-")}</li>
            </ul>
            {noteHtml}";
    }

    /// <summary>
    /// Gửi mail cho tất cả Admin để họ biết có yêu cầu mới.
    /// Không để lỗi gửi mail làm hỏng luồng nghiệp vụ.
    /// </summary>
    private async Task NotifyAdminsAsync(
        ApplicationUser requester,
        string subject,
        string body,
        CancellationToken ct)
    {
        var admins = await _userRepo.ListAsync(u => u.Role == (int)UserRole.Admin, ct);

        foreach (var admin in admins)
        {
            await SendMailSafeAsync(admin.Email, admin.FullName, subject, body, ct);
        }
    }

    /// <summary>
    /// Gửi mail nhưng nuốt lỗi — lỗi SMTP không được làm sập request.
    /// </summary>
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
            // Bỏ qua — nghiệp vụ chính đã được ghi nhận vào DB.
        }
    }
}
