using System.Security.Claims;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.API.Middleware;

/// <summary>
/// Đồng bộ vai trò (Role) của tài khoản với dữ liệu trong DB.
///
/// VÌ SAO CẦN MIDDLEWARE NÀY?
///   JWT là stateless — role được nhúng vào token lúc đăng nhập. Nếu Admin
///   cấp quyền Admin cho một user, user đó vẫn mang role "User" trong token
///   cũ cho tới khi hết hạn (30 phút) hoặc đăng nhập lại.
///
///   Middleware này tra `Role` từ DB mỗi request và ghi đè claim role trong
///   principal — nhờ vậy việc cấp / thu hồi quyền Admin có hiệu lực TỨC THÌ,
///   không cần đăng nhập lại.
///
/// Ghi chú:
///   - Chỉ áp dụng cho tài khoản xác thực bằng JWT (Role = User hoặc Admin).
///   - Tài khoản dịch vụ AI xác thực bằng API key không nằm trong bảng Users
///     -> bỏ qua hoàn toàn.
///   - Chi phí: 1 truy vấn theo PK mỗi request, chỉ select cột "Role".
/// </summary>
public class RoleSyncMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleSyncMiddleware> _logger;

    public RoleSyncMiddleware(RequestDelegate next, ILogger<RoleSyncMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepo)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.IsInRole("AiService"))
        {
            var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(raw, out var userId))
            {
                var dbRole = await userRepo.GetRoleAsync(userId, context.RequestAborted);

                if (dbRole.HasValue)
                {
                    var roleName = RoleNameOf(dbRole.Value);

                    // Ghi đè role theo DB — nguồn sự thật duy nhất.
                    if (context.User.IsInRole(roleName))
                    {
                        // Đã khớp -> giữ nguyên principal.
                    }
                    else
                    {
                        var identity = (ClaimsIdentity)context.User.Identity!;

                        // Xóa claim role cũ (ClaimTypes.Role và "role")
                        foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                            identity.RemoveClaim(claim);

                        foreach (var claim in identity.FindAll("role").ToList())
                            identity.RemoveClaim(claim);

                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        identity.AddClaim(new Claim("role", dbRole.Value.ToString()));

                        _logger.LogInformation(
                            "Dong bo role cho user {UserId}: {Role}", userId, roleName);
                    }
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Tên role dùng cho [Authorize(Roles = "...")].
    /// </summary>
    private static string RoleNameOf(int role) => role switch
    {
        (int)UserRole.Admin => "Admin",
        (int)UserRole.AiService => "AiService",
        _ => "User"
    };
}
