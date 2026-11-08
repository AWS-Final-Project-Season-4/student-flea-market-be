using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Repositories;

/// <summary>
/// Repository cho Review — đánh giá uy tín.
///
/// LƯU Ý QUAN TRỌNG VỀ BỘ LỌC "TÍNH VÀO ĐIỂM UY TÍN":
///   Đánh giá chỉ được tính khi:
///     1. Chưa bị vô hiệu (IsVoided = false) — đánh giá của kẻ lừa đảo bị
///        loại sau khi nhân viên xác nhận report.
///     2. Thuộc đơn giao dịch có giá trị &gt; 0 — đơn "nhận miễn phí" không
///        phải dịch vụ, cho đánh giá vào thì mọi người đều 5 sao -> vô nghĩa.
/// </summary>
public class ReviewRepository : EfRepository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<List<Review>> GetByTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .Include(r => r.Transaction).ThenInclude(t => t.Item)
            .Where(r => r.TransactionId == transactionId)
            .ToListAsync(ct);

    public Task<Review?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Review?> GetByTransactionAndReviewerAsync(        Guid transactionId,
        Guid reviewerId,
        CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TransactionId == transactionId && r.ReviewerId == reviewerId, ct);

    public async Task<(List<Review> Reviews, int TotalCount)> ListReceivedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeVoided,
        CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Transaction).ThenInclude(t => t.Item)
            .Where(r => r.RevieweeId == userId);

        if (!includeVoided)
            q = q.Where(r => !r.IsVoided);

        var total = await q.CountAsync(ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var reviews = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (reviews, total);
    }

    public async Task<(decimal? Average, int Count)> GetRatingStatsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        // Dùng 1 truy vấn aggregate thay vì load hết đánh giá về RAM.
        var row = await DbSet
            .AsNoTracking()
            .Where(r => r.RevieweeId == userId
                     && !r.IsVoided
                     && r.Transaction.Amount > 0)
            .GroupBy(r => r.RevieweeId)
            .Select(g => new
            {
                Average = g.Average(r => (decimal)r.Rating),
                Count = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? (null, 0)
            : (Math.Round(row.Average, 2, MidpointRounding.AwayFromZero), row.Count);
    }

    public Task<int> CountReceivedAsync(Guid userId, CancellationToken ct = default)
        => DbSet.AsNoTracking().CountAsync(r => r.RevieweeId == userId, ct);

    public Task<int> CountVoidedAsync(CancellationToken ct = default)
        => DbSet.AsNoTracking().CountAsync(r => r.IsVoided, ct);

    public async Task<int> VoidReviewsForTransactionAsync(
        Guid transactionId,
        Guid reviewerId,
        Guid reportId,
        Guid staffUserId,
        string reason,
        CancellationToken ct = default)
    {
        // Lọc theo ReviewerId (NGƯỜI VIẾT đánh giá), không phải RevieweeId.
        // Mục đích: kẻ lừa đảo hay để lại đánh giá 5 sao cho bên bị hơn rồi
        // bùng hàng / tráo hàng — cần vô hiệu đánh giá do CHÍNH họ viết.
        var reviews = await DbSet
            .Where(r => r.TransactionId == transactionId
                     && r.ReviewerId == reviewerId
                     && !r.IsVoided)
            .ToListAsync(ct);

        if (reviews.Count == 0)
            return 0;

        var now = DateTime.UtcNow;

        foreach (var review in reviews)
        {
            review.IsVoided = true;
            review.VoidedByReportId = reportId;
            review.VoidedByUserId = staffUserId;
            review.VoidedAt = now;
            review.VoidReason = reason;
        }

        DbSet.UpdateRange(reviews);
        await SaveChangesAsync(ct);

        return reviews.Count;
    }
}

/// <summary>
/// Repository cho Report — báo cáo vi phạm.
/// </summary>
public class ReportRepository : EfRepository<Report>, IReportRepository
{
    public ReportRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<Report?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.Item)
            .Include(r => r.Transaction)
            .Include(r => r.Conversation)
            .Include(r => r.ResolvedBy)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(List<Report> Reports, int TotalCount)> ListForModerationAsync(
        int? status,
        int? reason,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.Item)
            .Include(r => r.Transaction)
            .Include(r => r.ResolvedBy)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        if (reason.HasValue)
            q = q.Where(r => r.Reason == reason.Value);

        var total = await q.CountAsync(ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        // Report mới nhất lên đầu — report cũ bị bỏ sót thì càng để lâu càng
        // dễ quên, nên ưu tiên report chờ xử lý.
        var reports = await q
            .OrderBy(r => r.Status == (int)ReportStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (reports, total);
    }

    public async Task<(List<Report> Reports, int TotalCount)> ListByReporterAsync(
        Guid reporterId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking()
            .Include(r => r.ReportedUser)
            .Include(r => r.Item)
            .Where(r => r.ReporterId == reporterId);

        var total = await q.CountAsync(ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var reports = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (reports, total);
    }

    public Task<int> CountConfirmedAgainstUserAsync(Guid userId, CancellationToken ct = default)
        // Status = Resolved nghĩa là nhân viên ĐÃ XÁC NHẬN có vi phạm và đã
        // thực hiện hành động. Report bị bác (Rejected) không tính.
        => DbSet.AsNoTracking().CountAsync(
            r => r.ReportedUserId == userId && r.Status == (int)ReportStatus.Resolved,
            ct);

    public async Task<Dictionary<int, int>> CountByStatusAsync(CancellationToken ct = default)
    {
        var rows = await DbSet.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Status, x => x.Count);
    }
}
