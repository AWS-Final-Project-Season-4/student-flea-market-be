using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Application.Services;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// Quản lý tài khoản dịch vụ AI (kiểm duyệt, gợi ý sản phẩm...).
/// Chỉ Admin được gọi. Tài khoản dịch vụ xác thực bằng API key (header X-Api-Key).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ServiceAccountsController : ControllerBase
{
    private readonly IServiceAccountService _service;

    public ServiceAccountsController(IServiceAccountService service)
    {
        _service = service;
    }

    /// <summary>
    /// Tạo tài khoản dịch vụ AI mới và sinh API key.
    /// API key chỉ hiển thị đúng 1 lần trong response này.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceAccountRequestDto request, CancellationToken ct)
    {
        var (account, apiKey) = await _service.CreateAsync(
            request.Name,
            CurrentUserId(),
            request.ExpiresAt,
            ct);

        return Ok(ServiceAccountCreatedDto.From(account, apiKey));
    }

    /// <summary>
    /// Thu hồi API key của một tài khoản dịch vụ.
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await _service.RevokeAsync(id, "REVOKED_BY_ADMIN", ct);
        return Ok(new { message = "API key revoked successfully" });
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
