using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentFleaMarket.API.Filters;
using StudentFleaMarket.API.Middleware;
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
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------------- MVC + Swagger
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<ValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Campus Thrift & Exchange Hub API",
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
        Description = "Nhập token: {token}"
    });

    options.AddSecurityRequirement(doc =>
    {
        var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        requirement.Add(
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc, "Bearer"),
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
app.UseAuthorization();
app.UseMiddleware<ServiceAccountApiKeyMiddleware>();

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
}

app.Run();
