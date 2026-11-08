using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trust;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Tính tín hiệu uy tín của tài khoản.
///
/// NGUỒN SỰ THẬT:
///   - Số giao dịch hoàn thành -> <see cref="ITransactionRepository"/>
///   - Điểm đánh giá trung bình  -> <see cref="IReviewRepository"/>
///   - Số vi phạm đã xác nhận   -> <see cref="IReportRepository"/>
///   - Ngưỡng xếp mức          -> <see cref="TrustPolicy"/>
///
/// Vì sao KHÔNG tính điểm tổng hợp 0–100?
///   Một con số duy nhất dễ bị "mua" bằng vài giao dịch giá trị thấp và
///   không truyền đạt được cho người dùng. Thay vào đó ta trả về TỪNG tín
///   hiệu (để người dùng tự đánh giá) cộng với 1 badge mức do hệ thống quyết
///   định. Ngưỡng mức chỉ nằm 1 chỗ duy nhất: <see cref="TrustPolicy"/>.
/// </summary>
public class TrustService : ITrustService
{
    private readonly ITransactionRepository _txRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IReportRepository _reportRepo;

    public TrustService(
        ITransactionRepository txRepo,
        IReviewRepository reviewRepo,
        IReportRepository reportRepo)
    {
        _txRepo = txRepo;
        _reviewRepo = reviewRepo;
        _reportRepo = reportRepo;
    }

    public async Task<TrustMetricsDto> GetMetricsAsync(Guid userId, CancellationToken ct = default)
    {
        var (sales, purchases) = await _txRepo.CountCompletedForUserAsync(userId, ct);
        var (average, ratingCount) = await _reviewRepo.GetRatingStatsAsync(userId, ct);
        var totalReviews = await _reviewRepo.CountReceivedAsync(userId, ct);
        var confirmedReports = await _reportRepo.CountConfirmedAgainstUserAsync(userId, ct);

        return Build(sales, purchases, average, ratingCount, totalReviews, confirmedReports);
    }

    public async Task<Dictionary<Guid, TrustMetricsDto>> GetMetricsBatchAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();

        var result = new Dictionary<Guid, TrustMetricsDto>();
        if (ids.Count == 0)
            return result;

        // Từng tài khoản 1 vòng truy vấn theo nhóm số liệu (3 nhóm) —
        // tổng 3 * ids.Count truy vấn thay vì N+1 * 3. Với danh sách ngắn
        // (20-50 tin) đây là mức chấp nhận được và code dễ đọc hơn nhiều.
        foreach (var id in ids)
        {
            result[id] = await GetMetricsAsync(id, ct);
        }

        return result;
    }

    private static TrustMetricsDto Build(
        int sales,
        int purchases,
        decimal? average,
        int ratingCount,
        int totalReviews,
        int confirmedReports)
    {
        var level = TrustPolicy.DetermineLevel(sales + purchases, confirmedReports, average);

        return new TrustMetricsDto
        {
            Level = (int)level,
            LevelName = TrustPolicy.LevelName(level),
            Badge = TrustPolicy.LevelBadge(level),
            Color = TrustPolicy.LevelColor(level),
            CompletedSalesCount = sales,
            CompletedPurchaseCount = purchases,
            CompletedTransactionsCount = sales + purchases,
            RatingAverage = average,
            RatingCount = ratingCount,
            TotalReviewCount = totalReviews,
            ConfirmedReportCount = confirmedReports
        };
    }
}
