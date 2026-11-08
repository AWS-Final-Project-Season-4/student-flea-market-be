using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Seller;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý năng lực BÁN HÀNG (Seller) của người dùng.
///
/// "Seller" không phải một UserRole riêng — người bán vẫn là User và vẫn
/// mua / yêu thích / sửa hồ sơ bình thường. Admin là bên cấp quyền bán.
///
/// Luồng:
///   1. User gửi yêu cầu  -> RequestAsync        (SellerStatus = Pending)
///   2. Admin duyệt       -> ApproveAsync        (SellerStatus = Approved)
///   3. Admin từ chối     -> RejectAsync         (SellerStatus = Rejected)
///   4. Admin thu hồi     -> RevokeAsync         (SellerStatus = None)
///
/// Admin và Moderator (kiểm duyệt viên) đều thực hiện được các thao tác
/// duyệt / từ chối / thu hồi ở bước 2-4.
/// </summary>
public interface ISellerService
{
    /// <summary>
    /// Lấy trạng thái quyền bán của chính người đang đăng nhập.
    /// </summary>
    Task<SellerStatusDto> GetStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Người dùng gửi yêu cầu trở thành người bán (chờ Admin duyệt).
    /// </summary>
    Task<SellerStatusDto> RequestAsync(Guid userId, SellerRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Người dùng hủy yêu cầu đang chờ duyệt.
    /// </summary>
    Task<SellerStatusDto> CancelRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Admin xem danh sách yêu cầu trở thành người bán (lọc theo trạng thái, phân trang).
    /// </summary>
    Task<PagedResult<AdminSellerRequestDto>> ListRequestsAsync(
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Admin / Moderator cấp quyền bán cho một tài khoản.
    /// </summary>
    Task<AdminSellerRequestDto> ApproveAsync(Guid adminUserId, Guid targetUserId, CancellationToken ct = default);

    /// <summary>
    /// Admin / Moderator từ chối yêu cầu trở thành người bán.
    /// </summary>
    Task<AdminSellerRequestDto> RejectAsync(
        Guid adminUserId,
        Guid targetUserId,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Admin / Moderator thu hồi quyền bán đã cấp (đưa về trạng thái chưa được cấp quyền).
    /// </summary>
    Task<AdminSellerRequestDto> RevokeAsync(Guid adminUserId, Guid targetUserId, CancellationToken ct = default);

    /// <summary>
    /// HỒ SƠ NĂNG LỰC BÁN HÀNG (public) — xem tín hiệu uy tín, điểm đánh giá,
    /// số giao dịch hoàn thành và các đánh giá gần đây của một người bán.
    ///
    /// Endpoint: GET /api/Sellers/{sellerId}/profile
    ///
    /// Đây là nơi người MUA kiểm tra "người này có đáng tin không" trước khi
    /// gặp mặt — vì vậy nó là endpoint công khai (không cần đăng nhập).
    /// </summary>
    Task<SellerProfileDto> GetSellerProfileAsync(
        Guid sellerId,
        Guid? viewerId,
        CancellationToken ct = default);
}
