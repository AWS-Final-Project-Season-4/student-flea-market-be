using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Items;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý tin đăng bán đồ cũ (Item) & danh mục.
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Đăng tin bán đồ mới. Giá 0 = tặng miễn phí.
    /// </summary>
    Task<ItemDetailDto> CreateAsync(Guid sellerId, CreateItemRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật tin đăng (chỉ chủ tin được sửa).
    /// </summary>
    Task<ItemDetailDto> UpdateAsync(Guid userId, Guid itemId, UpdateItemRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Xóa tin đăng (soft delete). Chỉ chủ tin hoặc Admin được xóa.
    /// </summary>
    Task<ApiResponse<object>> DeleteAsync(Guid userId, bool isAdmin, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết tin đăng. userId = null nghĩa là khách vãng lai.
    /// </summary>
    Task<ItemDetailDto> GetByIdAsync(Guid itemId, Guid? userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết tin đăng theo slug.
    /// </summary>
    Task<ItemDetailDto> GetBySlugAsync(string slug, Guid? userId, CancellationToken ct = default);

    /// <summary>
    /// Tìm kiếm & lọc nâng cao.
    /// </summary>
    Task<PagedResult<ItemSummaryDto>> SearchAsync(ItemSearchQueryDto query, Guid? userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách tin đăng của một người bán (trang "Tin đăng của tôi").
    /// </summary>
    Task<PagedResult<ItemSummaryDto>> GetBySellerAsync(Guid sellerId, Guid? userId, ItemSearchQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Danh sách danh mục kèm số lượng tin đăng.
    /// </summary>
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

/// <summary>
/// Quản lý danh sách yêu thích (Wishlist) của người dùng.
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Thêm món đồ vào danh sách yêu thích.
    /// </summary>
    Task<FavoriteActionDto> AddAsync(Guid userId, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Xóa món đồ khỏi danh sách yêu thích.
    /// </summary>
    Task<FavoriteActionDto> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách yêu thích của người dùng.
    /// </summary>
    Task<PagedResult<ItemSummaryDto>> ListAsync(Guid userId, FavoriteQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Đảo trạng thái yêu thích — dùng cho nút "heart" một chạm.
    /// </summary>
    Task<FavoriteActionDto> ToggleAsync(Guid userId, Guid itemId, CancellationToken ct = default);
}
