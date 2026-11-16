using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Admin;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// QUẢN LÝ DANH MỤC SẢN PHẨM — phía Admin & Moderator.
///
/// Tính năng "đóng / mở danh mục" là yêu cầu của Feature 5 (Admin Dashboard):
/// khi một nhóm hàng trên sàn bị "săn" tin giả hoặc giao dịch có rủi ro
/// (vd: "điện thoại", "thẻ cào"), quản trị viên có thể ĐÓNG danh mục đó lại
/// để chặn ngay mà không cần gỡ từng tin.
///
/// 3 quy tắc an toàn quan trọng:
///
/// 1) KHÔNG XOÁ ĐƯỢC DANH MỤC CÒN TIN ĐĂNG.
///    Tin đăng có FK bắt buộc tới danh mục. Xoá danh mục còn tin sẽ làm hỏng
///    dữ liệu — nên ta chặn và yêu cầu chuyển tin sang danh mục khác trước,
///    hoặc chỉ đóng danh mục lại.
///
/// 2) TIN ĐĂNG CŨ VẪN HIỂN THỊ KHI DANH MỤC ĐÓNG.
///    Đóng danh mục = chặn TIN MỚI, không phải xoá lịch sử. Người bán vẫn
///    quản lý được tin cũ của mình.
///
/// 3) SLUG DUY NHẤT.
///    Slug dùng làm URL thân thiện nên phải duy nhất — khi tạo / đổi tên
///    ta tự sinh slug kèm hậu tố nếu cần.
/// </summary>
public class CategoryManagementService : ICategoryManagementService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserRepository _userRepo;

    public CategoryManagementService(ICategoryRepository categoryRepo, IUserRepository userRepo)
    {
        _categoryRepo = categoryRepo;
        _userRepo = userRepo;
    }

    // =====================================================================
    // LIST
    // =====================================================================
    public async Task<List<AdminCategoryDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _categoryRepo.ListForManagementAsync(ct);

        return rows
            .Select(r => new AdminCategoryDto
            {
                Id = r.Category.Id,
                Name = r.Category.Name,
                Slug = r.Category.Slug,
                Description = r.Category.Description,
                Order = r.Category.Order,
                IsActive = r.Category.IsActive,
                ItemCount = r.ItemCount,
                HiddenItemCount = r.HiddenItemCount
            })
            .ToList();
    }

    // =====================================================================
    // CREATE
    // =====================================================================
    public async Task<AdminCategoryDto> CreateAsync(
        Guid staffUserId,
        CreateCategoryDto request,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(staffUserId, ct);

        var name = request.Name.Trim();

        var order = request.Order ?? (await _categoryRepo.GetMaxOrderAsync(ct)) + 1;

        var category = new Category
        {
            Name = name,
            Slug = await GenerateUniqueSlugAsync(name, ct),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Order = order,
            IsActive = request.IsActive
        };

        await _categoryRepo.AddAsync(category, ct);
        await _categoryRepo.SaveChangesAsync(ct);

        return await BuildDtoAsync(category.Id, ct);
    }

    // =====================================================================
    // UPDATE
    // =====================================================================
    public async Task<AdminCategoryDto> UpdateAsync(
        Guid staffUserId,
        int categoryId,
        UpdateCategoryDto request,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(staffUserId, ct);

        var category = await GetCategoryOrThrowAsync(categoryId, ct);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var newName = request.Name.Trim();

            // Đổi tên -> đổi slug theo (slug là URL thân thiện của danh mục).
            if (!string.Equals(newName, category.Name, StringComparison.OrdinalIgnoreCase))
            {
                category.Name = newName;
                category.Slug = await GenerateUniqueSlugAsync(newName, ct);
            }
        }

        if (request.Description is not null)
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (request.Order.HasValue)
            category.Order = request.Order.Value;

        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync(ct);

        return await BuildDtoAsync(category.Id, ct);
    }

    // =====================================================================
    // TOGGLE (ĐÓNG / MỞ)
    // =====================================================================
    public async Task<AdminCategoryDto> ToggleAsync(
        Guid staffUserId,
        int categoryId,
        ToggleCategoryDto request,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(staffUserId, ct);

        var category = await GetCategoryOrThrowAsync(categoryId, ct);

        category.IsActive = request.IsActive;

        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync(ct);

        return await BuildDtoAsync(category.Id, ct);
    }

    // =====================================================================
    // DELETE
    // =====================================================================
    public async Task<ApiResponse<object>> DeleteAsync(
        Guid staffUserId,
        int categoryId,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(staffUserId, ct);

        var category = await GetCategoryOrThrowAsync(categoryId, ct);

        // Chặn xoá danh mục còn tin đăng — tin đăng có FK bắt buộc tới danh mục.
        var itemCount = await _categoryRepo.CountItemsAsync(categoryId, ct);
        if (itemCount > 0)
            throw new ValidationException(
                $"Danh mục này còn {itemCount} tin đăng nên không thể xoá. " +
                "Hãy chuyển các tin đăng sang danh mục khác trước, hoặc chỉ đóng danh mục lại.",
                "CATEGORY_HAS_ITEMS");

        _categoryRepo.Remove(category);
        await _categoryRepo.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { Message = "Đã xoá danh mục." });
    }

    // =====================================================================
    // REORDER
    // =====================================================================
    public async Task<List<AdminCategoryDto>> ReorderAsync(
        Guid staffUserId,
        ReorderCategoriesDto request,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(staffUserId, ct);

        var ids = request.CategoryIds;

        // Chỉ gán thứ tự cho các danh mục được liệt kê; các danh mục còn lại
        // giữ nguyên thứ tự cũ và đẩy xuống sau.
        var order = 0;

        foreach (var id in ids)
        {
            var category = await _categoryRepo.GetByIdAsync(id, ct);
            if (category is null)
                throw new NotFoundException($"Không tìm thấy danh mục có id {id}.", "CATEGORY_NOT_FOUND");

            category.Order = order++;
            _categoryRepo.Update(category);
        }

        await _categoryRepo.SaveChangesAsync(ct);

        return await ListAsync(ct);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task EnsureCanManageAsync(Guid staffUserId, CancellationToken ct)
    {
        var actor = await _userRepo.GetByIdAsync(staffUserId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (!StaffPermission.CanManageCategories(actor))
            throw new ForbiddenException(
                "Chỉ quản trị viên và kiểm duyệt viên mới được quản lý danh mục.",
                "FORBIDDEN");
    }

    private async Task<Category> GetCategoryOrThrowAsync(int categoryId, CancellationToken ct)
        => await _categoryRepo.GetByIdAsync(categoryId, ct)
           ?? throw new NotFoundException("Không tìm thấy danh mục.", "CATEGORY_NOT_FOUND");

    /// <summary>
    /// Sinh slug duy nhất cho danh mục. Nếu slug gốc đã tồn tại, thêm hậu tố
    /// số để tránh trùng (Slug có unique index ở DB).
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = SlugHelper.Generate(name);

        if (!await _categoryRepo.ExistsBySlugAsync(baseSlug, ct))
            return baseSlug;

        for (var i = 2; i < 100; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!await _categoryRepo.ExistsBySlugAsync(candidate, ct))
                return candidate;
        }

        // Cực hiếm — fallback về slug có hậu tố ngẫu nhiên.
        return SlugHelper.GenerateUnique(name);
    }

    private async Task<AdminCategoryDto> BuildDtoAsync(int categoryId, CancellationToken ct)
    {
        var rows = await _categoryRepo.ListForManagementAsync(ct);
        var row = rows.FirstOrDefault(r => r.Category.Id == categoryId);

        if (row.Category is null)
            throw new NotFoundException("Không tìm thấy danh mục.", "CATEGORY_NOT_FOUND");

        return new AdminCategoryDto
        {
            Id = row.Category.Id,
            Name = row.Category.Name,
            Slug = row.Category.Slug,
            Description = row.Category.Description,
            Order = row.Category.Order,
            IsActive = row.Category.IsActive,
            ItemCount = row.ItemCount,
            HiddenItemCount = row.HiddenItemCount
        };
    }
}
