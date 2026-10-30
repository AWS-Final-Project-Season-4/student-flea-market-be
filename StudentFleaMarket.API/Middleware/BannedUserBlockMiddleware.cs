using System.Security.Claims;
using System.Text.Json;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.API.Middleware;

/// <summary>
/// Chặn ngay lập tức các tài khoản đã bị Admin cấm.
///
/// VÌ SAO CẦN MIDDLEWARE NÀY?
///   JWT là "stateless" — server không tra DB mỗi request, nên access token đã
///   cấp vẫn dùng được tới khi hết hạn (30 phút). Nếu Admin cấm user, user đó
///   vẫn gọi được API trong khoảng thời gian đó.
///
///   Middleware này tra trạng thái tài khoản mỗi request (chỉ select 2 cột,
///   có index theo PK nên rất nhẹ) và trả 403 ACCOUNT_BANNED nếu tài khoản
///   đã bị cấm.
///
/// Ghi chú hiệu năng:
///   - Chỉ chạy khi request đã xác thực bằng JWT (bỏ qua request công khai
///     và request xác thực bằng API key của dịch vụ AI).
///   - Tài khoản dịch vụ AI nằm ở bảng riêng `ServiceAccounts`, không có trong
///     bảng `Users` -> bỏ qua qua check Role.
/// </summary>
public class BannedUserBlockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BannedUserBlockMiddleware> _logger;

    public BannedUserBlockMiddleware(RequestDelegate next, ILogger<BannedUserBlockMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepo)
    {
        var userId = GetUserId(context);

        if (userId.HasValue)
        {
            var status = await userRepo.GetStatusAsync(userId.Value, context.RequestAborted);

            // Không tìm thấy tài khoản -> để framework xử lý (401).
            if (status.HasValue && status.Value.Status == (int)UserStatus.Banned)
            {
                _logger.LogWarning(
                    "Chặn tài khoản bị cấm: {UserId}. Lý do: {Reason}",
                    userId.Value, status.Value.BanReason ?? "(không rõ)");

                await WriteForbiddenAsync(context, status.Value.BanReason);
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Lấy userId từ claim. Chỉ xử lý tài khoản người dùng (Role = User) —
    /// tài khoản dịch vụ AI xác thực bằng API key không nằm trong bảng Users.
    /// </summary>
    private static Guid? GetUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return null;

        // Bỏ qua tài khoản dịch vụ AI — xác thực bằng API key, không có trong bảng Users.
        if (context.User.IsInRole("AiService"))
            return null;

        var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string? banReason)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var message = string.IsNullOrWhiteSpace(banReason)
            ? "Tài khoản của bạn đã bị quản trị viên khoá. Vui lòng liên hệ quản trị viên."
            : $"Tài khoản của bạn đã bị quản trị viên khoá. Lý do: {banReason}";

        var body = ApiResponse.Fail(message, "ACCOUNT_BANNED");

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
