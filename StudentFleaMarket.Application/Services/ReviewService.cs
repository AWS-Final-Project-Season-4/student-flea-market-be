using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trust;
using StudentFleaMarket.Application.DTOs.Trading;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Xử lý đánh giá uy tín sau giao dịch.
///
/// QUY TẮC (nguồn sự thật: <see cref="ReviewPolicy"/>):
///   1. Chỉ đánh giá được đơn đã HOÀN THÀNH.
///   2. Đơn Purchase/Free : 1 chiều (mua -> bán). Đơn Exchange : 2 chiều.
///   3. Mỗi người chỉ đánh giá 1 lần cho mỗi đơn.
///   4. Sửa được trong 14 ngày.
///   5. Đơn giá 0 vẫn đánh giá được, nhưng không tính vào điểm uy tín.
///
/// ĐIỂM QUAN TRỌNG — CHỐNG KẺ LỪA ĐẢO TỰ CHẤM 5 SAO:
///   Kẻ lừa đảo thường chiếm món rồi bùng hàng / tráo hàng, và để lại đánh
///   giá 5 sao cho bên bị hơn để "rửa" uy tín. Vì vậy:
///   - Đánh giá có thể bị VÔ HIỆU (IsVoided) khi nhân viên xác nhận report
///     là có lý do. Đánh giá bị vô hiệu không tính vào điểm uy tín nữa.
///   - Việc vô hiệu được thực hiện ở ModerationService, không phải ở đây.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly ITransactionRepository _txRepo;

    public ReviewService(IReviewRepository reviewRepo, ITransactionRepository txRepo)
    {
        _reviewRepo = reviewRepo;
        _txRepo = txRepo;
    }

    // =====================================================================
    // CREATE
    // =====================================================================
    public async Task<ReviewDto> CreateAsync(
        Guid userId,
        Guid transactionId,
        CreateReviewDto request,
        CancellationToken ct = default)
    {
        var tx = await _txRepo.GetDetailAsync(transactionId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        // Chỉ đánh giá được đơn đã hoàn thành — đơn đang xử lý / đã hủy thì
        // chưa có gì để đánh giá.
        if ((TransactionStatus)tx.Status != TransactionStatus.Completed)
            throw new ValidationException(
                $"Chỉ được đánh giá giao dịch đã hoàn thành. Giao dịch hiện đang ở trạng thái \"{TransactionLabels.StatusName(tx.Status)}\".",
                "TRANSACTION_NOT_COMPLETED");

        // Xác định ai được đánh giá ai.
        var (revieweeId, errorCode, errorMessage) = ResolveReviewee(tx, userId);

        if (revieweeId is null)
            throw new ForbiddenException(errorMessage!, errorCode!);

        var targetRevieweeId = revieweeId.Value;

        // Không được tự đánh giá mình (phòng ngừa lỗi dữ liệu).
        if (targetRevieweeId == userId)
            throw new ValidationException("Bạn không thể tự đánh giá mình.", "CANNOT_REVIEW_SELF");

        // Mỗi người chỉ đánh giá 1 lần cho mỗi đơn.
        var existing = await _reviewRepo.GetByTransactionAndReviewerAsync(transactionId, userId, ct);
        if (existing is not null)
            throw new ConflictException(
                existing.IsVoided
                    ? "Đánh giá của bạn cho giao dịch này đã bị vô hiệu do vi phạm."
                    : "Bạn đã đánh giá giao dịch này rồi.",
                "ALREADY_REVIEWED");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ReviewerId = userId,
            RevieweeId = targetRevieweeId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddAsync(review, ct);
        await _reviewRepo.SaveChangesAsync(ct);

        return MapDto(review, tx.Item?.Title ?? "", userId);
    }

    // =====================================================================
    // UPDATE (trong 14 ngày)
    // =====================================================================
    public async Task<ReviewDto> UpdateAsync(
        Guid userId,
        Guid reviewId,
        UpdateReviewDto request,
        CancellationToken ct = default)
    {
        var review = await _reviewRepo.GetDetailAsync(reviewId, ct)
            ?? throw new NotFoundException("Không tìm thấy đánh giá.", "REVIEW_NOT_FOUND");

        if (review.ReviewerId != userId)
            throw new ForbiddenException("Bạn chỉ có thể sửa đánh giá của mình.", "NOT_REVIEW_OWNER");

        // Đánh giá đã bị vô hiệu -> không sửa được (nó không còn được tính).
        if (review.IsVoided)
            throw new ValidationException(
                "Đánh giá này đã bị vô hiệu do vi phạm nên không thể sửa.",
                "REVIEW_IS_VOIDED");

        var now = DateTime.UtcNow;
        if (!ReviewPolicy.IsWithinEditWindow(review.CreatedAt, now))
            throw new ValidationException(
                $"Chỉ được sửa đánh giá trong {ReviewPolicy.EditWindowDays} ngày kể từ khi viết.",
                "REVIEW_EDIT_WINDOW_EXPIRED");

        review.Rating = request.Rating;
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        review.UpdatedAt = now;

        _reviewRepo.Update(review);
        await _reviewRepo.SaveChangesAsync(ct);

        var tx = await _txRepo.GetDetailAsync(review.TransactionId, ct);

        return MapDto(review, tx?.Item?.Title ?? "", userId);
    }

    // =====================================================================
    // GET
    // =====================================================================
    public async Task<ReviewDto> GetByIdAsync(Guid reviewId, Guid viewerId, CancellationToken ct = default)
    {
        var review = await _reviewRepo.GetDetailAsync(reviewId, ct)
            ?? throw new NotFoundException("Không tìm thấy đánh giá.", "REVIEW_NOT_FOUND");

        var tx = await _txRepo.GetDetailAsync(review.TransactionId, ct);

        return MapDto(review, tx?.Item?.Title ?? "", viewerId);
    }

    public async Task<PagedResult<ReviewDto>> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (reviews, total) = await _reviewRepo.ListReceivedByUserAsync(
            userId, page, pageSize, includeVoided: false, ct);

        var result = new List<ReviewDto>(reviews.Count);
        foreach (var r in reviews)
        {
            var tx = await _txRepo.GetDetailAsync(r.TransactionId, ct);
            result.Add(MapDto(r, tx?.Item?.Title ?? "", userId));
        }

        return new PagedResult<ReviewDto>
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

    // =====================================================================
    // ELIGIBILITY
    // =====================================================================
    public async Task<ReviewEligibilityDto> GetEligibilityAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default)
    {
        var tx = await _txRepo.GetDetailAsync(transactionId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        var isParticipant = tx.InitiatorId == userId || tx.CounterpartyId == userId;
        var isTwoWay = ReviewPolicy.IsTwoWay(tx.Type);
        var countsTowardsTrust = ReviewPolicy.CountsTowardsTrust(tx.Amount);

        if (!isParticipant)
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Reason = "Bạn không phải người tham gia giao dịch này.",
                CountsTowardsTrust = countsTowardsTrust,
                IsTwoWay = isTwoWay
            };

        if ((TransactionStatus)tx.Status != TransactionStatus.Completed)
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Reason = "Chỉ được đánh giá giao dịch đã hoàn thành.",
                CountsTowardsTrust = countsTowardsTrust,
                IsTwoWay = isTwoWay
            };

        // Đơn 1 chiều: chỉ người mua đánh giá được người bán.
        if (!isTwoWay && tx.InitiatorId != userId)
            return new ReviewEligibilityDto
            {
                CanReview = false,
                Reason = "Giao dịch mua trực tiếp chỉ được người mua đánh giá người bán.",
                CountsTowardsTrust = countsTowardsTrust,
                IsTwoWay = isTwoWay
            };

        var existing = await _reviewRepo.GetByTransactionAndReviewerAsync(transactionId, userId, ct);

        return new ReviewEligibilityDto
        {
            CanReview = existing is null,
            AlreadyReviewed = existing is not null,
            Reason = existing is null
                ? null
                : (existing.IsVoided
                    ? "Đánh giá của bạn đã bị vô hiệu do vi phạm."
                    : "Bạn đã đánh giá giao dịch này rồi."),
            CountsTowardsTrust = countsTowardsTrust,
            IsTwoWay = isTwoWay
        };
    }

    public async Task<List<PendingReviewDto>> ListPendingReviewsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var transactions = await _txRepo.ListCompletedWithReviewsAsync(userId, ct);

        var result = new List<PendingReviewDto>();

        foreach (var tx in transactions)
        {
            var isTwoWay = ReviewPolicy.IsTwoWay(tx.Type);
            var countsTowardsTrust = ReviewPolicy.CountsTowardsTrust(tx.Amount);

            // Đơn 1 chiều -> chỉ người mua được đánh giá.
            if (!isTwoWay && tx.InitiatorId != userId)
                continue;

            // Đã đánh giá rồi -> không nằm trong danh sách "cần đánh giá".
            if (tx.Reviews.Any(r => r.ReviewerId == userId))
                continue;

            // Bên cần đánh giá là BÊN KIA của mình.
            var reviewee = tx.InitiatorId == userId ? tx.Counterparty : tx.Initiator;
            var revieweeId = tx.InitiatorId == userId ? tx.CounterpartyId : tx.InitiatorId;

            result.Add(new PendingReviewDto
            {
                TransactionId = tx.Id,
                ItemCode = tx.Item?.ItemCode ?? "",
                ItemTitle = tx.Item?.Title ?? "",
                ItemImageUrl = tx.Item?.Images?.FirstOrDefault()?.ImageUrl,
                RevieweeName = reviewee?.FullName ?? "Người dùng",
                RevieweeId = revieweeId,
                Amount = tx.Amount,
                CompletedAt = tx.CompletedAt ?? tx.CreatedAt,
                CountsTowardsTrust = countsTowardsTrust,
                IsTwoWay = isTwoWay
            });
        }

        return result;
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Xác định người được đánh giá dựa trên loại giao dịch.
    /// Trả về (revieweeId, errorCode, errorMessage). revieweeId = null nghĩa là
    /// không được phép đánh giá.
    /// </summary>
    private static (Guid? RevieweeId, string? ErrorCode, string? ErrorMessage) ResolveReviewee(
        Transaction tx,
        Guid userId)
    {
        var isTwoWay = ReviewPolicy.IsTwoWay(tx.Type);

        // Trường hợp 2 chiều (Exchange): ai cũng đánh giá được bên kia.
        if (isTwoWay)
        {
            if (tx.InitiatorId == userId)
                return (tx.CounterpartyId, null, null);

            if (tx.CounterpartyId == userId)
                return (tx.InitiatorId, null, null);

            return (null, "NOT_TRANSACTION_PARTICIPANT", "Bạn không phải người tham gia giao dịch này.");
        }

        // Trường hợp 1 chiều (Purchase / Free): chỉ người mua đánh giá người bán.
        if (tx.InitiatorId == userId)
            return (tx.CounterpartyId, null, null);

        if (tx.CounterpartyId == userId)
            return (null,
                "ONLY_BUYER_CAN_REVIEW",
                "Giao dịch mua trực tiếp chỉ được người mua đánh giá người bán.");

        return (null, "NOT_TRANSACTION_PARTICIPANT", "Bạn không phải người tham gia giao dịch này.");
    }

    private static ReviewDto MapDto(Review review, string itemTitle, Guid viewerId)
    {
        var now = DateTime.UtcNow;

        return new ReviewDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewerName = review.Reviewer?.FullName ?? "Người dùng",
            ReviewerAvatarUrl = review.Reviewer?.AvatarUrl,
            ItemTitle = itemTitle,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            CanEdit = review.ReviewerId == viewerId
                     && !review.IsVoided
                     && ReviewPolicy.IsWithinEditWindow(review.CreatedAt, now)
        };
    }
}
