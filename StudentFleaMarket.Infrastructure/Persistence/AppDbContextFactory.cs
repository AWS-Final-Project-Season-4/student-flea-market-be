using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StudentFleaMarket.Infrastructure.Persistence;

/// <summary>
/// Factory dùng riêng cho `dotnet ef` (design-time).
/// Khi chạy tool, EF gọi hàm này thay vì chạy toàn bộ Program.cs của API.
///
/// Vì sao phải tự đọc config:
///   Chỉ chuỗi kết nối trong appsettings.Development.json là KHÔNG đầy đủ
///   (không có mật khẩu — mật khẩu nằm trong user-secrets của API project).
///   Nếu hardcode chuỗi kết nối ở đây thì `dotnet ef` sẽ đăng nhập sai
///   và báo "password authentication failed".
///
/// Cách hoạt động:
///   1. Ưu tiên biến môi trường ConnectionStrings__DefaultConnection.
///   2. Nếu không có, dựng config từ appsettings*.json + user-secrets của
///      StudentFleaMarket.API (dùng chung UserSecretsId với project API).
///   3. Cuối cùng mới fallback về chuỗi local mặc định.
///
/// Lưu ý: `migrations add` KHÔNG kết nối tới database thật — chỉ cần biết
/// provider là Npgsql để sinh code đúng kiểu.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// UserSecretsId của StudentFleaMarket.API (khớp với file .csproj của API).
    /// </summary>
    private const string ApiUserSecretsId = "e5ebc5db-e6b8-45c4-b8c0-f5aef0fa27ba";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        // 1. Biến môi trường — ưu tiên cao nhất (dùng cho CI/CD).
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        // 2. Đọc config của API project: appsettings*.json + user-secrets.
        try
        {
            var config = BuildApiConfiguration();
            var fromConfig = config.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(fromConfig))
                return fromConfig;
        }
        catch
        {
            // Không tìm thấy file config -> dùng fallback bên dưới.
        }

        // 3. Fallback — chỉ dùng cho sinh migration (không cần kết nối thật).
        return "Host=localhost;Port=5432;Database=StudentFleaMarketDb;Username=postgres;Password=postgres";
    }

    /// <summary>
    /// Dựng IConfiguration từ appsettings của API project + user-secrets.
    /// </summary>
    private static IConfigurationRoot BuildApiConfiguration()
    {
        var apiProjectDir = FindApiProjectDirectory();
        if (apiProjectDir is null)
            throw new DirectoryNotFoundException("Không tìm thấy thư mục StudentFleaMarket.API.");

        var builder = new ConfigurationBuilder()
            .SetBasePath(apiProjectDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

        // User-secrets của API project được lưu tại:
        //   %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
        // Đọc trực tiếp file này để lấy chuỗi kết nối có mật khẩu.
        var secretsFile = FindUserSecretsFile(ApiUserSecretsId);
        if (secretsFile is not null)
        {
            using var stream = File.OpenRead(secretsFile);
            builder.AddJsonStream(stream);
        }

        return builder.Build();
    }

    /// <summary>
    /// Tìm file secrets.json của một UserSecretsId.
    /// </summary>
    private static string? FindUserSecretsFile(string userSecretsId)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            return null;

        var path = Path.Combine(appData, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Tìm thư mục StudentFleaMarket.API bằng cách đi ngược lên từ thư mục
    /// đang chạy (bin/Debug/net8.0) cho tới khi gặp thư mục chứa API project.
    /// </summary>
    private static string? FindApiProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "StudentFleaMarket.API");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "StudentFleaMarket.API.csproj")))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }
}
