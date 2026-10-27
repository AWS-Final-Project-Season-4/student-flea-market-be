using System.Security.Claims;
using System.Text.Json;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.Services;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.API.Middleware;

/// <summary>
/// Xác thực tài khoản dịch vụ AI bằng API key trong header "X-Api-Key".
/// Gắn claim vào HttpContext.User để controller đọc được.
/// </summary>
public class ServiceAccountApiKeyMiddleware
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;

    public ServiceAccountApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceAccountService serviceAccountService)
    {
        // Endpoint này chỉ áp dụng cho request có header X-Api-Key.
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var keyValues))
        {
            await _next(context);
            return;
        }

        var apiKey = keyValues.ToString();

        var account = await serviceAccountService.AuthenticateAsync(apiKey, context.RequestAborted);

        if (account is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Role, "AiService"),
            new("role", account.Role.ToString()),
            new("service_account_id", account.Id.ToString())
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));

        await _next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = ApiResponse.Fail("API key không hợp lệ, đã hết hạn hoặc đã bị thu hồi.", "INVALID_API_KEY");

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
