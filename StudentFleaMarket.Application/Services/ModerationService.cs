using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// KIỂM DUYỆT — xử lý report vi phạm (phía Admin & Moderator).
///
/// CÁC HÀNH ĐỘNG XỬ LÝ (nhân viên chọn, không có mặc định):
///
/// | action         | tác động
/// |----------------|---------------------------------------------------------------
/// | reject         | Bác report — không có vi phạm. KHÔNG tính vào uy tín.
/// | hide_item      | Ẩn tin đăng bị báo cáo (IsHidden). Không xoá, có thể bỏ ẩn.
/// | ban_user       | Cấm tài khoản bị báo cáo (login bị chặn, tin bị gỡ, session bị thu hồi).
/// | revoke_seller  | Thu hồi quyền bán của tài khoản bị báo cáo.
/// | void_review    | Vô hiệu đánh giá mà bên bị báo cáo đã viết trong đơn giao dịch liên quan.
/// | none           | Xác nhận có vi phạm nhưng không cần hành động gì thêm.
///
/// VÌ SAO "void_review" LẠI QUAN TRỌNG?
///   Kẻ lừa đảo chiếm món rồi bùng hàng / tráo hàng, kèm theo để lại đánh
///   giá 5 sao cho bên bị hơn để "rửa" uy tín. Nếu chỉ cấm tài khoản mà
///   không vô hiệu đánh giá thì đánh giá 5 sao giả vẫn được tính vào điểm
///   uy tín — lỗ hổng này phải đóng lại, nếu không toàn bộ cơ chế "điểm uy
///   tín" sẽ bị chính kẻ gian lợi dụng để tự tạo uy tín.
///
/// VÌ SAO "reject" KHÔNG TÍNH VÀO UY TÍN?
///   Nếu report bị bác mà vẫn tính vào uy tín, thì kẻ xấu chỉ cần bị report
///   rồi được bác là mất uy tín — tức là chỉ cần bị report là đủ. Điều đó
///   khiến hệ thống dễ bị lạm dụng để hạ uy tín người khác.
/// </summary>
public class ModerationService : IModerationService
{
    private const string ActionReject = "reject";
    private const string ActionHideItem = "hide_item";
    private const string ActionBanUser = "ban_user";
    private const string ActionRevokeSeller = "revoke_seller";
    private const string ActionVoidReview = "void_review";
    private const string ActionNone = "none";

    private readonly IReportRepository _reportRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAdminUserService _adminUserService;

    public ModerationService(
        IReportRepository reportRepo,
        IReviewRepository reviewRepo,
        IItemRepository itemRepo,
        IUserRepository userRepo,
        ITransactionRepository txRepo,
        ICategoryRepository categoryRepo,
        IAdminUserService adminUserService)
    {
        _reportRepo = reportRepo;
        _reviewRepo = reviewRepo;
        _itemRepo = itemRepo;
        _userRepo = userRepo;
        _txRepo = txRepo;
        _categoryRepo = categoryRepo;
        _adminUserService = adminUserService;
    }

