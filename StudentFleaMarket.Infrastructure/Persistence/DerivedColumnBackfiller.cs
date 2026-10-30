using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Helpers;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Backfill các cột dẫn xuất (derived column) cho dữ liệu đã tồn tại.
/// Hiện tại xử lý <c>Items.TitleNormalized</c> — bản bỏ dấu của <c>Title</c>
/// dùng cho tìm kiếm từ khóa không phân biệt dấu tiếng Việt.
///
/// Idempotent — chỉ xử lý những dòng có giá trị rỗng.
/// Chạy dùng C# (không dùng SQL translate) để hoạt động giống nhau trên
/// cả PostgreSQL lẫn InMemory provider.
/// </summary>
public static class DerivedColumnBackfiller
{
    public static async Task BackfillAsync(AppDbContext db, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: tin đã gỡ (soft delete) cũng cần backfill.
        var items = await db.Items
            .IgnoreQueryFilters()
            .Where(i => i.TitleNormalized == "" || i.TitleNormalized == null)
            .ToListAsync(ct);

        if (items.Count == 0)
            return;

        foreach (var item in items)
        {
            item.TitleNormalized = SlugHelper.NormalizeForSearch(item.Title);
        }

        await db.SaveChangesAsync(ct);
    }
}
