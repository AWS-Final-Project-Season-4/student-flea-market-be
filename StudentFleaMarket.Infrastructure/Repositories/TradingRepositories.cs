using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trading;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Repositories;

public class ExchangeRequestRepository : EfRepository<ExchangeRequest>, IExchangeRequestRepository
{
    public ExchangeRequestRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<ExchangeRequest?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            // IgnoreQueryFilters: Item có global query filter (soft delete).
            // Bỏ qua để vẫn thấy tin đã gỡ trong yêu cầu đổi đồ (giữ lịch sử).
            .IgnoreQueryFilters()
            .Include(er => er.Requester)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Images)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Seller)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Images)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Seller)
            .FirstOrDefaultAsync(er => er.Id == id, ct);

    public Task<ExchangeRequest?> GetTrackedDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .IgnoreQueryFilters()
            .Include(er => er.Requester)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Images)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Seller)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Images)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Seller)
            .FirstOrDefaultAsync(er => er.Id == id, ct);

    public async Task<(List<ExchangeRequest> Requests, int TotalCount)> ListForUserAsync(
        Guid userId,
        ExchangeRequestQueryDto query,
        CancellationToken ct = default)
    {
        var direction = NormalizeDirection(query.Direction, query.Incoming);

        // Yêu cầu liên quan tới user: user là người gửi (Requester)
        // HOẶC user là chủ món đồ được nhận (RequestedItem).
        // IgnoreQueryFilters: giữ cả các yêu cầu gắn với tin đã gỡ (lịch sử).
        var q = DbSet.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(er => er.RequesterId == userId || er.RequestedItem.SellerId == userId);

        if (direction == "incoming")
            q = q.Where(er => er.RequestedItem.SellerId == userId && er.RequesterId != userId);
        else if (direction == "outgoing")
            q = q.Where(er => er.RequesterId == userId);

        if (query.Status.HasValue)
            q = q.Where(er => er.Status == query.Status.Value);

        var total = await q.CountAsync(ct);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.PageSize
        };

        var requests = await q
            .Include(er => er.Requester)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Images)
            .Include(er => er.RequestedItem).ThenInclude(i => i.Seller)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Images)
            .Include(er => er.OfferedItem).ThenInclude(i => i.Seller)
            .OrderByDescending(er => er.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (requests, total);
    }

    public Task<bool> ExistsPendingAsync(
        Guid requestedItemId,
        Guid offeredItemId,
        CancellationToken ct = default)
        => DbSet.AnyAsync(er =>
            er.RequestedItemId == requestedItemId
            && er.OfferedItemId == offeredItemId
            && er.Status == (int)ExchangeRequestStatus.Pending, ct);

    public Task<bool> HasPendingRequestForItemAsync(Guid itemId, CancellationToken ct = default)
        => DbSet.AnyAsync(er =>
            (er.RequestedItemId == itemId || er.OfferedItemId == itemId)
            && er.Status == (int)ExchangeRequestStatus.Pending, ct);

    /// <summary>
    /// Chuẩn hóa tham số lọc hướng: "incoming" / "outgoing" / null.
    /// </summary>
    private static string? NormalizeDirection(string? direction, string? incoming)
    {
        var raw = direction ?? incoming;

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Trim().ToLowerInvariant() switch
        {
            "incoming" => "incoming",
            "outgoing" => "outgoing",
            _ => null
        };
    }
}

public class TransactionRepository : EfRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<Transaction?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            // IgnoreQueryFilters: Item có global query filter (soft delete).
            // Bỏ qua để vẫn thấy tin đã gỡ trong đơn giao dịch (giữ lịch sử).
            .IgnoreQueryFilters()
            .Include(t => t.Initiator)
            .Include(t => t.Counterparty)
            .Include(t => t.Item).ThenInclude(i => i.Images)
            .Include(t => t.Item).ThenInclude(i => i.Seller)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Images)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Seller)
            .Include(t => t.Payment).ThenInclude(p => p!.Payer)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transaction?> GetTrackedDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .IgnoreQueryFilters()
            .Include(t => t.Initiator)
            .Include(t => t.Counterparty)
            .Include(t => t.Item).ThenInclude(i => i.Images)
            .Include(t => t.Item).ThenInclude(i => i.Seller)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Images)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Seller)
            .Include(t => t.Payment).ThenInclude(p => p!.Payer)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(List<Transaction> Transactions, int TotalCount)> ListForUserAsync(
        Guid userId,
        TransactionQueryDto query,
        CancellationToken ct = default)
    {
        // Đơn liên quan tới user: user là người khởi tạo HOẶC là bên còn lại.
        // IgnoreQueryFilters: giữ cả các đơn gắn với tin đã gỡ (lịch sử).
        var q = DbSet.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.InitiatorId == userId || t.CounterpartyId == userId);

        var direction = query.Direction?.Trim().ToLowerInvariant();
        if (direction == "incoming")
            q = q.Where(t => t.CounterpartyId == userId && t.InitiatorId != userId);
        else if (direction == "outgoing")
            q = q.Where(t => t.InitiatorId == userId);

        if (query.Status.HasValue)
            q = q.Where(t => t.Status == query.Status.Value);

        if (query.Type.HasValue)
            q = q.Where(t => t.Type == query.Type.Value);

        var total = await q.CountAsync(ct);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.PageSize
        };

        var transactions = await q
            .Include(t => t.Initiator)
            .Include(t => t.Counterparty)
            .Include(t => t.Item).ThenInclude(i => i.Images)
            .Include(t => t.Item).ThenInclude(i => i.Seller)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Images)
            .Include(t => t.ExchangeRequest).ThenInclude(er => er!.OfferedItem).ThenInclude(i => i.Seller)
            .Include(t => t.Payment).ThenInclude(p => p!.Payer)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (transactions, total);
    }

    public Task<bool> HasPendingTransactionForItemAsync(Guid itemId, CancellationToken ct = default)
        => DbSet.AnyAsync(t =>
            t.ItemId == itemId
            && (t.Status == (int)TransactionStatus.Pending
                || t.Status == (int)TransactionStatus.Accepted), ct);

    public Task<Transaction?> GetByExchangeRequestAsync(Guid exchangeRequestId, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(t => t.ExchangeRequestId == exchangeRequestId, ct);
}

public class PaymentRepository : EfRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<Payment?> GetByTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(p => p.Payer)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, ct);
}
