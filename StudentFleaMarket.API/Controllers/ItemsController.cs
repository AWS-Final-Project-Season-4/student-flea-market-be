using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.API.Contracts;
using StudentFleaMarket.Application.Catalog;
using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/items")]
public sealed class ItemsController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ItemDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<PagedResult<ItemDto>>(await catalogService.GetItemsAsync(search, categoryId, page, pageSize, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(new ApiResponse<ItemDto>(await catalogService.GetItemAsync(id, cancellationToken)));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemDto>>> Create(CreateItemRequest request, IValidator<CreateItemRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var item = await catalogService.CreateItemAsync(CurrentUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ApiResponse<ItemDto>(item, "Item created."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> Update(Guid id, UpdateItemRequest request, IValidator<UpdateItemRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(new ApiResponse<ItemDto>(await catalogService.UpdateItemAsync(CurrentUserId(), id, request, cancellationToken), "Item updated."));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await catalogService.DeleteItemAsync(CurrentUserId(), id, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new UnauthorizedAccessException("The access token does not contain a valid user id.");
}
