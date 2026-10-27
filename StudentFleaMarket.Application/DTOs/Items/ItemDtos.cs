using FluentValidation;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Items;

public class CreateItemRequestDto
{
    public string Title { get; set; } = null!;
    public int CategoryId { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Giá bán. 0 = tặng miễn phí.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Độ mới — xem <see cref="ItemCondition"/>: 1=New, 2=LikeNew, 3=Good, 4=Fair, 5=Poor.
    /// </summary>
    public int Condition { get; set; } = (int)ItemCondition.Good;

    /// <summary>
    /// Cho phép đổi đồ.
    /// </summary>
    public bool IsExchangeEnabled { get; set; } = false;

    /// <summary>
    /// Địa điểm gặp mặt trực tiếp (vd: "KTX Khu A", "Cổng trường").
    /// </summary>
    public string Location { get; set; } = null!;

    /// <summary>
    /// Danh sách URL ảnh đã upload (tối đa 8 ảnh).
    /// </summary>
    public List<string>? ImageUrls { get; set; }
}

public class CreateItemRequestDtoValidator : AbstractValidator<CreateItemRequestDto>
{
    public CreateItemRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .MinimumLength(3).WithMessage("Tiêu đề phải có ít nhất 3 ký tự.")
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Danh mục không hợp lệ.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Mô tả không được vượt quá 5000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá bán không hợp lệ.");

        RuleFor(x => x.Condition)
            .Must(c => Enum.IsDefined(typeof(ItemCondition), c))
            .WithMessage("Độ mới không hợp lệ.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Địa điểm gặp mặt không được để trống.")
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.");

        RuleFor(x => x.ImageUrls)
            .Must(u => u is null || u.Count <= 8)
            .WithMessage("Mỗi tin đăng chỉ được có tối đa 8 ảnh.");
    }
}

public class UpdateItemRequestDto
{
    public string? Title { get; set; }
    public int? CategoryId { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Condition { get; set; }
    public bool? IsExchangeEnabled { get; set; }
    public string? Location { get; set; }

    /// <summary>
    /// Danh sách URL ảnh mới (thay thế toàn bộ ảnh cũ). Null = giữ nguyên.
    /// </summary>
    public List<string>? ImageUrls { get; set; }
}

public class UpdateItemRequestDtoValidator : AbstractValidator<UpdateItemRequestDto>
{
    public UpdateItemRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .MinimumLength(3).WithMessage("Tiêu đề phải có ít nhất 3 ký tự.")
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Danh mục không hợp lệ.")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Mô tả không được vượt quá 5000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá bán không hợp lệ.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Condition)
            .Must(c => c.HasValue && Enum.IsDefined(typeof(ItemCondition), c.Value))
            .WithMessage("Độ mới không hợp lệ.")
            .When(x => x.Condition.HasValue);

        RuleFor(x => x.Location)
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Location));

        RuleFor(x => x.ImageUrls)
            .Must(u => u is null || u.Count <= 8)
            .WithMessage("Mỗi tin đăng chỉ được có tối đa 8 ảnh.");
    }
}

/// <summary>
/// Ảnh của tin đăng (dùng cả khi tạo lẫi khi trả về).
/// </summary>
public class ItemImageDto
{
    public Guid Id { get; init; }
    public string ImageUrl { get; init; } = null!;
    public bool IsPrimary { get; init; }
    public int Order { get; init; }
}

/// <summary>
/// Tin đăng rút gọn — dùng cho danh sách / kết quả tìm kiếm.
/// </summary>
public class ItemSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public decimal Price { get; init; }
    public int Condition { get; init; }
    public string ConditionName { get; init; } = null!;
    public bool IsExchangeEnabled { get; init; }
    public string Location { get; init; } = null!;
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public string CategorySlug { get; init; } = null!;
    public Guid SellerId { get; init; }
    public string SellerName { get; init; } = null!;
    public string? PrimaryImageUrl { get; init; }

    /// <summary>
    /// Số lượt yêu thích của tin đăng (dùng cho sortBy=most_favorited).
    /// </summary>
    public int FavoriteCount { get; init; }

    /// <summary>
    /// Người đang xem đã yêu thích tin này hay chưa.
    /// </summary>
    public bool IsFavorited { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Chi tiết đầy đủ của tin đăng.
/// </summary>
public class ItemDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Condition { get; init; }
    public string ConditionName { get; init; } = null!;
    public bool IsExchangeEnabled { get; init; }
    public string Location { get; init; } = null!;
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;
    public CategoryDto Category { get; init; } = null!;
    public SellerDto Seller { get; init; } = null!;
    public List<ItemImageDto> Images { get; init; } = new();
    public int FavoriteCount { get; init; }
    public bool IsFavorited { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class SellerDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? SchoolName { get; init; }
    public string? StudentCode { get; init; }
    public DateTime MemberSince { get; init; }
}
