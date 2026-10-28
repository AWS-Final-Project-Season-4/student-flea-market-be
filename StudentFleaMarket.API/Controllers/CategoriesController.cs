using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.API.Contracts;
using StudentFleaMarket.Application.Catalog;

namespace StudentFleaMarket.API.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> Get(CancellationToken cancellationToken) =>
        Ok(new ApiResponse<IReadOnlyList<CategoryDto>>(await catalogService.GetCategoriesAsync(cancellationToken)));
}
