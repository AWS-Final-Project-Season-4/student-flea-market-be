using System.Linq.Expressions;
using StudentFleaMarket.Application.DTOs.Items;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Repository riêng cho Item — cần truy vấn tìm kiếm & lọc nâng cao.
/// </summary>
public interface IItemRepository : IRepository<Item>
{
    /// <summary>
    /// Tìm kiếm & lọc tin đăng. Trả về danh sách đã phân trang + tổng số bản ghi.
    /// </summary>
    /// <param name="includeAllStatuses">
    /// true = bỏ qua bộ lọc trạng thái (dùng cho trang "Tin đăng của tôi" —
    /// chủ tin cần thấy cả tin đã gỡ / đã bán).
    /// </param>
    Task<(List<Item> Items, int TotalCount)> SearchAsync(
        ItemSearchQueryDto query,
        bool includeAllStatuses = false,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết tin đăng kèm các quan hệ (Seller, Category, Images) đã load.
    /// </summary>
    Task<Item?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy tin đăng theo slug (dùng cho URL thân thiện).
    /// </summary>
    Task<Item?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Lấy ảnh của tin đăng (tracked) — dùng khi cần cập nhật / xóa ảnh.
    /// </summary>
    Task<List<ItemImage>> GetImagesAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Thêm một ảnh vào tin đăng.
    /// </summary>
    Task AddImageAsync(ItemImage image, CancellationToken ct = default);

    /// <summary>
    /// Xóa một ảnh khỏi tin đăng.
    /// </summary>
    Task RemoveImageAsync(ItemImage image, CancellationToken ct = default);

    /// <summary>
    /// Đếm số tin đăng theo danh mục — dùng cho màn hình danh mục.
    /// </summary>
    Task<int> CountByCategoryAsync(int categoryId, CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho Category.
/// Lưu ý: Category dùng khóa int (khác các entity khác dùng Guid),
/// nên cần overload GetByIdAsync(int).
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Lấy danh mục theo khóa int.
    /// </summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả danh mục (kèm số lượng tin đăng) sắp xếp theo Order.
    /// </summary>
    Task<List<(Category Category, int ItemCount)>> ListWithCountsAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho Favorite (Wishlist).
/// </summary>
public interface IFavoriteRepository : IRepository<Favorite>
{
    /// <summary>
    /// Lấy danh sách yêu thích của người dùng (phân trang).
    /// </summary>
    Task<(List<Favorite> Favorites, int TotalCount)> ListByUserAsync(
        Guid userId,
        FavoriteQueryDto query,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra người dùng đã yêu thích món đồ này chưa.
    /// </summary>
    Task<bool> ExistsAsync(Guid userId, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Đếm số người đã yêu thích một món đồ.
    /// </summary>
    Task<int> CountByItemAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách Id món đồ đã yêu thích của người dùng — dùng để đánh dấu
    /// IsFavorited khi trả về danh sách tin đăng.
    /// </summary>
    Task<List<Guid>> GetFavoriteItemIdsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Đếm số lượt yêu thích cho NHIỀU món đồ cùng lúc (1 query, tránh N+1).
    /// Trả về dictionary: itemId -> số lượt yêu thích.
    /// </summary>
    Task<Dictionary<Guid, int>> GetFavoriteCountsAsync(IEnumerable<Guid> itemIds, CancellationToken ct = default);
}
