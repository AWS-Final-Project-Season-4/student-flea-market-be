namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Danh mục sản phẩm trên sàn.
///
/// <see cref="IsActive"/> cho phép Admin / Moderator ĐÓNG hoặc MỞ danh mục
/// (yêu cầu của Feature 5 - Admin Dashboard).
///
/// Khi danh mục bị đóng:
///   - Không hiển thị trong danh sách danh mục công khai.
///   - Người dùng KHÔNG đăng được tin mới vào danh mục đó.
///   - Tin đăng cũ đã có trong danh mục vẫn hiển thị bình thường
///     (đóng danh mục không đồng nghĩa xoá tin đăng của người bán).
/// </summary>
public class Category 
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int Order { get; set; } = 0;

    /// <summary>
    /// Danh mục còn mở để đăng tin / hiển thị hay không.
    /// Mặc định true (mở).
    /// </summary>
    public bool IsActive { get; set; } = true;

    //Navigation Properties (Single-level, NO ParentId)
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
