using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.Application.Catalog;

public sealed record CategoryDto(int Id, string Name, string Slug, string? Description, int Order);

public sealed record ItemDto(
    Guid Id,
    Guid SellerId,
    string SellerName,
    int CategoryId,
    string CategoryName,
    string Title,
    string Slug,
    string? Description,
    decimal Price,
    int Condition,
    bool IsExchangeEnabled,
    string Location,
    int Status,
    DateTime CreatedAt,
    IReadOnlyList<string> Images);

public sealed record CreateItemRequest(
    int CategoryId,
    string Title,
    string? Description,
    decimal Price,
    int Condition,
    bool IsExchangeEnabled,
    string Location,
    IReadOnlyList<string>? ImageUrls);

public sealed record UpdateItemRequest(
    int CategoryId,
    string Title,
    string? Description,
    decimal Price,
    int Condition,
    bool IsExchangeEnabled,
    string Location,
    int Status,
    IReadOnlyList<string>? ImageUrls);

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ItemDto>> GetItemsAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ItemDto> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItemDto> CreateItemAsync(Guid sellerId, CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<ItemDto> UpdateItemAsync(Guid sellerId, Guid id, UpdateItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid sellerId, Guid id, CancellationToken cancellationToken = default);
}
