using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Trust;

// ============================================================ REVIEW

/// <summary>
/// Viết đánh giá cho một đơn giao dịch đã hoàn thành.
///
/// PHẠM VI ĐÁNH GIÁ (đã chốt):
///   - Đơn Purchase / Free : CHỈ người MUA (Initiator) đánh giá được người BÁN.
///   - Đơn Exchange        : CẢ 2 bên cùng đánh giá được nhau.
///
/// Nếu đơn có giá trị 0 (tặng miễn phí) thì vẫn cho đánh giá, NHƯNG
/// đánh giá đó không được tính vào điểm uy tín (xem <see cref="TrustMetricsDto"/>).
/// </summary>
public class CreateReviewDto
{
    /// <summary>Số sao: 1 → 5.</summary>
    public int Rating { get; set; }

    /// <summary>Bình luận (không bắt buộc) — tối đa 500 ký tự.</summary>
    public string? Comment { get; set; }
}

public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Số sao phải từ 1 đến 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Bình luận không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}

/// <summary>
/// Sửa đánh giá đã viết.
///
/// CHỈ sửa được trong 14 ngày kể từ khi viết (xem ReviewPolicy.EditWindowDays).
/// </summary>
public class UpdateReviewDto
{
    /// <summary>Số sao mới: 1 → 5.</summary>
    public int Rating { get; set; }

    /// <summary>Bình luận mới (không bắt buộc) — tối đa 500 ký tự.</summary>
    public string? Comment { get; set; }
}

public class UpdateReviewDtoValidator : AbstractValidator<UpdateReviewDto>
{
    public UpdateReviewDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Số sao phải từ 1 đến 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Bình luận không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}

/// <summary>
/// Một đánh giá hiển thị trên profile người bán.
/// </summary>
public class ReviewDto
{
    public Guid Id { get; init; }
    public int Rating { get; init; }

    /// <summary>Nội dung bình luận (null nếu người đánh giá không viết).</summary>
    public string? Comment { get; init; }

    /// <summary>Tên người viết đánh giá.</summary>
    public string ReviewerName { get; init; } = null!;

    /// <summary>Ảnh đại diện người viết đánh giá.</summary>
    public string? ReviewerAvatarUrl { get; init; }

    /// <summary>Tên món đồ được đánh giá (giúp người mua hiểu đánh giá này nói về cái gì).</summary>
    public string ItemTitle { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    /// <summary>Thời điểm sửa (null nếu chưa từng sửa).</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Người đang xem còn sửa được đánh giá này không
    /// (trong 14 ngày & không bị vô hiệu).
    /// </summary>
    public bool CanEdit { get; init; }
}

/// <summary>
/// Một giao dịch đã hoàn thành mà người dùng CẦN đánh giá.
/// Dùng cho màn hình "Cần đánh giá" (sau khi giao dịch xong).
/// </summary>
public class PendingReviewDto
{
    /// <summary>Id đơn giao dịch — dùng để gọi <c>POST /api/Reviews/transactions/{id}</c>.</summary>
    public Guid TransactionId { get; init; }

    /// <summary>Mã hàng của món đồ (để người dùng nhớ mình đánh giá món gì).</summary>
    public string ItemCode { get; init; } = null!;

    /// <summary>Tên món đồ.</summary>
    public string ItemTitle { get; init; } = null!;

    /// <summary>Ảnh món đồ.</summary>
    public string? ItemImageUrl { get; init; }

    /// <summary>Tên người bị đánh giá (bên kia giao dịch).</summary>
    public string RevieweeName { get; init; } = null!;

    /// <summary>Id người bị đánh giá.</summary>
    public Guid RevieweeId { get; init; }

    /// <summary>Giá trị đơn — 0 = tặng miễn phí.</summary>
    public decimal Amount { get; init; }

    /// <summary>Ngày hoàn thành giao dịch.</summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>Đánh giá này có tính vào điểm uy tín không (đơn có giá trị &gt; 0).</summary>
    public bool CountsTowardsTrust { get; init; }

    /// <summary>Đánh giá 2 chiều (đơn đổi đồ) hay 1 chiều (đơn mua).</summary>
    public bool IsTwoWay { get; init; }
}

/// <summary>
/// Kết quả trả về khi người dùng bấm "viết đánh giá" — mô tả đơn giao dịch
/// kèm quyền được đánh giá hay không, để frontend không phải tự suy luận.
/// </summary>
public class ReviewEligibilityDto
{
    /// <summary>Người dùng có được đánh giá đơn này không.</summary>
    public bool CanReview { get; init; }

    /// <summary>
    /// Đánh giá đã tồn tại chưa (mỗi người chỉ đánh giá 1 lần / đơn).
    /// </summary>
    public bool AlreadyReviewed { get; init; }

    /// <summary>Lý do không được đánh giá (null nếu được đánh giá).</summary>
    public string? Reason { get; init; }

    /// <summary>Đơn có giá trị &gt; 0 không — quyết định đánh giá có tính vào uy tín không.</summary>
    public bool CountsTowardsTrust { get; init; }

    /// <summary>Đánh giá này là 1 chiều hay 2 chiều.</summary>
    public bool IsTwoWay { get; init; }
}
