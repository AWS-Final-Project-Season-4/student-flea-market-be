using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// Nhóm 1 — TÌM KIẾM & XEM TIN.
/// Các endpoint công khai (không cần token). Nếu có token, <c>isFavorited</c>
/// trong kết quả sẽ phản ánh đúng trạng thái yêu thích của người gọi.
/// </summary>
[ApiController]
[Route("api/Items")]
[Authorize(Roles = "User, Admin")]
[Produces("application/json")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    /// <summary>
    /// Tìm kiếm & lọc nâng cao danh sách tin đăng.
    /// Hỗ trợ: keyword, categoryIds, categorySlug, minPrice, maxPrice,
    /// conditions, statuses, exchangeOnly, location, sellerId, sortBy, page, pageSize.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ItemSearchQueryDto query, CancellationToken ct)
    {
        var result = await _itemService.SearchAsync(query, GetOptionalUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách danh mục kèm số lượng tin đăng đang bán.
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _itemService.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    /// <summary>
    /// Chi tiết tin đăng theo id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _itemService.GetByIdAsync(id, GetOptionalUserId(), ct);
        return Ok(item);
    }

    /// <summary>
    /// Chi tiết tin đăng theo slug (URL thân thiện).
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var item = await _itemService.GetBySlugAsync(slug, GetOptionalUserId(), ct);
        return Ok(item);
    }

    /// <summary>
    /// Tin đăng của một người bán.
    /// </summary>
    [HttpGet("seller/{sellerId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySeller(Guid sellerId, [FromQuery] ItemSearchQueryDto query, CancellationToken ct)
    {
        var result = await _itemService.GetBySellerAsync(sellerId, GetOptionalUserId(), query, ct);
        return Ok(result);
    }

    private Guid? GetOptionalUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
