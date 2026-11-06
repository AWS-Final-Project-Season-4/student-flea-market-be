using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Admin;

// ============================================================ MODERATOR (kiểm duyệt viên)

/// <summary>
/// Admin cấp role kiểm duyệt viên (Moderator) cho một tài khoản thường.
/// </summary>
public class AssignModeratorRequestDto
{
    /// <summary>
    /// Lý do / ghi chú về việc cấp quyền (để truy vết).
    /// </summary>
    public string? Note { get; set; }
}

public class AssignModeratorRequestDtoValidator : AbstractValidator<AssignModeratorRequestDto>
{
    public AssignModeratorRequestDtoValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

/// <summary>
/// Thông tin về một tài khoản kiểm duyệt viên (Moderator).
/// </summary>
public class ModeratorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Username { get; set; }

    /// <summary>
    /// Thời điểm được cấp role kiểm duyệt viên.
    /// </summary>
    public DateTime? GrantedAt { get; set; }

    /// <summary>
    /// Admin đã cấp role (nếu có).
    /// </summary>
    public Guid? GrantedByUserId { get; set; }

    /// <summary>
    /// Tên Admin đã cấp role (nếu có).
    /// </summary>
    public string? GrantedByFullName { get; set; }
}
