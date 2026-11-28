using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Service managing student flea market item listings:
/// creation, updates, soft deletion, search, and retrieval.
/// </summary>
public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IUserRepository _userRepo;
    private readonly IReviewRepository _reviewRepo;

    public ItemService(
        IItemRepository itemRepo,
        ICategoryRepository categoryRepo,
        IFavoriteRepository favoriteRepo,
        IUserRepository userRepo,
        IReviewRepository reviewRepo)
    {
        _itemRepo = itemRepo;
        _categoryRepo = categoryRepo;
        _favoriteRepo = favoriteRepo;
        _userRepo = userRepo;
        _reviewRepo = reviewRepo;
    }

    // ------------------------------------------------------------ CREATE
    public async Task<ItemDetailDto> CreateAsync(Guid sellerId, CreateItemRequestDto request, CancellationToken ct = default)
    {
        // 1. Verify that category exists
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException("Category does not exist.", "CATEGORY_NOT_FOUND");

        // 2. Closed category cannot accept new listings (Admin/Moderator circuit breaker)
        if (!category.IsActive)
            throw new ValidationException(
                $"Category \"{category.Name}\" is currently inactive and cannot accept new listings.",
                "CATEGORY_INACTIVE");

        // 3. Seller must be a real user account
        var seller = await _userRepo.GetByIdAsync(sellerId, ct)
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");

        if (seller.Role == (int)UserRole.AiService)
            throw new ForbiddenException("AI service accounts are not permitted to list items for sale.", "AI_CANNOT_CREATE_ITEM");

        // 4. Ensure user has approved seller permissions
        SellerPermission.EnsureCanSell(seller);

        var item = new Item
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = SlugHelper.GenerateUnique(request.Title),
            TitleNormalized = SlugHelper.NormalizeForSearch(request.Title),
            // Automatic ItemCode generation with category-based prefix (BK, EL, ST...)
            ItemCode = await GenerateUniqueItemCodeAsync(ItemCodeHelper.GetCategoryPrefix(category.Slug ?? category.Name), ct),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            Quantity = request.Quantity > 0 ? request.Quantity : 1,
            Price = request.Price,
            Condition = request.Condition,
            IsExchangeEnabled = request.IsExchangeEnabled,
            Location = request.Location.Trim(),
            Status = (int)ItemStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _itemRepo.AddAsync(item, ct);

        // Attach images (first image is primary)
        var imageUrls = NormalizeImageUrls(request.ImageUrls);
        for (var i = 0; i < imageUrls.Count; i++)
        {
            item.Images.Add(new ItemImage
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                ImageUrl = imageUrls[i],
                IsPrimary = i == 0,
                Order = i,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _itemRepo.SaveChangesAsync(ct);

        return await BuildDetailDto(item, category, seller, favoriteCount: 0, isFavorited: false);
    }

    // ------------------------------------------------------------ UPDATE
    public async Task<ItemDetailDto> UpdateAsync(Guid userId, Guid itemId, UpdateItemRequestDto request, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Listing not found.", "ITEM_NOT_FOUND");

        // Only the seller can edit their own listing
        if (item.SellerId != userId)
            throw new ForbiddenException("You do not have permission to edit this listing.", "NOT_ITEM_OWNER");

        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct)
            ?? throw new NotFoundException("Seller not found.", "USER_NOT_FOUND");

        SellerPermission.EnsureCanSell(seller);

        if (item.Status == (int)ItemStatus.Sold)
            throw new ValidationException("Sold items cannot be edited.", "ITEM_ALREADY_SOLD");

        if (request.CategoryId.HasValue)
        {
            var newCategory = await _categoryRepo.GetByIdAsync(request.CategoryId.Value, ct)
                ?? throw new NotFoundException("Category does not exist.", "CATEGORY_NOT_FOUND");

            if (!newCategory.IsActive)
                throw new ValidationException(
                    $"Category \"{newCategory.Name}\" is currently inactive and cannot accept listings.",
                    "CATEGORY_INACTIVE");

            item.CategoryId = newCategory.Id;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            item.Title = request.Title.Trim();
            item.Slug = SlugHelper.GenerateUnique(request.Title);
            item.TitleNormalized = SlugHelper.NormalizeForSearch(request.Title);
        }

        if (request.Description is not null)
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (request.Color is not null)
            item.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();

        if (request.Quantity.HasValue && request.Quantity.Value > 0)
            item.Quantity = request.Quantity.Value;

        if (request.Price.HasValue)
            item.Price = request.Price.Value;

        if (request.Condition.HasValue)
            item.Condition = request.Condition.Value;

        if (request.IsExchangeEnabled.HasValue)
            item.IsExchangeEnabled = request.IsExchangeEnabled.Value;

        if (!string.IsNullOrWhiteSpace(request.Location))
            item.Location = request.Location.Trim();

        // Replace all images if ImageUrls is provided
        if (request.ImageUrls is not null)
        {
            var oldImages = await _itemRepo.GetImagesAsync(itemId, ct);
            foreach (var img in oldImages)
                await _itemRepo.RemoveImageAsync(img, ct);

            var newUrls = NormalizeImageUrls(request.ImageUrls);
            for (var i = 0; i < newUrls.Count; i++)
            {
                await _itemRepo.AddImageAsync(new ItemImage
                {
                    Id = Guid.NewGuid(),
                    ItemId = item.Id,
                    ImageUrl = newUrls[i],
                    IsPrimary = i == 0,
                    Order = i,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        item.UpdatedAt = DateTime.UtcNow;
        _itemRepo.Update(item);
        await _itemRepo.SaveChangesAsync(ct);

        var reloaded = await _itemRepo.GetDetailAsync(itemId, ct) ?? item;
        var category = await _categoryRepo.GetByIdAsync(reloaded.CategoryId, ct);
        var favCount = await _favoriteRepo.CountByItemAsync(itemId, ct);
        var isFav = await _favoriteRepo.ExistsAsync(userId, itemId, ct);

        return await BuildDetailDto(reloaded, category, seller, favCount, isFav);
    }

    // ------------------------------------------------------------ DELETE
    public async Task<ApiResponse<object>> DeleteAsync(Guid userId, bool isStaff, Guid itemId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Listing not found.", "ITEM_NOT_FOUND");

        if (!isStaff && item.SellerId != userId)
            throw new ForbiddenException("You do not have permission to delete this listing.", "NOT_ITEM_OWNER");

        // Soft delete — hides item from public queries while preserving transaction history
        item.IsDeleted = true;
        item.Status = (int)ItemStatus.Removed;
        item.UpdatedAt = DateTime.UtcNow;

        _itemRepo.Update(item);
        await _itemRepo.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { Message = "Listing removed successfully." });
    }

    // ------------------------------------------------------------ GET
    public async Task<ItemDetailDto> GetByIdAsync(Guid itemId, Guid? userId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetDetailAsync(itemId, ct)
            ?? throw new NotFoundException("Listing not found.", "ITEM_NOT_FOUND");

        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct);
        var favCount = await _favoriteRepo.CountByItemAsync(itemId, ct);
        var isFav = userId.HasValue && await _favoriteRepo.ExistsAsync(userId.Value, itemId, ct);

        return await BuildDetailDto(item, item.Category, seller, favCount, isFav);
    }

    public async Task<ItemDetailDto> GetBySlugAsync(string slug, Guid? userId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetBySlugAsync(slug, ct)
            ?? throw new NotFoundException("Listing not found.", "ITEM_NOT_FOUND");

        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct);
        var favCount = await _favoriteRepo.CountByItemAsync(item.Id, ct);
        var isFav = userId.HasValue && await _favoriteRepo.ExistsAsync(userId.Value, item.Id, ct);

        return await BuildDetailDto(item, item.Category, seller, favCount, isFav);
    }

    public async Task<ItemDetailDto> GetByCodeAsync(string code, Guid? userId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetByCodeAsync(code, ct)
            ?? throw new NotFoundException(
                $"Listing with code \"{ItemCodeHelper.Normalize(code)}\" was not found.",
                "ITEM_CODE_NOT_FOUND");

        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct);
        var favCount = await _favoriteRepo.CountByItemAsync(item.Id, ct);
        var isFav = userId.HasValue && await _favoriteRepo.ExistsAsync(userId.Value, item.Id, ct);

        return await BuildDetailDto(item, item.Category, seller, favCount, isFav);
    }

    // ------------------------------------------------------------ SEARCH
    public async Task<PagedResult<ItemSummaryDto>> SearchAsync(
        ItemSearchQueryDto query,
        Guid? userId,
        CancellationToken ct = default)
        => await SearchInternalAsync(query, userId, includeAllStatuses: false, ct);

    private async Task<PagedResult<ItemSummaryDto>> SearchInternalAsync(
        ItemSearchQueryDto query,
        Guid? userId,
        bool includeAllStatuses,
        CancellationToken ct = default)
    {
        if (!ItemSortOptions.IsValid(query.SortBy))
            throw new ValidationException("Invalid sort option.", "INVALID_SORT_BY");

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
            throw new ValidationException("Minimum price cannot exceed maximum price.", "INVALID_PRICE_RANGE");

        var (items, total) = await _itemRepo.SearchAsync(query, includeAllStatuses, ct);

        var favoriteIds = userId.HasValue
            ? await _favoriteRepo.GetFavoriteItemIdsAsync(userId.Value, ct)
            : new List<Guid>();

        var favSet = favoriteIds.ToHashSet();

        // Count favorites per item in a single batch query (avoids N+1 query problem)
        var favCounts = await _favoriteRepo.GetFavoriteCountsAsync(items.Select(i => i.Id), ct);

        var result = items
            .Select(i => MapSummary(i, favSet.Contains(i.Id), favCounts.GetValueOrDefault(i.Id)))
            .ToList();

        return new PagedResult<ItemSummaryDto>
        {
            Items = result,
            TotalCount = total,
            Page = query.Page < 1 ? 1 : query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<PagedResult<ItemSummaryDto>> GetBySellerAsync(
        Guid sellerId,
        Guid? userId,
        ItemSearchQueryDto query,
        CancellationToken ct = default)
    {
        query.SellerId = sellerId;

        // Listing owner can also see their own removed or sold listings
        var isOwner = userId.HasValue && userId.Value == sellerId;

        return await SearchInternalAsync(query, userId, includeAllStatuses: isOwner, ct);
    }

    // ------------------------------------------------------------ CATEGORY
    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var rows = await _categoryRepo.ListWithCountsAsync(ct);

        // Only return active categories for public display
        return rows
            .Where(r => r.Category.IsActive)
            .Select(r => new CategoryDto
            {
                Id = r.Category.Id,
                Name = r.Category.Name,
                Slug = r.Category.Slug,
                Description = r.Category.Description,
                Order = r.Category.Order,
                ItemCount = r.ItemCount
            })
            .ToList();
    }

    // ------------------------------------------------------------ HELPERS

    /// <summary>
    /// Generates a unique ItemCode that does not yet exist in the database.
    /// Length: 8 characters (Category prefix + 6 Crockford Base32 characters).
    /// </summary>
    private async Task<string> GenerateUniqueItemCodeAsync(string prefix, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = ItemCodeHelper.Generate(prefix);
            if (!await _itemRepo.ExistsByCodeAsync(code, ct))
                return code;
        }

        throw new InvalidOperationException(
            "Failed to generate a unique item code. Please try again.");
    }

    private static List<string> NormalizeImageUrls(List<string>? urls)
    {
        if (urls is null || urls.Count == 0)
            return new List<string>();

        return urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static ItemSummaryDto MapSummary(Item item, bool isFavorited, int favoriteCount = 0)
    {
        var primary = item.Images
            .OrderBy(i => i.IsPrimary ? 0 : 1)
            .ThenBy(i => i.Order)
            .FirstOrDefault();

        return new ItemSummaryDto
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            ItemCode = item.ItemCode,
            Price = item.Price,
            Color = item.Color,
            Quantity = item.Quantity,
            Condition = item.Condition,
            ConditionName = ItemEnumLabels.ConditionName(item.Condition),
            IsExchangeEnabled = item.IsExchangeEnabled,
            Location = item.Location,
            Status = item.Status,
            StatusName = ItemEnumLabels.StatusName(item.Status),
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? "",
            CategorySlug = item.Category?.Slug ?? "",
            SellerId = item.SellerId,
            SellerName = item.Seller?.FullName ?? "",
            PrimaryImageUrl = primary?.ImageUrl,
            FavoriteCount = favoriteCount,
            IsFavorited = isFavorited,
            CreatedAt = item.CreatedAt
        };
    }

    private async Task<ItemDetailDto> BuildDetailDto(
        Item item,
        Category? category,
        ApplicationUser? seller,
        int favoriteCount,
        bool isFavorited,
        CancellationToken ct = default)
    {
        var (ratingAvg, ratingCount) = await _reviewRepo.GetRatingStatsAsync(item.SellerId, ct);

        return new ItemDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            ItemCode = item.ItemCode,
            Description = item.Description,
            Color = item.Color,
            Quantity = item.Quantity,
            Price = item.Price,
            Condition = item.Condition,
            ConditionName = ItemEnumLabels.ConditionName(item.Condition),
            IsExchangeEnabled = item.IsExchangeEnabled,
            Location = item.Location,
            Status = item.Status,
            StatusName = ItemEnumLabels.StatusName(item.Status),
            Category = new CategoryDto
            {
                Id = item.CategoryId,
                Name = category?.Name ?? "",
                Slug = category?.Slug ?? "",
                Description = category?.Description,
                Order = category?.Order ?? 0
            },
            Seller = new SellerDto
            {
                Id = item.SellerId,
                FullName = seller?.FullName ?? "",
                AvatarUrl = seller?.AvatarUrl,
                SchoolName = seller?.SchoolName,
                StudentCode = seller?.StudentCode,
                RatingAverage = ratingAvg.HasValue ? Math.Round(ratingAvg.Value, 1) : null,
                RatingCount = ratingCount,
                MemberSince = seller?.CreatedAt ?? DateTime.UtcNow
            },
            Images = item.Images
                .OrderBy(i => i.IsPrimary ? 0 : 1)
                .ThenBy(i => i.Order)
                .Select(i => new ItemImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary,
                    Order = i.Order
                })
                .ToList(),
            FavoriteCount = favoriteCount,
            IsFavorited = isFavorited,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
