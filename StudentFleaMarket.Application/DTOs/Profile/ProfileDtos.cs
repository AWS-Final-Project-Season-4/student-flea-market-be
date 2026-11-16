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
            .MinimumLength(2).WithMessage("Họ tên phải có ít nhất 2 ký tự.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Số điện thoại không hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("URL avatar không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).WithMessage("Tên trường không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchoolName));

        RuleFor(x => x.StudentCode)
            .MaximumLength(50).WithMessage("Mã sinh viên không được vượt quá 50 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode));

        RuleFor(x => x.CommonArea)
            .MaximumLength(200).WithMessage("Khu vực không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.CommonArea));

        RuleFor(x => x.Dormitory)
            .MaximumLength(200).WithMessage("Ký túc xá không được vượt quá 200 ký tự.")
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
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu mới không được vượt quá 100 ký tự.")
            .Matches(@"[A-Za-z]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ cái.")
            .Matches(@"\d").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ số.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu mới không khớp.");
    }
}
