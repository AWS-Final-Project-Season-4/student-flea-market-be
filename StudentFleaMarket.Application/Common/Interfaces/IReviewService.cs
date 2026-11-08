using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý đánh giá uy tín sau giao dịch.
///
/// Luật nghiệp vụ nằm ở <c>ReviewPolicy</c> — nguồn sự thật duy nhất.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Viết đánh giá cho một đơn giao dịch đã hoàn thành.
    ///
    /// - Đơn Purchase / Free : chỉ người MUA đánh giá được người BÁN.
    /// - Đơn Exchange        : cả 2 bên cùng đánh giá được nhau.
    /// </summary>
    Task<ReviewDto> CreateAsync(
        Guid userId,
        Guid transactionId,
        CreateReviewDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Sửa đánh giá đã viết (trong 14 ngày).
    /// </summary>
    Task<ReviewDto> UpdateAsync(
        Guid userId,
        Guid reviewId,
        UpdateReviewDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Xem một đánh giá.
    /// </summary>
    Task<ReviewDto> GetByIdAsync(Guid reviewId, Guid viewerId, CancellationToken ct = default);

    /// <summary>
    /// Đánh giá nhận được của một tài khoản (phân trang) — hiển thị trên profile.
    /// </summary>
    Task<PagedResult<ReviewDto>> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra người dùng có được đánh giá đơn này không, và lý do nếu không.
    /// Dùng để frontend bật/tắt nút "Viết đánh giá".
    /// </summary>
    Task<ReviewEligibilityDto> GetEligibilityAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Đơn giao dịch đã hoàn thành mà người dùng CHƯA đánh giá — dùng cho
    /// màn hình "Cần đánh giá" sau khi giao dịch xong.
    /// </summary>
    Task<List<PendingReviewDto>> ListPendingReviewsAsync(
        Guid userId,
        CancellationToken ct = default);
}
