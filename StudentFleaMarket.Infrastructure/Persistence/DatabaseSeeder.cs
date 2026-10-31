using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Nạp dữ liệu khởi tạo (seed) vào database: tài khoản Admin mặc định + danh mục sản phẩm.
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

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedAdminAsync(db);
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

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        var categories = new (string Name, string Slug, int Order)[]
        {
            ("Sách", "sach", 1),
            ("Giáo dụng & Tài liệu", "giao-duc-tai-lieu", 2),
            ("Thời trang", "thoi-trang", 3),
            ("Đồ gia dụng", "do-gia-dung", 4),
            ("Điện tử", "dien-tu", 5),
            ("Thể thao & Sức khỏe", "the-thao-suc-khoe", 6),
            ("Xe cộ & Phụ kiện", "xe-co-phu-kien", 7),
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
}
