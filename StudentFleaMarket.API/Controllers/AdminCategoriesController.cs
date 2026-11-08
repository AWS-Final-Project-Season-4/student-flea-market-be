using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.API.Controllers;

/// <summary>
/// QUẢN LÝ DANH MỤC SẢN PHẨM — phía Admin & Moderator.
///
/// Cho phép ĐÓNG / MỞ danh mục để chặn nhanh nhóm hàng có rủi ro (tin giả,
/// giao dịch lừa đảo) mà không cần gỡ từng tin đăng.
///
/// Endpoint:
///   GET    /api/Admin/categories                  -> danh sách (kèm trạng thái mở/đóng)
///   POST   /api/Admin/categories                  -> tạo danh mục
///   PUT    /api/Admin/categories/{id}             -> sửa danh mục
///   PATCH  /api/Admin/categories/{id}/status      -> bật / tắt danh mục
///   DELETE /api/Admin/categories/{id}             -> xoá danh mục (chỉ khi không còn tin)
///   PUT    /api/Admin/categories/reorder          -> sắp xếp lại thứ tự
/// </summary>
[ApiController]
[Route("api/Admin/categories")]
[Authorize(Roles = "Admin, Moderator")]
[Produces("application/json")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryManagementService _categoryService;

    public AdminCategoriesController(ICategoryManagementService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Danh sách danh mục kèm trạng thái mở/đóng và số tin đăng.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _categoryService.ListAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Tạo danh mục mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryDto request,
        CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(List), result);
    }

    /// <summary>
    /// Cập nhật danh mục (tên / mô tả / thứ tự hiển thị).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCategoryDto request,
        CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Bật / tắt danh mục.
    ///
    /// Khi ĐÓNG: người dùng không đăng được tin mới vào danh mục đó, và danh
    /// mục không hiển thị trong danh sách công khai. Tin đăng cũ vẫn hiển thị.
    ///
    /// Body: { "isActive": false }
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Toggle(
        int id,
        [FromBody] ToggleCategoryDto request,
        CancellationToken ct)
    {
        var result = await _categoryService.ToggleAsync(GetUserId(), id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Xoá danh mục. Chỉ xoá được khi danh mục KHÔNG còn tin đăng nào.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _categoryService.DeleteAsync(GetUserId(), id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sắp xếp lại thứ tự hiển thị của nhiều danh mục cùng lúc.
    ///
    /// Body: { "categoryIds": [3, 1, 5, 2] }  -> 3 hiển thị trước, rồi 1, 5, 2.
    /// </summary>
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderCategoriesDto request,
        CancellationToken ct)
    {
        var result = await _categoryService.ReorderAsync(GetUserId(), request, ct);
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
