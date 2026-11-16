using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.Application.DTOs.Seller;

/// <summary>
/// HỒ SƠ NĂNG LỰC BÁN HÀNG (public) — trả về cho bất kỳ ai xem profile.
///
/// Endpoint: GET /api/Sellers/{sellerId}/profile
///
/// Mục đích: người mua cần biết "người này có đáng tin không" TRƯỚC khi gặp
/// mặt. Đây là nơi hiển thị:
///   - Badge uy tín (🥉 / 🥈 / 🥇 / ⚠)
///   - Điểm đánh giá trung bình + số lượt đánh giá
///   - Số giao dịch đã hoàn thành (mua + bán)
///   - Số vi phạm đã được xác nhận
///   - Danh sách đánh giá gần đây
/// </summary>
public class SellerProfileDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? SchoolName { get; init; }
    public string? StudentCode { get; init; }
    public string? CommonArea { get; init; }

    public DateTime MemberSince { get; init; }

    /// <summary>Ngày tài khoản được duyệt quyền bán (null nếu chưa được duyệt).</summary>
    public DateTime? SellerApprovedAt { get; init; }

    /// <summary>Số tin đăng đang bán hiện tại.</summary>
    public int ActiveItemCount { get; init; }

    /// <summary>
    /// Tổng hợp tín hiệu uy tín — badge + điểm + số giao dịch + số vi phạm.
    /// </summary>
    public TrustMetricsDto Trust { get; init; } = null!;

    /// <summary>
    /// Đánh giá gần đây (tối đa 10 đánh giá mới nhất, chỉ đánh giá hợp lệ).
    /// </summary>
    public List<ReviewDto> RecentReviews { get; init; } = new();

    /// <summary>
    /// Người xem hiện tại đã đánh giá người bán này chưa (tiện cho UI).
    /// </summary>
    public bool HasViewerReviewed { get; init; }
}