    // =====================================================================
    // LIST / GET
    // =====================================================================
    public async Task<PagedResult<ModerationReportDto>> ListReportsAsync(
        int? status,
        int? reason,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (reports, total) = await _reportRepo.ListForModerationAsync(status, reason, page, pageSize, ct);

        var items = new List<ModerationReportDto>(reports.Count);
        foreach (var r in reports)
        {
            items.Add(await MapDtoAsync(r, ct));
        }

        return new PagedResult<ModerationReportDto>
        {
            Items = items,
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

    public async Task<ModerationReportDto> GetReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _reportRepo.GetDetailAsync(reportId, ct)
            ?? throw new NotFoundException("Không tìm thấy báo cáo.", "REPORT_NOT_FOUND");

        return await MapDtoAsync(report, ct);
    }

    // =====================================================================
    // MARK REVIEWING
    // =====================================================================
    public async Task<ModerationReportDto> MarkReviewingAsync(
        Guid staffUserId,
        Guid reportId,
        CancellationToken ct = default)
    {
        var actor = await GetStaffOrThrowAsync(staffUserId, ct);

        var report = await _reportRepo.GetByIdAsync(reportId, ct)
            ?? throw new NotFoundException("Không tìm thấy báo cáo.", "REPORT_NOT_FOUND");

        if ((ReportStatus)report.Status != ReportStatus.Pending)
            throw new ConflictException(
                $"Báo cáo đang ở trạng thái \"{ReportLabels.StatusName(report.Status)}\" nên không thể đánh dấu đang xem xét.",
                "REPORT_NOT_PENDING");

        report.Status = (int)ReportStatus.Reviewing;
        _reportRepo.Update(report);
        await _reportRepo.SaveChangesAsync(ct);

        _ = actor;

        return await GetReportAsync(reportId, ct);
    }

    // =====================================================================
    // RESOLVE — XỬ LÝ CHÍNH
    // =====================================================================
    public async Task<ModerationReportDto> ResolveAsync(
        Guid staffUserId,
        Guid reportId,
        ResolveReportDto request,
        CancellationToken ct = default)
    {
        var actor = await GetStaffOrThrowAsync(staffUserId, ct);

        var report = await _reportRepo.GetByIdAsync(reportId, ct)
            ?? throw new NotFoundException("Không tìm thấy báo cáo.", "REPORT_NOT_FOUND");

        if ((ReportStatus)report.Status is ReportStatus.Resolved or ReportStatus.Rejected)
            throw new ConflictException(
                "Báo cáo này đã được xử lý xong.",
                "REPORT_ALREADY_RESOLVED");

        var actions = request.Actions
            .Select(a => a.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (actions.Count == 0)
            throw new ValidationException("Phải chọn ít nhất một hành động xử lý.", "NO_ACTION_SELECTED");

        // "reject" và các hành động khác là MUTUALLY EXCLUSIVE.
        // Bác report = không có vi phạm; thực hiện hành động = có vi phạm.
        // Trộn 2 thứ lại là mâu thuẫn -> không cho phép.
        if (actions.Contains(ActionReject) && actions.Count > 1)
            throw new ValidationException(
                "Không thể vừa bác báo cáo vừa thực hiện hành động xử lý.",
                "CONFLICTING_ACTIONS");

        var note = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
        var summary = new List<string>();

        // ---------------------------------------------------------------
        // TRƯỜNG HỢP 1: BÁC REPORT — không có vi phạm.
        // ---------------------------------------------------------------
        if (actions.Contains(ActionReject))
        {
            report.Status = (int)ReportStatus.Rejected;
            summary.Add("Đã bác báo cáo (không có vi phạm)");
        }
        else
        {
            // ---------------------------------------------------------------
            // TRƯỜNG HỢP 2: XÁC NHẬN CÓ VI PHẠM + thực hiện hành động.
            // ---------------------------------------------------------------
            report.Status = (int)ReportStatus.Resolved;

            // --- Ẩn tin đăng ---
            if (actions.Contains(ActionHideItem))
            {
                var hidden = await HideReportedItemAsync(report, actor.Id, note, ct);
                summary.Add(hidden
                    ? "Đã ẩn tin đăng bị báo cáo"
                    : "Không có tin đăng để ẩn");
            }

            // --- Vô hiệu đánh giá (lớp phòng thủ chống tự chấm 5 sao) ---
            if (actions.Contains(ActionVoidReview))
            {
                var voided = await VoidReportedReviewsAsync(report, actor.Id, note, ct);
                summary.Add(voided > 0
                    ? $"Đã vô hiệu {voided} đánh giá không hợp lệ"
                    : "Không có đánh giá nào để vô hiệu");
            }

            // --- Cấm tài khoản ---
            if (actions.Contains(ActionBanUser))
            {
                var banned = await BanReportedUserAsync(report, actor, note, ct);
                summary.Add(banned
                    ? "Đã cấm tài khoản bị báo cáo"
                    : "Không cấm được tài khoản (tài khoản này không thuộc phạm vi được cấm)");
            }

            // --- Thu hồi quyền bán ---
            if (actions.Contains(ActionRevokeSeller))
            {
                var revoked = await RevokeSellerAsync(report, actor.Id, ct);
                summary.Add(revoked
                    ? "Đã thu hồi quyền bán"
                    : "Tài khoản không có quyền bán để thu hồi");
            }

            // --- Không cần hành động gì thêm ---
            if (actions.Contains(ActionNone) || summary.Count == 0)
                summary.Add("Đã xác nhận có vi phạm");
        }

        report.ResolvedById = actor.Id;
        report.ResolvedAt = DateTime.UtcNow;

        // Ghi chú: phần do nhân viên nhập + tóm tắt hành động đã thực hiện.
        report.AdminNote = BuildAdminNote(note, summary);

        _reportRepo.Update(report);
        await _reportRepo.SaveChangesAsync(ct);

        return await GetReportAsync(reportId, ct);
    }

    // =====================================================================
    // DASHBOARD
    // =====================================================================
    public async Task<ModerationDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var byStatus = await _reportRepo.CountByStatusAsync(ct);

        int Count(ReportStatus s) => byStatus.TryGetValue((int)s, out var c) ? c : 0;

        var hiddenItems = await _itemRepo.CountHiddenAsync(ct);

        var bannedUsers = await _userRepo.ListForAdminAsync((int)UserStatus.Banned, null, 1, 1, ct);

        var categories = await _categoryRepo.ListWithCountsAsync(ct);

        var voidedReviews = await _reviewRepo.CountVoidedAsync(ct);

        return new ModerationDashboardDto
        {
            PendingReportCount = Count(ReportStatus.Pending),
            ReviewingReportCount = Count(ReportStatus.Reviewing),
            ResolvedReportCount = Count(ReportStatus.Resolved),
            RejectedReportCount = Count(ReportStatus.Rejected),
            HiddenItemCount = hiddenItems,
            BannedUserCount = bannedUsers.TotalCount,
            ActiveCategoryCount = categories.Count(c => c.Category.IsActive),
            TotalCategoryCount = categories.Count,
            VoidedReviewCount = voidedReviews
        };
    }

    // =====================================================================
    // HELPERS — CÁC HÀNH ĐỘNG
    // =====================================================================

    /// <summary>
    /// Ẩn tin đăng bị báo cáo.
    /// </summary>
    private async Task<bool> HideReportedItemAsync(
        Report report,
        Guid staffUserId,
        string? note,
        CancellationToken ct)
    {
        if (!report.ItemId.HasValue)
            return false;

        var reason = string.IsNullOrWhiteSpace(note)
            ? "Tin đăng bị ẩn do vi phạm sau khi xử lý báo cáo."
            : note;

        return await _itemRepo.HideAsync(report.ItemId.Value, staffUserId, reason, ct);
    }

    /// <summary>
    /// Vô hiệu đánh giá mà bên bị báo cáo đã viết trong đơn giao dịch liên quan.
    ///
    /// CHỈ vô hiệu trong phạm vi ĐƠN GIAO DỊCH của report — không đụng tới
    /// các đánh giá hợp lệ khác của tài khoản. Đây là hành động có tỷ lệ
    /// "đúng" cao nhất: report bùng hàng / tráo hàng chắc chắn liên quan tới
    /// chính đơn đó.
    /// </summary>
    private async Task<int> VoidReportedReviewsAsync(
        Report report,
        Guid staffUserId,
        string? note,
        CancellationToken ct)
    {
        if (!report.TransactionId.HasValue || !report.ReportedUserId.HasValue)
            return 0;

        var reason = string.IsNullOrWhiteSpace(note)
            ? "Đánh giá bị vô hiệu do nhân viên xác nhận vi phạm trong giao dịch này."
            : note;

        return await _reviewRepo.VoidReviewsForTransactionAsync(
            report.TransactionId.Value,
            report.ReportedUserId.Value,
            report.Id,
            staffUserId,
            reason,
            ct);
    }

    /// <summary>
    /// Cấm tài khoản bị báo cáo — dùng lại AdminUserService để luật cấm
    /// (phân tầng quyền, thu hồi session, gỡ tin, gửi email) chỉ có 1 nơi.
    /// </summary>
    private async Task<bool> BanReportedUserAsync(
        Report report,
        ApplicationUser actor,
        string? note,
        CancellationToken ct)
    {
        if (!report.ReportedUserId.HasValue)
            return false;

        var reason = string.IsNullOrWhiteSpace(note)
            ? "Vi phạm bị xác nhận qua báo cáo của người dùng."
            : note;

        try
        {
            await _adminUserService.BanUserAsync(actor.Id, report.ReportedUserId.Value, reason, ct);
            return true;
        }
        catch (ForbiddenException)
        {
            // Không đủ quyền cấm (ví dụ đích là Admin / Moderator) -> bỏ qua,
            // các hành động khác vẫn được thực hiện.
            return false;
        }
    }

    /// <summary>
    /// Thu hồi quyền bán của tài khoản bị báo cáo.
    /// </summary>
    private async Task<bool> RevokeSellerAsync(Report report, Guid staffUserId, CancellationToken ct)
    {
        if (!report.ReportedUserId.HasValue)
            return false;

        var target = await _userRepo.GetByIdAsync(report.ReportedUserId.Value, ct);

        if (target is null)
            return false;

        if (target.SellerStatus != (int)SellerStatus.Approved)
            return false;

        target.SellerStatus = (int)SellerStatus.None;
        target.SellerApprovedAt = null;
        target.SellerApprovedByUserId = null;
        target.SellerRejectionReason = "Quyền bán bị thu hồi do vi phạm bị xác nhận qua báo cáo.";
        target.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(target);
        await _userRepo.SaveChangesAsync(ct);

        return true;
    }

    // =====================================================================
    // HELPERS — CHUNG
    // =====================================================================

    private async Task<ApplicationUser> GetStaffOrThrowAsync(Guid staffUserId, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(staffUserId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (!StaffPermission.CanHandleReports(user))
            throw new ForbiddenException(
                "Chỉ quản trị viên và kiểm duyệt viên mới được xử lý báo cáo.",
                "FORBIDDEN");

        return user;
    }

    private static string BuildAdminNote(string? staffNote, List<string> summary)
    {
        var actions = string.Join("; ", summary);

        return string.IsNullOrWhiteSpace(staffNote)
            ? $"Hành động: {actions}"
            : $"Hành động: {actions}. Ghi chú: {staffNote}";
    }

    private async Task<ModerationReportDto> MapDtoAsync(Report report, CancellationToken ct)
    {
        bool? txCompleted = null;

        if (report.TransactionId.HasValue)
        {
            var tx = await _txRepo.GetByIdAsync(report.TransactionId.Value, ct);
            txCompleted = tx is not null
                ? tx.Status == (int)TransactionStatus.Completed
                : null;
        }

        return new ModerationReportDto
        {
            Id = report.Id,
            Reason = report.Reason,
            ReasonName = ReportLabels.ReasonName(report.Reason),
            Status = report.Status,
            StatusName = ReportLabels.StatusName(report.Status),
            Description = report.Description,
            ReporterId = report.ReporterId,
            ReporterName = report.Reporter?.FullName ?? "",
            ReportedUserId = report.ReportedUserId,
            ReportedUserName = report.ReportedUser?.FullName,
            ItemId = report.ItemId,
            ItemTitle = report.Item?.Title,
            ItemCode = report.Item?.ItemCode,
            TransactionId = report.TransactionId,
            ConversationId = report.ConversationId,
            TransactionCompleted = txCompleted,
            AdminNote = report.AdminNote,
            ResolvedById = report.ResolvedById,
            ResolvedByName = report.ResolvedBy?.FullName,
            CreatedAt = report.CreatedAt,
            ResolvedAt = report.ResolvedAt
        };
    }
}
