namespace StudentFleaMarket.Domain.Entities;

public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Mã hàng do hệ thống tự sinh khi tin đăng được tạo (8 ký tự, chữ cái +
    /// chữ số, không có ký tự đặc biệt). Đây là mã để NGƯỜI DÙNG giao tiếp
    /// với hệ thống (mua / đổi đồ), ngắn gọn hơn nhiều so với <see cref="Id"/>.
    ///
    /// Bất biến: không đổi khi sửa tin đăng. Khác với <see cref="Slug"/> (được
    /// sinh lại theo tiêu đề) nên mã này luôn trỏ đúng về một tin đăng.
    ///
    /// <see cref="Id"/> vẫn là khoá chính dùng nội bộ (khoá ngoại ở 6 bảng khác).
    /// </summary>
    public string ItemCode { get; set; } = null!;

    /// <summary>
    /// Bản chuẩn hóa của Title: bỏ dấu tiếng Việt, chữ thường, gộp khoảng trắng.
    /// Dùng cho tìm kiếm từ khóa (đã có index) để "sach" khớp "Sách".
    /// </summary>
    public string TitleNormalized { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Condition { get; set; }
    public bool IsExchangeEnabled { get; set; } = false;
    public string Location { get; set; } = null!;
    public int Status { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ApplicationUser Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}


