using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentFleaMarket.API.Filters;
using StudentFleaMarket.API.Middleware;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Infrastructure.DependencyInjection;
using StudentFleaMarket.Infrastructure.Persistence;
using StudentFleaMarket.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// ------------------------------------------------------------------- Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
{
    // Chỉ dùng cho môi trường Development / smoke test khi chưa có PostgreSQL.
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("StudentFleaMarketDb"));
}
else
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection chưa được cấu hình. " +
            "Hãy thiết lập qua user-secrets (dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\") " +
            "hoặc đặt \"UseInMemoryDatabase\": true trong appsettings.Development.json.");
    }

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}

// --------------------------------------------------------- Application services
builder.Services.AddApplicationServices();

// ---------------------------------------------------------------------- Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey chưa được cấu hình đúng hoặc ngắn hơn 32 ký tự. Hãy thiết lập qua appsettings/user-secrets.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Trả body chuẩn ApiResponse thay vì body rỗng mặc định của JwtBearer.
        // Áp dụng cho mọi endpoint [Authorize] (kể cả Profile).
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Ghi log lý do từ chối token để dễ chẩn đoán (401 do dán sai token).
                // Dùng mã ASCII để hiển thị đúng trên mọi console (kể cả khi redirect).
                var reason = context.Exception switch
                {
                    SecurityTokenExpiredException => "TOKEN_EXPIRED: token da het han.",
                    SecurityTokenInvalidSignatureException => "INVALID_TOKEN_SIGNATURE: sai chu ky token (thuong do dan nham token hoac dan them tien to 'Bearer').",
                    SecurityTokenInvalidAudienceException => "INVALID_TOKEN_AUDIENCE: sai audience cua token.",
                    SecurityTokenInvalidIssuerException => "INVALID_TOKEN_ISSUER: sai issuer cua token.",
                    SecurityTokenMalformedException => "TOKEN_MALFORMED: token sai dinh dang (thuong do dan nham refresh token hoac dan ca JSON response).",
                    _ => $"AUTH_FAILED: {context.Exception.GetType().Name}."
                };

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");

                logger.LogWarning("Token bi tu choi -> {Reason}. Path={Path}",
                    reason, context.HttpContext.Request.Path);

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                // Không ghi đè response nếu đã có nội dung (vd. middleware khác đã trả lỗi).
                if (context.Response.HasStarted) return;

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";

                var errorCode = context.AuthenticateFailure switch
                {
                    SecurityTokenExpiredException => "TOKEN_EXPIRED",
                    SecurityTokenInvalidSignatureException => "INVALID_TOKEN_SIGNATURE",
                    SecurityTokenInvalidAudienceException => "INVALID_TOKEN_AUDIENCE",
                    SecurityTokenInvalidIssuerException => "INVALID_TOKEN_ISSUER",
                    _ => "UNAUTHORIZED"
                };

                var message = context.AuthenticateFailure switch
                {
                    SecurityTokenExpiredException => "Token đã hết hạn. Vui lòng đăng nhập lại.",
                    SecurityTokenInvalidSignatureException => "Token không hợp lệ. Hãy chắc chắn bạn dán đúng accessToken, KHÔNG thêm chữ 'Bearer' và không dán nguyên JSON response.",
                    SecurityTokenMalformedException => "Token sai định dạng. Hãy dán đúng accessToken, KHÔNG dán refreshToken hay cả JSON response của /api/Auth/login.",
                    _ => "Yêu cầu đăng nhập để truy cập tài nguyên này."
                };

                var body = ApiResponse.Fail(message, errorCode);
                await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";

                var body = ApiResponse.Fail("Bạn không có quyền thực hiện thao tác này.", "FORBIDDEN");
                await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------------- MVC + Swagger
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<ValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Gắn tag cho từng nhóm endpoint để Swagger hiển thị gọn gàng.
    // "Items"            -> Tìm kiếm & Xem tin (public)
    // "ItemsForSale"     -> Đăng bán & Quản lý tin của mình
    options.TagActionsBy(api =>
    {
        var controllerName = (api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)
            ?.ControllerTypeInfo.Name;

        var tag = controllerName switch
        {
            "ItemsController" => "1. Tìm kiếm & Xem tin",
            "ItemsForSaleController" => "2. Đăng bán & Quản lý tin",
            "FavoritesController" => "3. Yêu thích (Wishlist)",
            "SellerController" => "4. Quyền bán hàng (Seller)",
            "AdminSellerController" => "5. Admin - Duyệt người bán",
            "AdminUsersController" => "6. Admin - Quản lý & cấm tài khoản",
            _ => controllerName ?? "Khác"
        };

        return new List<string> { tag };
    });

    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Student Flea Market API",
        Version = "v1",
        Description = "API cho sàn giao dịch đồ cũ sinh viên."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Dán accessToken lấy từ response của POST /api/Auth/login (trường accessToken). " +
                      "CHỈ dán token, KHÔNG thêm chữ 'Bearer' và KHÔNG dán nguyên cả JSON response. " +
                      "Swagger sẽ tự thêm tiền tố 'Bearer '."
    });

    // Yêu cầu bảo mật toàn cục.
    //
    // LƯU Ý QUAN TRỌNG: tham số thứ 3 của OpenApiSecuritySchemeReference là
    // "externalResource" (KHÔNG phải tên scheme). Truyền "Bearer" sẽ tạo ra
    // key "Bearer#/components/securitySchemes/Bearer" trong swagger.json —
    // Swagger UI không tìm thấy scheme tương ứng nên KHÔNG gửi header
    // Authorization, dẫn tới 401 dù token hoàn toàn hợp lệ.
    // Truyền null để key ra đúng "Bearer".
    options.AddSecurityRequirement(doc =>
    {
        var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        requirement.Add(
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>());
        return requirement;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// ---------------------------------------------------------------- Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
// Đồng bộ role từ DB -> cấp / thu hồi quyền Admin có hiệu lực tức thì
// (JWT là stateless, role nhúng trong token sẽ cũ nếu không đồng bộ).
// Phải chạy SAU UseAuthentication và TRƯỚC UseAuthorization.
app.UseMiddleware<RoleSyncMiddleware>();
// Chặn tài khoản đã bị Admin cấm — phải chạy SAU UseAuthentication (để đọc
// được JWT đã xác thực) và TRƯỚC UseAuthorization.
// JWT là stateless nên access token đã cấp vẫn dùng được tới hết hạn —
// middleware này tra trạng thái tài khoản mỗi request để cấm có hiệu lực tức thì.
app.UseMiddleware<BannedUserBlockMiddleware>();
// Xác thực tài khoản dịch vụ AI phải chạy SAU UseAuthentication (để đọc được JWT
// đã xác thực) và TRƯỚC UseAuthorization (để principal do API key tạo ra được
// framework kiểm tra khi áp dụng [Authorize]).
app.UseMiddleware<ServiceAccountApiKeyMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// -------------------------------------------------- Seed dữ liệu khởi tạo
// (tài khoản Admin mặc định + danh mục). Idempotent — chạy lại không ghi đè.
if (!builder.Configuration.GetValue<bool>("SkipDatabaseSeed"))
{
    try
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Không seed được dữ liệu khởi tạo.");
        throw;
    }

    // ------------------------------------------- Backfill cột dẫn xuất (luôn chạy)
    // Ví dụ: Items.TitleNormalized (bản bỏ dấu của Title) cho dữ liệu đã có sẵn.
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DerivedColumnBackfiller.BackfillAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Bỏ qua backfill cột dẫn xuất.");
    }

    // ---------------------------------------------------------- Seed DEMO data
    // Dữ liệu mẫu để test nhanh: tìm kiếm, đăng bán, gỡ bán.
    //
    // Thứ tự ưu tiên:
    //   1. SkipDemoDataSeed = true            -> TẮT (dù cấu hình gì khác)
    //   2. SeedDemoData được chỉ định       -> theo giá trị đó
    //   3. Không chỉ định                    -> bật nếu đang ở Development
    var skipDemo = builder.Configuration.GetValue<bool>("SkipDemoDataSeed");
    var seedDemo = !skipDemo && (
        builder.Configuration.GetValue<bool?>("SeedDemoData")
        ?? app.Environment.IsDevelopment());

    if (seedDemo)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DemoDataSeeder.SeedAsync(db);
            app.Logger.LogInformation("Đã nạp dữ liệu demo (users + tin đăng mẫu).");
        }
        catch (Exception ex)
        {
            // Không ném lỗi — demo data là tiện ích, lỗi seed demo không được
            // làm sập API (ví dụ DB chưa migrate đủ bảng).
            app.Logger.LogWarning(ex, "Bỏ qua seed dữ liệu demo.");
        }
    }
}

app.Run();
