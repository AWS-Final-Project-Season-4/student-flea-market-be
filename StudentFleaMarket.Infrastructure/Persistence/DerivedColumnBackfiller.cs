using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Helpers;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Backfill các cột dẫn xuất (derived column) cho dữ liệu đã tồn tại.
/// Hiện xử lý:
///   - <c>Items.TitleNormalized</c> — bản bỏ dấu của <c>Title</c> dùng cho
///     tìm kiếm từ khóa không phân biệt dấu tiếng Việt.
///   - <c>Items.ItemCode</c> — mã hàng 8 ký tự cho người dùng (trường hợp
///     tin được tạo bởi đường khác mà không sinh mã).
///
/// Idempotent — chỉ xử lý những dòng có giá trị rỗng.
/// Chạy dùng C# (không dùng SQL translate) để hoạt động giống nhau trên
/// cả PostgreSQL lẫn InMemory provider.
/// </summary>
public static class DerivedColumnBackfiller
{
    public static async Task BackfillAsync(AppDbContext db, CancellationToken ct = default)
    {
        await BackfillTitleNormalizedAsync(db, ct);
        await BackfillItemCodeAsync(db, ct);
    }

    private static async Task BackfillTitleNormalizedAsync(AppDbContext db, CancellationToken ct)
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

    private static async Task BackfillItemCodeAsync(AppDbContext db, CancellationToken ct)
    {
        // IgnoreQueryFilters: tin đã gỡ (soft delete) cũng cần backfill.
        var items = await db.Items
            .IgnoreQueryFilters()
            .Where(i => i.ItemCode == "" || i.ItemCode == null)
            .ToListAsync(ct);

        if (items.Count == 0)
            return;

        // Lấy danh sách mã đã dùng để tránh sinh trùng.
        var used = await db.Items
            .IgnoreQueryFilters()
            .Where(i => i.ItemCode != "" && i.ItemCode != null)
            .Select(i => i.ItemCode)
            .ToListAsync(ct);

        var taken = new HashSet<string>(used, StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var code = ItemCodeHelper.Generate();
            var attempts = 0;

            while (taken.Contains(code) && attempts < 50)
            {
                code = ItemCodeHelper.Generate();
                attempts++;
            }

            if (taken.Contains(code))
                throw new InvalidOperationException(
                    "Không thể sinh mã hàng duy nhất cho các tin đăng cũ.");

            item.ItemCode = code;
            taken.Add(code);
        }

        await db.SaveChangesAsync(ct);
    }
}
