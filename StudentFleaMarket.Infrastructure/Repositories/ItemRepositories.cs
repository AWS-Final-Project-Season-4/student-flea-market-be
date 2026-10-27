using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Repositories;

public class ItemRepository : EfRepository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<(List<Item> Items, int TotalCount)> SearchAsync(
        ItemSearchQueryDto query,
        bool includeAllStatuses = false,
        CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking();

        // ----- Lọc theo trạng thái (mặc định chỉ hiện tin đang bán) -----
        if (includeAllStatuses)
        {
            // Chủ tin cần thấy cả tin đã gỡ / đã bán của mình.
            // Phải bỏ global query filter (soft delete) — nếu không tin đã gỡ
            // sẽ bị ẩn khỏi kết quả.
            q = q.IgnoreQueryFilters();
        }
        else if (query.Statuses is { Count: > 0 })
            q = q.Where(i => query.Statuses.Contains(i.Status));
        else
            q = q.Where(i => i.Status == (int)Domain.Enums.ItemStatus.Available);

        // ----- Tìm kiếm theo từ khóa tiêu đề -----
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            // TitleNormalized đã bỏ dấu + chuyển chữ thường khi ghi vào DB,
            // nên "sach" khớp "Sách", "LAP TRINH" khớp "Lập trình".
            // Contains() -> dịch thành LIKE '%...%' trên Npgsql và chạy được
            // cả với InMemory provider (không dùng EF.Functions.ILike).
            var keyword = SlugHelper.NormalizeForSearch(query.Keyword);
            if (keyword.Length > 0)
                q = q.Where(i => i.TitleNormalized.Contains(keyword));
        }

        // ----- Lọc theo danh mục -----
        if (query.CategoryIds is { Count: > 0 })
            q = q.Where(i => query.CategoryIds.Contains(i.CategoryId));

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            var slug = query.CategorySlug.Trim();
            q = q.Where(i => i.Category.Slug == slug);
        }

        // ----- Lọc theo khoảng giá -----
        if (query.MinPrice.HasValue)
            q = q.Where(i => i.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(i => i.Price <= query.MaxPrice.Value);

        // ----- Lọc theo độ mới -----
        if (query.Conditions is { Count: > 0 })
            q = q.Where(i => query.Conditions.Contains(i.Condition));

        // ----- Lọc tin cho phép đổi đồ -----
        if (query.ExchangeOnly == true)
            q = q.Where(i => i.IsExchangeEnabled);

        // ----- Lọc theo địa điểm (chứa) -----
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim().ToLower();
            q = q.Where(i => i.Location.ToLower().Contains(location));
        }

        // ----- Lọc theo người bán -----
        if (query.SellerId.HasValue)
            q = q.Where(i => i.SellerId == query.SellerId.Value);

        var total = await q.CountAsync(ct);

        // ----- Sắp xếp -----
        q = ApplySort(q, query.SortBy);

        // ----- Phân trang -----
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.PageSize
        };

        var items = await q
            .Include(i => i.Category)
            .Include(i => i.Seller)
            .Include(i => i.Images)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    private static IQueryable<Item> ApplySort(IQueryable<Item> q, string? sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            ItemSortOptions.Oldest => q.OrderBy(i => i.CreatedAt),
            ItemSortOptions.PriceAsc => q.OrderBy(i => i.Price).ThenBy(i => i.CreatedAt),
            ItemSortOptions.PriceDesc => q.OrderBy(i => i.Price).ThenBy(i => i.CreatedAt),
            ItemSortOptions.MostFavorited => q.OrderByDescending(i => i.Favorites.Count).ThenBy(i => i.CreatedAt),
            _ => q.OrderByDescending(i => i.CreatedAt)
        };
    }

    public Task<Item?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Seller)
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Item?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Seller)
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Slug == slug, ct);

    public Task<int> CountByCategoryAsync(int categoryId, CancellationToken ct = default)
        => DbSet.CountAsync(i => i.CategoryId == categoryId, ct);

    /// <summary>
    /// Lấy ảnh theo itemId. KHÔNG dùng AsNoTracking vì cần theo dõi thay đổi
    /// (thêm/xóa ảnh) trước khi SaveChanges.
    /// </summary>
    public async Task<List<ItemImage>> GetImagesAsync(Guid itemId, CancellationToken ct = default)
    {
        return await DbContext.ItemImages
            .Where(img => img.ItemId == itemId)
            .OrderBy(img => img.Order)
            .ToListAsync(ct);
    }

    public async Task AddImageAsync(ItemImage image, CancellationToken ct = default)
    {
        await DbContext.ItemImages.AddAsync(image, ct);
    }

    public Task RemoveImageAsync(ItemImage image, CancellationToken ct = default)
    {
        DbContext.ItemImages.Remove(image);
        return Task.CompletedTask;
    }
}

public class CategoryRepository : EfRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<(Category Category, int ItemCount)>> ListWithCountsAsync(CancellationToken ct = default)
    {
        var rows = await DbSet
            .AsNoTracking()
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                Category = c,
                ItemCount = c.Items.Count(i => !i.IsDeleted && i.Status == (int)Domain.Enums.ItemStatus.Available)
            })
            .ToListAsync(ct);

        return rows.Select(x => (x.Category, x.ItemCount)).ToList();
    }
}

public class FavoriteRepository : EfRepository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<(List<Favorite> Favorites, int TotalCount)> ListByUserAsync(
        Guid userId,
        FavoriteQueryDto query,
        CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking().Where(f => f.UserId == userId);

        var total = await q.CountAsync(ct);

        q = query.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(f => f.Item.Price),
            "price_desc" => q.OrderByDescending(f => f.Item.Price),
            _ => q.OrderByDescending(f => f.CreatedAt)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.PageSize
        };

        var favorites = await q
            .Include(f => f.Item)
                .ThenInclude(i => i.Category)
            .Include(f => f.Item)
                .ThenInclude(i => i.Seller)
            .Include(f => f.Item)
                .ThenInclude(i => i.Images)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (favorites, total);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid itemId, CancellationToken ct = default)
        => DbSet.AnyAsync(f => f.UserId == userId && f.ItemId == itemId, ct);

    public Task<int> CountByItemAsync(Guid itemId, CancellationToken ct = default)
        => DbSet.CountAsync(f => f.ItemId == itemId, ct);

    public Task<List<Guid>> GetFavoriteItemIdsAsync(Guid userId, CancellationToken ct = default)
        => DbSet
            .Where(f => f.UserId == userId)
            .Select(f => f.ItemId)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, int>> GetFavoriteCountsAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken ct = default)
    {
        var ids = itemIds.Distinct().ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        // 1 query duy nhất, group by để đếm số lượt yêu thích từng tin.
        var rows = await DbSet
            .Where(f => ids.Contains(f.ItemId))
            .GroupBy(f => f.ItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.ItemId, r => r.Count);
    }
}
