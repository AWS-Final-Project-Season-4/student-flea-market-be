using FluentValidation;

namespace StudentFleaMarket.Application.DTOs.Profile;

public class UpdateProfileRequestDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SchoolName { get; set; }
    public string? StudentCode { get; set; }
    public string? CommonArea { get; set; }
    public string? Dormitory { get; set; }
}

public class UpdateProfileRequestDtoValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Full name cannot be blank.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Invalid phone number format.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).WithMessage("School name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchoolName));

        RuleFor(x => x.StudentCode)
            .MaximumLength(50).WithMessage("Student code must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode));

        RuleFor(x => x.CommonArea)
            .MaximumLength(200).WithMessage("Common area must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CommonArea));

        RuleFor(x => x.Dormitory)
            .MaximumLength(200).WithMessage("Dormitory must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Dormitory));
    }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password cannot be blank.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("New password cannot be blank.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .MaximumLength(100).WithMessage("New password must not exceed 100 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter (A-Z).")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter (a-z).")
            .Matches(@"\d").WithMessage("New password must contain at least one digit (0-9).")
            .Matches(@"[\W_]").WithMessage("New password must contain at least one special character (!@#$%^&*...).");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Password confirmation does not match.");
    }
}
