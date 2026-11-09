using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Admin;

// ============================================================ QUẢN LÝ DANH MỤC

/// <summary>
/// Một danh mục trong bảng quản lý của Admin / Moderator.
/// Khác với <c>CategoryDto</c> (bản public) ở chỗ có thêm trạng thái mở/đóng
/// và số tin đăng đang bán.
/// </summary>
public class AdminCategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }

    /// <summary>Thứ tự hiển thị (số nhỏ lên trước).</summary>
    public int Order { get; init; }

    /// <summary>Danh mục đang mở (cho phép đăng tin mới) hay đã đóng.</summary>
    public bool IsActive { get; init; }

    /// <summary>Số tin đăng đang bán trong danh mục này.</summary>
    public int ItemCount { get; init; }

    /// <summary>Số tin đăng đang bị ẩn do kiểm duyệt trong danh mục này.</summary>
    public int HiddenItemCount { get; init; }
}

/// <summary>
/// Tạo danh mục mới.
/// </summary>
public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// Thứ tự hiển thị. Bỏ trống -> tự động đặt sau danh mục cuối cùng.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>Danh mục mở hay đóng ngay từ đầu (mặc định mở).</summary>
    public bool IsActive { get; set; } = true;
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống.")
            .MinimumLength(2).WithMessage("Tên danh mục phải có ít nhất 2 ký tự.")
            .MaximumLength(100).WithMessage("Tên danh mục không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm.")
            .When(x => x.Order.HasValue);
    }
}

/// <summary>
/// Cập nhật danh mục (tên / mô tả / thứ tự).
/// </summary>
public class UpdateCategoryDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Tên danh mục phải có ít nhất 2 ký tự.")
            .MaximumLength(100).WithMessage("Tên danh mục không được vượt quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm.")
            .When(x => x.Order.HasValue);
    }
}

/// <summary>
/// Bật / tắt danh mục (đóng mở sàn theo danh mục).
/// </summary>
public class ToggleCategoryDto
{
    /// <summary>true = mở danh mục, false = đóng danh mục.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Sắp xếp lại thứ tự hiển thị của nhiều danh mục cùng lúc.
/// Danh sách Id theo đúng thứ tự mong muốn (đầu tiên = hiển thị trước).
/// </summary>
public class ReorderCategoriesDto
{
    public List<int> CategoryIds { get; set; } = new();
}

public class ReorderCategoriesDtoValidator : AbstractValidator<ReorderCategoriesDto>
{
    public ReorderCategoriesDtoValidator()
    {
        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("Danh sách danh mục không được để trống.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách danh mục không được có trùng lặp.")
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("Id danh mục không hợp lệ.");
    }
}
