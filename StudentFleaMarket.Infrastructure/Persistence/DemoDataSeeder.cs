using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Seed DỮ LIỆU DEMO để test nhanh các chức năng:
///   1. Tìm kiếm / lọc nâng cao  (GET /api/Items/search)
///   2. Đăng bán tin              (POST /api/Items/sell)
///   3. Gỡ bán tin               (DELETE /api/Items/sell/{id})
///
/// Idempotent — chạy lại nhiều lần vẫn không nhân bản dữ liệu.
/// Bật/tắt bằng config "SeedDemoData" (mặc định chỉ bật ở Development).
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>
    /// Mật khẩu chung cho các tài khoản demo: "123456"
    /// </summary>
    private const string DemoPassword = "123456";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var (users, items) = await SeedUsersAsync(db, ct);
        await SeedItemsAsync(db, users, items, ct);
        await SeedFavoritesAsync(db, users, items, ct);
    }

    // ------------------------------------------------------------- USERS
    private static async Task<(Dictionary<string, ApplicationUser> Users, List<Item> Items)> SeedUsersAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var hasher = new Services.PasswordHasher();

        var specs = new (string Key, string FullName, string Email, string Username, string School, string Code, string Area)[]
        {
            ("minh",  "Nguyễn Minh",  "minh@sv.edu.vn",  "minh",  "Đại học Bách Khoa",  "BK22DS001", "Khu A - Bắc"),
            ("linh",  "Trần Linh",    "linh@sv.edu.vn",  "linh",  "Đại học Bách Khoa",  "BK22DS002", "Khu B - Nam"),
            ("tu",    "Lê Tu",        "tu@sv.edu.vn",    "tu",    "Đại học Bách Khoa",  "BK22DS003", "Khu C - Đông")
        };

        var users = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in specs)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == s.Username, ct);

            if (existing is null)
            {
                existing = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    FullName = s.FullName,
                    Email = s.Email,
                    Username = s.Username,
                    PasswordHash = hasher.Hash(DemoPassword),
                    PhoneNumber = "09" + Random.Shared.Next(10000000, 99999999),
                    SchoolName = s.School,
                    StudentCode = s.Code,
                    CommonArea = s.Area,
                    Dormitory = "KTX " + s.Area.Split(" - ")[0] + " - Tầng 3",
                    AvatarUrl = null,
                    Role = (int)UserRole.User,
                    Status = (int)UserStatus.Active,
                    // Demo user đã xác thực email sẵn để login được ngay, không cần OTP.
                    IsEmailVerified = true,
                    // Demo user đã có sẵn tin đăng -> cấp quyền bán luôn,
                    // nếu không thì luồng "đăng bán" của demo sẽ bị chặn 403.
                    SellerStatus = (int)SellerStatus.Approved,
                    SellerApprovedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow
                };

                db.Users.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            else if (existing.SellerStatus == (int)SellerStatus.None)
            {
                // Demo user đã tồn tại từ trước khi có tính năng duyệt seller:
                // cấp quyền bán luôn để luồng demo không bị chặn 403.
                existing.SellerStatus = (int)SellerStatus.Approved;
                existing.SellerApprovedAt = DateTime.UtcNow.AddDays(-30);
                existing.UpdatedAt = DateTime.UtcNow;
                db.Users.Update(existing);
                await db.SaveChangesAsync(ct);
            }

            users[s.Key] = existing;
        }

        return (users, new List<Item>());
    }

    // ------------------------------------------------------------- ITEMS
    private static async Task SeedItemsAsync(
        AppDbContext db,
        Dictionary<string, ApplicationUser> users,
        List<Item> _,
        CancellationToken ct)
    {
        // Chỉ seed 1 lần: nếu đã có tin đăng nào thì bỏ qua toàn bộ.
        if (await db.Items.IgnoreQueryFilters().AnyAsync(ct))
            return;

        var categories = await db.Category.ToDictionaryAsync(c => c.Slug, ct);
        if (categories.Count == 0)
            return;

        // (SellerKey, Title, CategorySlug, Price, Condition, Exchange, Location, Status, DaysAgo, Images)
        var specs = new (string Seller, string Title, string Cat, decimal Price, int Cond, bool Exchange, string Loc, int Status, int Days, string[] Images)[]
        {
            // ---------- Sách / Giáo dục ----------
            ("minh", "Sach Lap trinh C# co ban",            "sach",                 50000,  2, false, "KTX Khu A",           1, 1,  ["https://picsum.photos/seed/book1/600/600"]),
            ("linh", "Giáo trình Lập trình Web cơ bản",      "sach",                 80000,  3, true,  "KTX Khu B",           1, 2,  ["https://picsum.photos/seed/book2/600/600", "https://picsum.photos/seed/book2b/600/600"]),
            ("tu",   "Sách Lập trình Python cho người mới", "sach",                 65000,  2, false, "Thư viện trường",    1, 3,  ["https://picsum.photos/seed/book3/600/600"]),
            ("minh", "Bộ đề thi Toán cao cấp",              "giao-duc-tai-lieu",   45000,  3, false, "KTX Khu A",           1, 5,  []),
            ("linh", "Sổ tay ghi chú bìa da",               "giao-duc-tai-lieu",   25000,  1, true,  "KTX Khu B",           1, 7,  ["https://picsum.photos/seed/note/600/600"]),
            ("tu",   "Từ điển Anh-Việt sinh viên",         "giao-duc-tai-lieu",   30000,  4, false, "Canteen B",           1, 9,  []),

            // ---------- Điện tử ----------
            ("tu",   "Bàn phím cơ Keychron K2",             "dien-tu",            650000,  2, false, "KTX Khu C",           1, 2,  ["https://picsum.photos/seed/kb/600/600", "https://picsum.photos/seed/kb2/600/600"]),
            ("minh", "Chuột không dây Logitech M330",        "dien-tu",            180000,  3, true,  "KTX Khu A",           1, 4,  ["https://picsum.photos/seed/mouse/600/600"]),
            ("linh", "Tai nghe Bluetooth AirPods Pro",       "dien-tu",           1200000,  2, false, "KTX Khu B",           1, 6,  ["https://picsum.photos/seed/buds/600/600"]),
            ("tu",   "Sạc dự phòng Xiaomi 20000mAh",         "dien-tu",            220000,  3, false, "KTX Khu C",           1, 8,  []),
            ("minh", "Ổ cứng SSD 512GB Samsung",            "dien-tu",            750000,  1, false, "KTX Khu A",           1, 11, ["https://picsum.photos/seed/ssd/600/600"]),

            // ---------- Thời trang ----------
            ("linh", "Áo khoác denim unisex",                "thoi-trang",         250000,  2, true,  "KTX Khu B",           1, 3,  ["https://picsum.photos/seed/jacket/600/600"]),
            ("tu",   "Giày thể thao Nike Air Force 1",       "thoi-trang",         900000,  3, false, "Sân vận động",       1, 6,  ["https://picsum.photos/seed/shoes/600/600"]),
            ("minh", "Áo hoodie size L",                    "thoi-trang",         180000,  4, false, "KTX Khu A",           1, 10, []),
            ("linh", "Váy trắng basic size M",              "thoi-trang",         150000,  2, true,  "KTX Khu B",           1, 13, []),

            // ---------- Đồ gia dụng ----------
            ("tu",   "Nồi cơm điện đa năng 1.8L",            "do-gia-dung",        420000,  2, false, "KTX Khu C",           1, 5,  ["https://picsum.photos/seed/rice/600/600"]),
            ("minh", "Bàn học gỗ gấp gọn",                  "do-gia-dung",        200000,  3, true,  "KTX Khu A",           1, 12, []),
            ("linh", "Quạt điện Senko không dây",            "do-gia-dung",        350000,  2, false, "KTX Khu B",           1, 15, []),

            // ---------- Xe cộ & phụ kiện ----------
            ("tu",   "Xe đạp thể thao Giant Escape",        "xe-co-phu-kien",    2800000,  3, false, "Nhà xe Khu C",        1, 8,  ["https://picsum.photos/seed/bike/600/600"]),
            ("minh", "Mũ bảo hiểm scooter",                 "xe-co-phu-kien",     180000,  4, false, "KTX Khu A",           1, 14, []),

            // ---------- Nhạc cụ ----------
            ("linh", "Đàn guitar Yamaha F310",              "nhac-cu",            950000,  3, true,  "Nhà văn hóa",         1, 7,  ["https://picsum.photos/seed/guitar/600/600"]),
            ("tu",   "Loa Bluetooth JBL Flip 5",             "nhac-cu",            900000,  2, false, "KTX Khu C",           1, 17, []),

            // ---------- Thể thao ----------
            ("minh", "Dumbbell 5kg có gạt chống",           "the-thao-suc-khoe",  200000,  3, false, "Nhà thi chấn",        1, 11, []),

            // ---------- Tặng miễn phí (giá 0) ----------
            ("tu",   "Cho xin sách cũ miễn phí",             "sach",                     0,  4, false, "KTX Khu C",           1, 2,  []),
            ("linh", "Cho xin túi đựng sách",               "giao-duc-tai-lieu",       0,  3, true,  "KTX Khu B",           1, 9,  []),

            // ---------- Trạng thái khác (dùng để test lọc statuses) ----------
            ("minh", "Laptop cũ đã bán - giá tham khảo",     "dien-tu",            5000000,  5, false, "KTX Khu A",           4, 30, []),
            ("linh", "Bàn học đã đặt trước",                 "do-gia-dung",        200000,  3, false, "KTX Khu B",           3, 20, []),

            // ---------- Tin đã gỡ (soft delete) - test chức năng gỡ bán ----------
            ("tu",   "Tin đã gỡ - Quạt cũ hỏng",             "do-gia-dung",         50000,  5, false, "KTX Khu C",           2, 25, []),
            ("minh", "Tin đã gỡ - Giày size không vừa",     "thoi-trang",         400000,  4, false, "KTX Khu A",           2, 18, [])
        };

        var now = DateTime.UtcNow;
        var items = new List<Item>();

        foreach (var s in specs)
        {
            if (!users.TryGetValue(s.Seller, out var seller)) continue;
            if (!categories.TryGetValue(s.Cat, out var category)) continue;

            var item = new Item
            {
                Id = Guid.NewGuid(),
                SellerId = seller.Id,
                CategoryId = category.Id,
                Title = s.Title,
                Slug = SlugHelper.GenerateUnique(s.Title),
                TitleNormalized = SlugHelper.NormalizeForSearch(s.Title),
                Description = BuildDescription(s.Title, s.Cond, s.Loc),
                Price = s.Price,
                Condition = s.Cond,
                IsExchangeEnabled = s.Exchange,
                Location = s.Loc,
                Status = s.Status,
                // Tin có Status = Removed thì đánh dấu soft delete đúng như
                // luồng "gỡ bán" thật -> ẩn khỏi tìm kiếm công khai.
                IsDeleted = s.Status == (int)ItemStatus.Removed,
                CreatedAt = now.AddDays(-s.Days),
                UpdatedAt = now.AddDays(-s.Days)
            };

            for (var i = 0; i < s.Images.Length; i++)
            {
                item.Images.Add(new ItemImage
                {
                    Id = Guid.NewGuid(),
                    ItemId = item.Id,
                    ImageUrl = s.Images[i],
                    IsPrimary = i == 0,
                    Order = i,
                    CreatedAt = item.CreatedAt
                });
            }

            items.Add(item);
        }

        db.Items.AddRange(items);
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------- FAVORITES
    private static async Task SeedFavoritesAsync(
        AppDbContext db,
        Dictionary<string, ApplicationUser> users,
        List<Item> _,
        CancellationToken ct)
    {
        if (await db.Favorites.AnyAsync(ct))
            return;

        // Lấy lại các tin còn active để tạo yêu thích hợp lệ.
        var items = await db.Items
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        if (items.Count == 0)
            return;

        var favorites = new List<Favorite>();
        var now = DateTime.UtcNow;

        void Add(string userKey, Item item)
        {
            if (!users.TryGetValue(userKey, out var u)) return;
            favorites.Add(new Favorite
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                ItemId = item.Id,
                CreatedAt = now.AddDays(-1)
            });
        }

        // Cho "minh" nhiều yêu thích hơn -> kiểm tra sortBy=most_favorited.
        foreach (var it in items.Where(i => i.Title.Contains("C#"))) Add("minh", it);
        foreach (var it in items.Where(i => i.Title.Contains("Sách") || i.Title.Contains("Giáo trình"))) Add("minh", it);
        foreach (var it in items.Where(i => i.Title.Contains("Sách") || i.Title.Contains("Giáo trình"))) Add("linh", it);
        foreach (var it in items.Where(i => i.Title.Contains("điện") || i.Title.Contains("Bàn phím") || i.Title.Contains("Tai nghe"))) Add("tu", it);
        foreach (var it in items.Where(i => i.Title.Contains("Sách"))) Add("tu", it);

        // Loại trùng cặp (userId, itemId) vì có unique index.
        var distinct = favorites
            .GroupBy(f => new { f.UserId, f.ItemId })
            .Select(g => g.First())
            .ToList();

        if (distinct.Count == 0)
            return;

        db.Favorites.AddRange(distinct);
        await db.SaveChangesAsync(ct);
    }

    private static string BuildDescription(string title, int condition, string location)
    {
        var condName = condition switch
        {
            1 => "Mới 99%",
            2 => "Như mới",
            3 => "Tốt",
            4 => "Khá",
            _ => "Kém"
        };

        return $"Tin đăng demo: {title}. Tình trạng: {condName}. " +
               $"Giao dịch trực tiếp tại {location}. Liên hệ người bán qua hệ thống.";
    }
}
