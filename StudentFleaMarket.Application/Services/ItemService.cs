using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Items;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IUserRepository _userRepo;

    public ItemService(
        IItemRepository itemRepo,
        ICategoryRepository categoryRepo,
        IFavoriteRepository favoriteRepo,
        IUserRepository userRepo)
    {
        _itemRepo = itemRepo;
        _categoryRepo = categoryRepo;
        _favoriteRepo = favoriteRepo;
        _userRepo = userRepo;
    }

    // ------------------------------------------------------------ CREATE
    public async Task<ItemDetailDto> CreateAsync(Guid sellerId, CreateItemRequestDto request, CancellationToken ct = default)
    {
        // Kiểm tra danh mục tồn tại
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException("Danh mục không tồn tại.", "CATEGORY_NOT_FOUND");

        // Người bán phải là tài khoản thật (loại trừ tài khoản dịch vụ AI)
        var seller = await _userRepo.GetByIdAsync(sellerId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (seller.Role == (int)UserRole.AiService)
            throw new ForbiddenException("Tài khoản dịch vụ AI không được đăng tin bán.", "AI_CANNOT_CREATE_ITEM");

        // ---------------------------------------------------------------
        // Chỉ tài khoản được Admin duyệt mới đăng được tin mới.
        // Admin có sẵn toàn bộ quyền nên không cần duyệt.
        // Luật nằm trong SellerPermission (nguồn sự thật duy nhất).
        // ---------------------------------------------------------------
        SellerPermission.EnsureCanSell(seller);

        var item = new Item
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = SlugHelper.GenerateUnique(request.Title),
            TitleNormalized = SlugHelper.NormalizeForSearch(request.Title),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Condition = request.Condition,
            IsExchangeEnabled = request.IsExchangeEnabled,
            Location = request.Location.Trim(),
            Status = (int)ItemStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _itemRepo.AddAsync(item, ct);

        // Gắn ảnh (ảnh đầu tiên là ảnh chính)
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
        // Dùng bản tracked (không AsNoTracking) để các thay đổi ảnh được ghi nhận.
        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        // Chỉ chủ tin được sửa
        if (item.SellerId != userId)
            throw new ForbiddenException("Bạn không có quyền sửa tin đăng này.", "NOT_ITEM_OWNER");

        // ---------------------------------------------------------------
        // Chủ tin vẫn phải CÓ quyền bán mới được sửa tin.
        // Nếu Admin đã thu hồi quyền bán (SellerStatus = None) thì toàn bộ
        // tin của tài khoản đó đã bị gỡ khỏi sàn -> không còn gì để sửa.
        // Nhưng nếu tài khoản bị từ chối / đang chờ duyệt mà vẫn còn tin cũ,
        // phải chặn để không lách luật "chỉ seller được duyệt mới đăng bán".
        // ---------------------------------------------------------------
        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct)
            ?? throw new NotFoundException("Không tìm thấy người bán.", "USER_NOT_FOUND");

        SellerPermission.EnsureCanSell(seller);

        if (item.Status == (int)ItemStatus.Sold)
            throw new ValidationException("Tin đã bán không thể chỉnh sửa.", "ITEM_ALREADY_SOLD");

        if (request.CategoryId.HasValue)
        {
            var newCategory = await _categoryRepo.GetByIdAsync(request.CategoryId.Value, ct)
                ?? throw new NotFoundException("Danh mục không tồn tại.", "CATEGORY_NOT_FOUND");
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

        if (request.Price.HasValue)
            item.Price = request.Price.Value;

        if (request.Condition.HasValue)
            item.Condition = request.Condition.Value;

        if (request.IsExchangeEnabled.HasValue)
            item.IsExchangeEnabled = request.IsExchangeEnabled.Value;

        if (!string.IsNullOrWhiteSpace(request.Location))
            item.Location = request.Location.Trim();

        // Thay toàn bộ ảnh nếu có ImageUrls (kể cả mảng rỗng = xóa hết ảnh)
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
    public async Task<ApiResponse<object>> DeleteAsync(Guid userId, bool isAdmin, Guid itemId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        if (!isAdmin && item.SellerId != userId)
            throw new ForbiddenException("Bạn không có quyền xóa tin đăng này.", "NOT_ITEM_OWNER");

        // Soft delete — query filter của DbContext sẽ ẩn tin này khỏi mọi truy vấn.
        item.IsDeleted = true;
        item.Status = (int)ItemStatus.Removed;
        item.UpdatedAt = DateTime.UtcNow;

        _itemRepo.Update(item);
        await _itemRepo.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { Message = "Đã gỡ tin đăng." });
    }

    // ------------------------------------------------------------ GET
    public async Task<ItemDetailDto> GetByIdAsync(Guid itemId, Guid? userId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetDetailAsync(itemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        var seller = await _userRepo.GetByIdAsync(item.SellerId, ct);
        var favCount = await _favoriteRepo.CountByItemAsync(itemId, ct);
        var isFav = userId.HasValue && await _favoriteRepo.ExistsAsync(userId.Value, itemId, ct);

        return await BuildDetailDto(item, item.Category, seller, favCount, isFav);
    }

    public async Task<ItemDetailDto> GetBySlugAsync(string slug, Guid? userId, CancellationToken ct = default)
    {
        var item = await _itemRepo.GetBySlugAsync(slug, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

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
            throw new ValidationException("Tiêu chí sắp xếp không hợp lệ.", "INVALID_SORT_BY");

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
            throw new ValidationException("Giá tối thiểu không được lớn hơn giá tối đa.", "INVALID_PRICE_RANGE");

        var (items, total) = await _itemRepo.SearchAsync(query, includeAllStatuses, ct);

        var favoriteIds = userId.HasValue
            ? await _favoriteRepo.GetFavoriteItemIdsAsync(userId.Value, ct)
            : new List<Guid>();

        var favSet = favoriteIds.ToHashSet();

        // Số lượt yêu thích từng tin — 1 query cho cả trang (không N+1).
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

        // Chủ tin cần xem được cả tin đã gỡ / đã bán của mình.
        var isOwner = userId.HasValue && userId.Value == sellerId;

        return await SearchInternalAsync(query, userId, includeAllStatuses: isOwner, ct);
    }

    // ------------------------------------------------------------ CATEGORY
    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var rows = await _categoryRepo.ListWithCountsAsync(ct);

        return rows
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
            Price = item.Price,
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

    // ------------------------------------------------------------ HELPERS

    private static async Task<ItemDetailDto> BuildDetailDto(
        Item item,
        Category? category,
        ApplicationUser? seller,
        int favoriteCount,
        bool isFavorited)
    {
        await Task.CompletedTask;

        return new ItemDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            Description = item.Description,
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
