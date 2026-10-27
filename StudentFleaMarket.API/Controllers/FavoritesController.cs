using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// Danh sách yêu thích (Wishlist) của người dùng.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User, Admin")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    /// <summary>
    /// Danh sách món đồ đã yêu thích của chính mình.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FavoriteQueryDto query, CancellationToken ct)
    {
        var result = await _favoriteService.ListAsync(GetUserId(), query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Thêm món đồ vào danh sách yêu thích.
    /// </summary>
    [HttpPost("{itemId:guid}")]
    public async Task<IActionResult> Add(Guid itemId, CancellationToken ct)
    {
        var result = await _favoriteService.AddAsync(GetUserId(), itemId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xóa món đồ khỏi danh sách yêu thích.
    /// </summary>
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Remove(Guid itemId, CancellationToken ct)
    {
        var result = await _favoriteService.RemoveAsync(GetUserId(), itemId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đảo trạng thái yêu thích — dùng cho nút "heart" một chạm.
    /// </summary>
    [HttpPost("{itemId:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid itemId, CancellationToken ct)
    {
        var result = await _favoriteService.ToggleAsync(GetUserId(), itemId, ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("Token không hợp lệ.");

        return userId;
    }
}
