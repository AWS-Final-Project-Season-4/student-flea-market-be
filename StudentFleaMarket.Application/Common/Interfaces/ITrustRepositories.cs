using StudentFleaMarket.Application.DTOs.Admin;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Repository cho Review (đánh giá uy tín).
///
/// Số đánh giá có TÍNH vào điểm uy tín được lọc bằng
/// <see cref="CountsTowardsTrustAsync"/> — đơn giá 0 (tặng miễn phí) và
/// đánh giá đã bị vô hiệu đều bị loại.
/// </summary>
public interface IReviewRepository : IRepository<Review>
{
    /// <summary>
    /// Lấy đánh giá của một đơn giao dịch (kèm quan hệ đã load).
    /// </summary>
    Task<List<Review>> GetByTransactionAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết một đánh giá kèm quan hệ (Reviewer, Reviewee) đã load.
    /// Dùng khi cần hiển thị tên người đánh giá.
    /// </summary>
    Task<Review?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy đánh giá mà một người đã viết cho một đơn (để kiểm tra trùng).
    /// </summary>
    Task<Review?> GetByTransactionAndReviewerAsync(
        Guid transactionId,
        Guid reviewerId,
        CancellationToken ct = default);

    /// <summary>
    /// Đánh giá nhận được của một tài khoản (mới nhất trước), phân trang.
    /// </summary>
    Task<(List<Review> Reviews, int TotalCount)> ListReceivedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeVoided,
        CancellationToken ct = default);

    /// <summary>
    /// Tính điểm đánh giá trung bình của một tài khoản — CHỈ tính đánh giá hợp lệ:
    ///   - chưa bị vô hiệu (IsVoided = false)
    ///   - thuộc đơn giao dịch có giá trị &gt; 0 (không tính đơn tặng miễn phí)
    ///
    /// Trả về (Average, Count). Average = null nếu chưa có đánh giá hợp lệ.
    /// </summary>
    Task<(decimal? Average, int Count)> GetRatingStatsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Tổng số đánh giá nhận được (kể cả đã vô hiệu) — để hiển thị
    /// "3 đánh giá (1 đã bị ẩn)".
    /// </summary>
    Task<int> CountReceivedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Đếm tổng số đánh giá đã bị vô hiệu hoá trên toàn hệ thống.
    /// Dùng cho bảng điều khiển kiểm duyệt.
    /// </summary>
    Task<int> CountVoidedAsync(CancellationToken ct = default);

    /// <summary>
    /// Vô hiệu hoá các đánh giá mà người bị báo cáo đã viết trong một đơn giao dịch.
    ///
    /// LƯU Ý: lọc theo <c>ReviewerId</c> (NGƯỜI VIẾT), không phải <c>RevieweeId</c>
    /// (người được đánh giá). Mục đích là vô hiệu đánh giá 5 sao mà kẻ lừa đảo
    /// để lại cho bên bị hơn trước khi bùng hàng / tráo hàng.
    ///
    /// Trả về số đánh giá bị vô hiệu.
    /// </summary>
    Task<int> VoidReviewsForTransactionAsync(
        Guid transactionId,
        Guid reviewerId,
        Guid reportId,
        Guid staffUserId,
        string reason,
        CancellationToken ct = default);
}

/// <summary>
/// Repository cho Report (báo cáo vi phạm).
/// </summary>
public interface IReportRepository : IRepository<Report>
{
    /// <summary>
    /// Lấy report kèm quan hệ (Reporter, ReportedUser, Item, Transaction) đã load.
    /// </summary>
    Task<Report?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Danh sách report cho bảng kiểm duyệt, lọc theo trạng thái, phân trang.
    /// </summary>
    Task<(List<Report> Reports, int TotalCount)> ListForModerationAsync(
        int? status,
        int? reason,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Report do một người gửi, phân trang (để người dùng theo dõi report của mình).
    /// </summary>
    Task<(List<Report> Reports, int TotalCount)> ListByReporterAsync(
        Guid reporterId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Số report ĐÃ ĐƯỢC XÁC NHẬN là có lý do, nhắm vào một tài khoản.
    ///
    /// "Đã xác nhận" = Status = Resolved VÀ có hành động thực sự được thực hiện
    /// (không phải report bị bác). Report bị bác (Rejected) không tính.
    ///
    /// Đây là tín hiệu làm tài khoản bị gắn badge ⚠ Cần cảnh giác.
    /// </summary>
    Task<int> CountConfirmedAgainstUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Đếm report theo trạng thái — dùng cho bảng điều khiển.
    /// </summary>
    Task<Dictionary<int, int>> CountByStatusAsync(CancellationToken ct = default);
}
