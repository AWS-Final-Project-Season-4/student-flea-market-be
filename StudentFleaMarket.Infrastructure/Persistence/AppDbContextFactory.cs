using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Factory dùng riêng cho `dotnet ef` (design-time).
/// Khi chạy tool, EF gọi hàm này thay vì chạy toàn bộ Program.cs của API.
/// Chuỗi kết nối ở đây chỉ dùng để EF biết provider là Npgsql —
/// `migrations add` KHÔNG kết nối tới database thật.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=StudentFleaMarketDb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
