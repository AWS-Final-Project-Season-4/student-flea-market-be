using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Nạp dữ liệu khởi tạo (seed) vào database: tài khoản Admin + Moderator mặc định
/// và danh mục sản phẩm.
/// Chạy tự động khi khởi động API (idempotent — không ghi đè dữ liệu đã có).
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Tài khoản Admin mặc định.
    /// Mật khẩu nên được đổi sau lần đăng nhập đầu tiên.
    /// </summary>
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminEmail = "admin@gmail.com";
    private const string DefaultAdminPassword = "admin123";
    private const string DefaultAdminFullName = "Administrator";

    /// <summary>
    /// Tài khoản Moderator (kiểm duyệt viên) mặc định.
    /// Cùng mật khẩu với Admin — nên được đổi sau lần đăng nhập đầu tiên.
    /// </summary>
    private const string DefaultModeratorUsername = "moderator";
    private const string DefaultModeratorEmail = "moderator@gmail.com";
    private const string DefaultModeratorPassword = "moderator123";
    private const string DefaultModeratorFullName = "Moderator";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedAdminAsync(db);
        await SeedModeratorAsync(db);
        await SeedCategoriesAsync(db);
    }

    private static async Task SeedAdminAsync(AppDbContext db)
    {
        var adminExists = await db.Users.AnyAsync(u => u.Username == DefaultAdminUsername);

        if (adminExists)
            return;

        // Tái sử dụng PasswordHasher của Infrastructure để hash giống hệt format lưu trong DB.
        var hasher = new Services.PasswordHasher();

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = DefaultAdminFullName,
            Email = DefaultAdminEmail,
            Username = DefaultAdminUsername,
            PasswordHash = hasher.Hash(DefaultAdminPassword),
            Role = (int)UserRole.Admin,
            Status = (int)UserStatus.Active,
            // Admin seed không cần OTP — đánh dấu đã xác thực để đăng nhập được ngay.
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seed tài khoản kiểm duyệt viên — chia sẻ công việc quản trị với Admin.
    /// Moderator có sẵn quyền đăng tin bán (không cần xin duyệt) nên
    /// SellerStatus được set luôn là Approved.
    /// </summary>
    private static async Task SeedModeratorAsync(AppDbContext db)
    {
        var moderatorExists = await db.Users.AnyAsync(u => u.Username == DefaultModeratorUsername);

        if (moderatorExists)
            return;

        var hasher = new Services.PasswordHasher();

        var moderator = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = DefaultModeratorFullName,
            Email = DefaultModeratorEmail,
            Username = DefaultModeratorUsername,
            PasswordHash = hasher.Hash(DefaultModeratorPassword),
            Role = (int)UserRole.Moderator,
            Status = (int)UserStatus.Active,
            IsEmailVerified = true,
            // Moderator được phép đăng tin nhờ vai trò (SellerPermission.CanSell)
            // -> không cần SellerStatus = Approved.
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(moderator);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        await MigrateCategoryAsync(db, "giao-duc-tai-lieu", "dung-cu-tai-lieu-hoc-tap", "Dụng cụ & Tài liệu học tập", 2);
        await MigrateCategoryAsync(db, "xe-co-phu-kien", "phu-kien-xe", "Phụ kiện xe", 7);

        var categories = new (string Name, string Slug, int Order)[]
        {
            ("Sách", "sach", 1),
            ("Dụng cụ & Tài liệu học tập", "dung-cu-tai-lieu-hoc-tap", 2),
            ("Thời trang", "thoi-trang", 3),
            ("Đồ gia dụng", "do-gia-dung", 4),
            ("Điện tử", "dien-tu", 5),
            ("Thể thao & Sức khỏe", "the-thao-suc-khoe", 6),
            ("Phụ kiện xe", "phu-kien-xe", 7),
            ("Nhạc cụ", "nhac-cu", 8),
            ("Khác", "khac", 9)
        };

        var existingSlugs = await db.Category.Select(c => c.Slug).ToListAsync();

        var missing = categories
            .Where(c => !existingSlugs.Contains(c.Slug))
            .Select(c => new Category
            {
                Id = 0,
                Name = c.Name,
                Slug = c.Slug,
                Order = c.Order
            })
            .ToList();

        if (missing.Count == 0)
            return;

        db.Category.AddRange(missing);
        await db.SaveChangesAsync();
    }

    private static async Task MigrateCategoryAsync(
        AppDbContext db,
        string legacySlug,
        string targetSlug,
        string targetName,
        int targetOrder)
    {
        var legacyCat = await db.Category.FirstOrDefaultAsync(c => c.Slug == legacySlug);
        var targetCat = await db.Category.FirstOrDefaultAsync(c => c.Slug == targetSlug);

        if (legacyCat is not null)
        {
            if (targetCat is null)
            {
                legacyCat.Name = targetName;
                legacyCat.Slug = targetSlug;
                legacyCat.Order = targetOrder;
            }
            else
            {
                var legacyItems = await db.Items.IgnoreQueryFilters()
                    .Where(i => i.CategoryId == legacyCat.Id)
                    .ToListAsync();
                foreach (var item in legacyItems)
                {
                    item.CategoryId = targetCat.Id;
                }
                db.Category.Remove(legacyCat);
            }

            await db.SaveChangesAsync();
            return;
        }

        if (targetCat is not null && targetCat.Name != targetName)
        {
            targetCat.Name = targetName;
            await db.SaveChangesAsync();
        }
    }
}
