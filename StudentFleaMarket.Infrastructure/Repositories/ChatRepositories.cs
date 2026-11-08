using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Repositories;

public class ChatRepository : EfRepository<Conversation>, IChatRepository
{
    // DbSet riêng cho Message — DbSet của base class là Conversation.
    private readonly DbSet<Message> _messages;

    public ChatRepository(AppDbContext dbContext) : base(dbContext)
    {
        _messages = dbContext.Set<Message>();
    }

    public Task<Conversation?> GetByItemAndBuyerAsync(
        Guid itemId,
        Guid buyerId,
        CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ItemId == itemId && c.BuyerId == buyerId, ct);

    public Task<Conversation?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.Item).ThenInclude(i => i.Images)
            .Include(c => c.Buyer)
            .Include(c => c.Seller)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(List<Conversation> Conversations, int TotalCount)> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // IgnoreQueryFilters: giu ca hoi thoi gan voi mon do da gos /
        // soft delete — lich su chat phai duoc giu lai.
        var q = DbSet
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.BuyerId == userId || c.SellerId == userId);

        var total = await q.CountAsync(ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var conversations = await q
            .Include(c => c.Item).ThenInclude(i => i.Images)
            .Include(c => c.Buyer)
            .Include(c => c.Seller)
            // Tin moi nhat len dau. Hoi thoi chua co tin nao -> xuong cuoi.
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (conversations, total);
    }

    public async Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default)
    {
        // Tinh tong theo 2 truc rieng (huong nhan) — tach ra de tranh
        // phep toan dieu kien ben trong SUM khong dich duoc tren Npgsql.
        var asBuyer = await DbSet
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.BuyerId == userId)
            .SumAsync(c => c.BuyerUnreadCount, ct);

        var asSeller = await DbSet
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.SellerId == userId)
            .SumAsync(c => c.SellerUnreadCount, ct);

        return asBuyer + asSeller;
    }

    public async Task<(List<Message> Messages, int TotalCount)> ListMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = _messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        var total = await q.CountAsync(ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 50,
            > 100 => 100,
            _ => pageSize
        };

        var messages = await q
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (messages, total);
    }

    public async Task<int> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerId,
        DateTime readAt,
        CancellationToken ct = default)
    {
        // ExecuteUpdate -> 1 lenh UPDATE atomic, khong can load entity ve.
        return await _messages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerId
                        && !m.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, readAt), ct);
    }

    public async Task IncrementUnreadAsync(
        Guid conversationId,
        Guid recipientId,
        CancellationToken ct = default)
    {
        // Tang atomic theo huong nhan: nguoi nhan la Buyer -> tang
        // BuyerUnreadCount, nguoi nhan la Seller -> tang SellerUnreadCount.
        //
        // Dung 2 lenh UPDATE rieng thay vi 1 lenh co dieu kien, vi EF Core
        // dich ternary trong SetProperty khong on dinh tren Npgsql.
        // Ca 2 deu la UPDATE ... SET x = x + 1 nen van atomic.
        var affected = await DbSet
            .Where(c => c.Id == conversationId && c.BuyerId == recipientId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.BuyerUnreadCount, c => c.BuyerUnreadCount + 1), ct);

        if (affected == 0)
        {
            await DbSet
                .Where(c => c.Id == conversationId && c.SellerId == recipientId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.SellerUnreadCount, c => c.SellerUnreadCount + 1), ct);
        }
    }
}

public class MessageRepository : EfRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<List<Message>> ListWithSenderAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<Message?> GetWithSenderAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
}
