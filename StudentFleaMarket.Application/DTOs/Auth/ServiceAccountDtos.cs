using FluentValidation;
using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.Application.DTOs.Auth;

public class CreateServiceAccountRequestDto
{
    /// <summary>
    /// Tên dịch vụ (vd: "ai-moderation", "ai-recommendation").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Ngày hết hạn (không bắt buộc). null = không hết hạn.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

public class CreateServiceAccountRequestDtoValidator : AbstractValidator<CreateServiceAccountRequestDto>
{
    public CreateServiceAccountRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name cannot be left blank.")
            .MinimumLength(3).WithMessage("Service name must be at least 3 characters long.")
            .MaximumLength(100).WithMessage("Service name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Service name can only contain letters, numbers, dots, underscores and hyphens.");

        RuleFor(x => x.ExpiresAt)
            .Must(x => x is null || x > DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Expiry date must be in the future.");
    }
}

/// <summary>
/// Kết quả tạo tài khoản dịch vụ. ApiKey chỉ được trả về đúng 1 lần này.
/// </summary>
public class ServiceAccountCreatedDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string KeyPrefix { get; set; } = null!;
    public string RoleName { get; set; } = "AiService";
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ServiceAccountCreatedDto From(
        Domain.Entities.ServiceAccount account,
        string apiKey) => new()
    {
        Id = account.Id,
        Name = account.Name,
        ApiKey = apiKey,
        KeyPrefix = account.KeyPrefix,
        RoleName = "AiService",
        ExpiresAt = account.ExpiresAt,
        CreatedAt = account.CreatedAt
    };
}

public class ServiceAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string KeyPrefix { get; set; } = null!;
    public string RoleName { get; set; } = "AiService";
    public int Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
}
