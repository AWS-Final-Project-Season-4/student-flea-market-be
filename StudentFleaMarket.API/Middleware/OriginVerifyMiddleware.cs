using System.Security.Cryptography;
using System.Text;

namespace StudentFleaMarket.API.Middleware;

/// <summary>
/// Bảo đảm request vào <c>/api/*</c> đã đi qua CloudFront (nơi gắn AWS WAF).
/// CloudFront thêm header <c>X-Origin-Verify: &lt;secret&gt;</c> vào mọi request gửi tới
/// origin API; ai gọi thẳng URL API Gateway sẽ không có header này và bị trả 403.
/// <para>
/// Cấu hình: <c>OriginVerify:Enforce</c> (bool) và <c>OriginVerify:Secret</c> (lấy từ SSM).
/// Enforce=false (mặc định) thì bỏ qua — dùng khi chạy local hoặc trong lúc FE chưa
/// chuyển sang URL CloudFront. <c>/health</c> không nằm dưới <c>/api</c> nên luôn gọi được.
/// </para>
/// </summary>
public sealed class OriginVerifyMiddleware
{
    public const string HeaderName = "X-Origin-Verify";

    private readonly RequestDelegate _next;
    private readonly bool _enforce;
    private readonly byte[] _secret;

    public OriginVerifyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<OriginVerifyMiddleware> logger)
    {
        _next = next;
        var secret = configuration["OriginVerify:Secret"] ?? string.Empty;
        _secret = Encoding.UTF8.GetBytes(secret);
        _enforce = configuration.GetValue<bool>("OriginVerify:Enforce");

        if (_enforce && secret.Length < 32)
        {
            // Bật enforce mà thiếu secret thì mọi request đều bị chặn — báo lỗi rõ ngay khi khởi động.
            throw new InvalidOperationException(
                "OriginVerify:Enforce=true requires OriginVerify:Secret (>= 32 chars).");
        }

        if (_enforce)
            logger.LogInformation("OriginVerify enforced: /api/* only accepts requests from CloudFront.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_enforce && context.Request.Path.StartsWithSegments("/api") && !HasValidHeader(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Direct access to the API is not allowed. Use the CloudFront URL.",
                code = "ORIGIN_NOT_ALLOWED"
            });
            return;
        }

        await _next(context);
    }

    private bool HasValidHeader(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1)
            return false;

        var provided = Encoding.UTF8.GetBytes(values[0] ?? string.Empty);
        // So sánh thời gian hằng để không lộ secret qua thời gian phản hồi.
        return CryptographicOperations.FixedTimeEquals(provided, _secret);
    }
}
