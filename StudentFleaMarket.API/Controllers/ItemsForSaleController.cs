using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// Nhóm 2 — ĐĂNG BÁN & QUẢN LÝ TIN CỦA MÌNH.
/// Tất cả endpoint ở đây đều yêu cầu token và chỉ tác động lên
/// tin đăng do chính người gọi làm chủ (trừ khi là Admin).
/// </summary>
[ApiController]
[Route("api/Items/sell")]
[Authorize(Roles = "User, Admin")]
[Produces("application/json")]
public class ItemsForSaleController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsForSaleController(IItemService itemService)
    {
        _itemService = itemService;
    }

    /// <summary>
    /// Đăng tin bán đồ cũ. Giá 0 = tặng miễn phí.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemRequestDto request, CancellationToken ct)
    {
        var item = await _itemService.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(
            actionName: nameof(ItemsController.GetById),
            controllerName: "Items",
            routeValues: new { id = item.Id },
            value: item);
    }

    /// <summary>
    /// Cập nhật tin đăng (chỉ chủ tin được sửa).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemRequestDto request, CancellationToken ct)
    {
        var item = await _itemService.UpdateAsync(GetUserId(), id, request, ct);
        return Ok(item);
    }

    /// <summary>
    /// Gỡ tin đăng (soft delete). Chủ tin hoặc Admin được quyền.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _itemService.DeleteAsync(GetUserId(), isAdmin, id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Danh sách tin đăng của chính mình (chủ tin thấy được cả tin đã gỡ / đã bán).
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] ItemSearchQueryDto query, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _itemService.GetBySellerAsync(userId, userId, query, ct);
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
