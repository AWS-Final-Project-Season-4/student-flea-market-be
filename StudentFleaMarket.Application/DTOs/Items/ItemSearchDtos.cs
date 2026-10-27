using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Items;

/// <summary>
/// Tham số tìm kiếm & lọc nâng cao cho danh sách tin đăng.
/// </summary>
public class ItemSearchQueryDto
{
    /// <summary>
    /// Từ khóa tìm kiếm trên tiêu đề (không phân biệt hoa thường).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo danh mục. Có thể truyền nhiều danh mục.
    /// </summary>
    public List<int>? CategoryIds { get; set; }

    /// <summary>
    /// Lọc theo slug danh mục (thay thế / bổ sung cho CategoryIds).
    /// </summary>
    public string? CategorySlug { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Lọc theo độ mới (1=New, 2=LikeNew, 3=Good, 4=Fair, 5=Poor).
    /// </summary>
    public List<int>? Conditions { get; set; }

    /// <summary>
    /// Lọc theo trạng thái tin đăng (1=Available, 2=Removed, 3=Reserved, 4=Sold).
    /// Mặc định chỉ hiện Available.
    /// </summary>
    public List<int>? Statuses { get; set; }

    /// <summary>
    /// Chỉ hiện tin cho phép đổi đồ.
    /// </summary>
    public bool? ExchangeOnly { get; set; }

    /// <summary>
    /// Lọc theo địa điểm gặp mặt (chứa).
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Lọc theo người bán cụ thể.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Sắp xếp: newest | oldest | price_asc | price_desc | most_favorited.
    /// </summary>
    public string? SortBy { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Tham số lọc cho danh sách yêu thích của chính người dùng.
/// </summary>
public class FavoriteQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
}

/// <summary>
/// Kết quả thêm/xóa yêu thích.
/// </summary>
public class FavoriteActionDto
{
    public Guid ItemId { get; init; }
    public bool IsFavorited { get; init; }
    public int FavoriteCount { get; init; }
}

/// <summary>
/// Enum status cho việc sắp xếp — dùng để validate đầu vào.
/// </summary>
public static class ItemSortOptions
{
    public const string Newest = "newest";
    public const string Oldest = "oldest";
    public const string PriceAsc = "price_asc";
    public const string PriceDesc = "price_desc";
    public const string MostFavorited = "most_favorited";

    public static readonly string[] All =
    {
        Newest, Oldest, PriceAsc, PriceDesc, MostFavorited
    };

    public static bool IsValid(string? sortBy)
        => sortBy is null || All.Contains(sortBy.ToLowerInvariant());
}

/// <summary>
/// Tóm tắt các giá trị enum để frontend hiển thị nhãn tiếng Việt.
/// </summary>
public static class ItemEnumLabels
{
    public static string ConditionName(int condition) => (ItemCondition)condition switch
    {
        ItemCondition.New => "Mới 99%",
        ItemCondition.LikeNew => "Như mới",
        ItemCondition.Good => "Tốt",
        ItemCondition.Fair => "Khá",
        ItemCondition.Poor => "Kém",
        _ => "Không xác định"
    };

    public static string StatusName(int status) => (ItemStatus)status switch
    {
        ItemStatus.Available => "Đang bán",
        ItemStatus.Removed => "Đã gỡ",
        ItemStatus.Reserved => "Đã đặt",
        ItemStatus.Sold => "Đã bán",
        _ => "Không xác định"
    };
}
