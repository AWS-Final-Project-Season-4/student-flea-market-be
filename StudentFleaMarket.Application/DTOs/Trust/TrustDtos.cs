using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Trust;

// ============================================================ TRUST METRICS

/// <summary>
/// Tổng hợp tín hiệu uy tín của một tài khoản.
///
/// Đây là "nguồn sự thật" mà badge uy tín dựa vào.
/// Mọi trường đều là TÍN HIỆU TÍNH (signal) chứ không phải điểm số tổng hợp —
/// xem <c>TrustPolicy</c> để hiểu vì sao không dùng một con số 0–100.
/// </summary>
public class TrustMetricsDto
{
    /// <summary>TrustLevel: 1 = New, 2 = Member, 3 = Trusted, 4 = Warning.</summary>
    public int Level { get; init; }

    /// <summary>Tên mức (không emoji): "Mới tham gia" / "Đã thiết lập" / "Uy tín" / "Cần cảnh giác".</summary>
    public string LevelName { get; init; } = null!;

    /// <summary>Badge hiển thị kèm emoji: "🥇 Uy tín".</summary>
    public string Badge { get; init; } = null!;

    /// <summary>Mã màu gợi ý cho frontend.</summary>
    public string Color { get; init; } = null!;

    // ------------------------------------------------- Tín hiệu chính: giao dịch

    /// <summary>
    /// Số giao dịch đã HOÀN THÀNH mà tài khoản này là người BÁN.
    /// (Trong hệ thống: user là Counterparty của đơn.)
    /// </summary>
    public int CompletedSalesCount { get; init; }

    /// <summary>
    /// Số giao dịch đã HOÀN THÀNH mà tài khoản này là người MUA.
    /// (Trong hệ thống: user là Initiator của đơn.)
    /// </summary>
    public int CompletedPurchaseCount { get; init; }

    /// <summary>
    /// Tổng số giao dịch đã hoàn thành (mua + bán) — đây là TIÊU CHÍ CHÍNH
    /// để xếp mức uy tín.
    /// </summary>
    public int CompletedTransactionsCount { get; init; }

    // ------------------------------------------------- Tín hiệu phụ: đánh giá

    /// <summary>
    /// Điểm đánh giá trung bình (1–5), null nếu chưa có đánh giá hợp lệ nào.
    ///
    /// CHỈ tính đánh giá của giao dịch có giá trị &gt; 0 (đơn "nhận miễn phí"
    /// không được tính — tặng miễn phí không phải dịch vụ, ai cũng làm được).
    /// Đánh giá đã bị vô hiệu (IsVoided) cũng không tính.
    /// </summary>
    public decimal? RatingAverage { get; init; }

    /// <summary>Số đánh giá hợp lệ (dùng để tính trung bình).</summary>
    public int RatingCount { get; init; }

    /// <summary>Tổng số đánh gia nhận được, kể cả đã bị vô hiệu.</summary>
    public int TotalReviewCount { get; init; }

    // ------------------------------------------------- Tín hiệu phụ: vi phạm

    /// <summary>
    /// Số report ĐÃ ĐƯỢC NHÂN VIÊN XÁC NHẬN là có lý do.
    /// Report bị bác (Rejected) không tính.
    /// </summary>
    public int ConfirmedReportCount { get; init; }

    /// <summary>
    /// Đánh giá có thấp không (dưới ngưỡng 3.0) — dùng để hiển thị cảnh báo.
    /// </summary>
    public bool HasLowRating => RatingAverage.HasValue && RatingAverage < TrustPolicy.LowRatingThreshold;

    /// <summary>
    /// Tài khoản này có bị cảnh báo không (đã có vi phạm được xác nhận).
    /// </summary>
    public bool HasWarning => Level == (int)TrustLevel.Warning;
}
