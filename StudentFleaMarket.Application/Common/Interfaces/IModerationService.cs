using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// KIỂM DUYỆT VIÊN (Moderation) — xử lý report vi phạm.
///
/// Đây là phần "răng" của Feature 5. Nguyên tắc thiết kế:
///
/// 1) HÀNH ĐỘNG TÚY BIẾN, KHÔNG PHẢI HÀNH ĐỘNG CỐ ĐỊNH:
///    Không có hành động nào là mặc định cho mọi report. Nhân viên chọn
///    những hành động phù hợp: ẩn tin, cấm tài khoản, thu hồi quyền bán,
///    vô hiệu đánh giá, hoặc chỉ bác report (không có vi phạm).
///
/// 2) VÔ HIỆU ĐÁNH GIÁ (void_review) — LỚP PHÒNG THỦ CUỐI CÙNG:
///    Kẻ lừa đảo thường chiếm món rồi bùng hàng / tráo hàng, kèm theo để
///    lại một đánh giá 5 sao để "rửa" uy tín của bên bị hơn. Nếu chỉ cấm
///    tài khoản mà không vô hiệu đánh giá, thì đánh giá 5 sao giả vẫn còn
///    và vẫn được tính vào điểm uy tín — lỗ hổng này phải đóng lại.
///
/// 3) MỌI HÀNH ĐỘNG ĐỂU LƯU VẾT:
///    Ai xử lý, lúc nào, làm gì, ghi chú gì — đều lưu lại trong bảng
///    Reports (ResolvedById / ResolvedAt / AdminNote) và trên bản ghi bị
///    tác động (Review.VoidedByUserId, Item.HiddenByUserId).
///
/// 4) TẦNG QUYỀN:
///    - Admin và Moderator đều xử lý được report.
///    - Cấm tài khoản Moderator / Admin: chỉ Admin (dùng lại
///      AdminUserService.BanUserAsync để không lệch luật).
/// </summary>
public interface IModerationService
{
    /// <summary>
    /// Danh sách report cho bảng kiểm duyệt (lọc theo trạng thái / lý do, phân trang).
    /// </summary>
    Task<PagedResult<ModerationReportDto>> ListReportsAsync(
        int? status,
        int? reason,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Chi tiết một report.
    /// </summary>
    Task<ModerationReportDto> GetReportAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Xử lý một report với danh sách hành động được chọn.
    /// </summary>
    Task<ModerationReportDto> ResolveAsync(
        Guid staffUserId,
        Guid reportId,
        ResolveReportDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Đánh dấu report đang được xem xét (để nhiều nhân viên không xử lý
    /// trùng cùng 1 report).
    /// </summary>
    Task<ModerationReportDto> MarkReviewingAsync(
        Guid staffUserId,
        Guid reportId,
        CancellationToken ct = default);

    /// <summary>
    /// Thống kê tổng quan cho bảng điều khiển kiểm duyệt.
    /// </summary>
    Task<ModerationDashboardDto> GetDashboardAsync(CancellationToken ct = default);
}
