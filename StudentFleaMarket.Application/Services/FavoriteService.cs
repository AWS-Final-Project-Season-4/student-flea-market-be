using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUserRepository _userRepo;

    public FavoriteService(
        IFavoriteRepository favoriteRepo,
        IItemRepository itemRepo,
        IUserRepository userRepo)
    {
        _favoriteRepo = favoriteRepo;
        _itemRepo = itemRepo;
        _userRepo = userRepo;
    }

    public async Task<FavoriteActionDto> AddAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        // Không yêu thích tin đã gỡ / đã bán
        if (item.Status is (int)ItemStatus.Removed or (int)ItemStatus.Sold)
            throw new ValidationException("Tin đăng này không còn khả dụng để yêu thích.", "ITEM_NOT_AVAILABLE");

        if (await _favoriteRepo.ExistsAsync(userId, itemId, ct))
        {
            var count = await _favoriteRepo.CountByItemAsync(itemId, ct);
            return new FavoriteActionDto { ItemId = itemId, IsFavorited = true, FavoriteCount = count };
        }

        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            CreatedAt = DateTime.UtcNow
        };

        await _favoriteRepo.AddAsync(favorite, ct);
        await _favoriteRepo.SaveChangesAsync(ct);

        var newCount = await _favoriteRepo.CountByItemAsync(itemId, ct);
        return new FavoriteActionDto { ItemId = itemId, IsFavorited = true, FavoriteCount = newCount };
    }

    public async Task<FavoriteActionDto> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var favorite = await _favoriteRepo.FirstOrDefaultAsync(
            f => f.UserId == userId && f.ItemId == itemId, ct);

        if (favorite is not null)
        {
            _favoriteRepo.Remove(favorite);
            await _favoriteRepo.SaveChangesAsync(ct);
        }

        var count = await _favoriteRepo.CountByItemAsync(itemId, ct);
        return new FavoriteActionDto { ItemId = itemId, IsFavorited = false, FavoriteCount = count };
    }

    public async Task<FavoriteActionDto> ToggleAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        if (await _favoriteRepo.ExistsAsync(userId, itemId, ct))
            return await RemoveAsync(userId, itemId, ct);

        return await AddAsync(userId, itemId, ct);
    }

    public async Task<PagedResult<ItemSummaryDto>> ListAsync(
        Guid userId,
        FavoriteQueryDto query,
        CancellationToken ct = default)
    {
        var (favorites, total) = await _favoriteRepo.ListByUserAsync(userId, query, ct);

        var items = favorites
            .Select(f => f.Item)
            .Where(i => i is not null)
            .Select(i => new ItemSummaryDto
            {
                Id = i.Id,
                Title = i.Title,
                Slug = i.Slug,
                Price = i.Price,
                Condition = i.Condition,
                ConditionName = ItemEnumLabels.ConditionName(i.Condition),
                IsExchangeEnabled = i.IsExchangeEnabled,
                Location = i.Location,
                Status = i.Status,
                StatusName = ItemEnumLabels.StatusName(i.Status),
                CategoryId = i.CategoryId,
                CategoryName = i.Category?.Name ?? "",
                CategorySlug = i.Category?.Slug ?? "",
                SellerId = i.SellerId,
                SellerName = i.Seller?.FullName ?? "",
                PrimaryImageUrl = i.Images
                    .OrderBy(img => img.IsPrimary ? 0 : 1)
                    .ThenBy(img => img.Order)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                CreatedAt = i.CreatedAt
            })
            .ToList();

        return new PagedResult<ItemSummaryDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page < 1 ? 1 : query.Page,
            PageSize = query.PageSize
        };
    }
}
