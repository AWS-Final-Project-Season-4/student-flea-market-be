namespace StudentFleaMarket.Application.Common;

/// <summary>
/// Kết quả phân trang chuẩn — dùng cho các endpoint trả về danh sách.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
