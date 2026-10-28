using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Catalog;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Catalog;

internal sealed partial class CatalogService(AppDbContext dbContext) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(x => x.Order)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.Description, x.Order))
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<ItemDto>> GetItemsAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.Items.AsNoTracking().Where(x => !x.IsDeleted).Include(x => x.Seller).Include(x => x.Category).Include(x => x.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{search.Trim()}%"));
        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<ItemDto>(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ItemDto> GetItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await ItemQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
        return Map(item);
    }

    public async Task<ItemDto> CreateItemAsync(Guid sellerId, CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Id == sellerId && x.Status == 1, cancellationToken))
            throw new ForbiddenException("Seller account is not active.");
        if (!await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
            throw new NotFoundException("Category was not found.");

        var baseSlug = Slugify(request.Title);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "item";
        baseSlug = baseSlug[..Math.Min(baseSlug.Length, 210)];
        var item = new Item
        {
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = $"{baseSlug}-{Guid.NewGuid():N}",
            Description = request.Description?.Trim(),
            Price = request.Price,
            Condition = request.Condition,
            IsExchangeEnabled = request.IsExchangeEnabled,
            Location = request.Location.Trim(),
            Status = 1
        };
        AddImages(item, request.ImageUrls);
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetItemAsync(item.Id, cancellationToken);
    }

    public async Task<ItemDto> UpdateItemAsync(Guid sellerId, Guid id, UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Items.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
        if (item.SellerId != sellerId)
            throw new ForbiddenException("Only the seller can update this item.");
        if (!await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
            throw new NotFoundException("Category was not found.");

        item.CategoryId = request.CategoryId;
        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Price = request.Price;
        item.Condition = request.Condition;
        item.IsExchangeEnabled = request.IsExchangeEnabled;
        item.Location = request.Location.Trim();
        item.Status = request.Status;
        item.UpdatedAt = DateTime.UtcNow;
        dbContext.ItemImages.RemoveRange(item.Images);
        item.Images.Clear();
        AddImages(item, request.ImageUrls);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetItemAsync(item.Id, cancellationToken);
    }

    public async Task DeleteItemAsync(Guid sellerId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Items.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
        if (item.SellerId != sellerId)
            throw new ForbiddenException("Only the seller can delete this item.");
        item.IsDeleted = true;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Item> ItemQuery() => dbContext.Items.AsNoTracking().Where(x => !x.IsDeleted).Include(x => x.Seller).Include(x => x.Category).Include(x => x.Images);

    private static ItemDto Map(Item x) => new(x.Id, x.SellerId, x.Seller.FullName, x.CategoryId, x.Category.Name, x.Title, x.Slug,
        x.Description, x.Price, x.Condition, x.IsExchangeEnabled, x.Location, x.Status, x.CreatedAt,
        x.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).ToList());

    private static void AddImages(Item item, IReadOnlyList<string>? urls)
    {
        if (urls is null) return;
        for (var index = 0; index < urls.Count; index++)
            item.Images.Add(new ItemImage { ImageUrl = urls[index], IsPrimary = index == 0, Order = index });
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var ascii = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return MultipleHyphens().Replace(InvalidSlugCharacters().Replace(ascii, "-"), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidSlugCharacters();
    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleHyphens();
}
