using FluentValidation;
using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>
    /// Số điện thoại (không bắt buộc).
    /// </summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// Tên trường học (không bắt buộc).
    /// </summary>
    public string? SchoolName { get; set; }

    /// <summary>
    /// Mã sinh viên (không bắt buộc).
    /// </summary>
    public string? StudentCode { get; set; }

    /// <summary>
    /// Tên tài khoản để đăng nhập (không bắt buộc).
    /// Nếu để trống, tài khoản này chỉ đăng nhập được bằng email.
    /// Nếu có giá trị, đăng nhập được bằng CẢ username lẫn email.
    /// </summary>
    public string? Username { get; set; }
}

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name cannot be left blank.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password cannot be left blank.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            .Matches(@"[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Confirm password does not match.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number cannot be left blank.")
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Invalid phone number.");

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).WithMessage("School name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchoolName));

        RuleFor(x => x.StudentCode)
            .MaximumLength(50).WithMessage("Student code cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode));

        RuleFor(x => x.Username)
            .Matches(@"^[a-zA-Z0-9._]{3,30}$").WithMessage("Username must be 3-30 characters long and contain only letters, numbers, dots and underscores.")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));
    }
}

public class LoginRequestDto
{
    /// <summary>
    /// Định danh đăng nhập: username ("admin") HOẶC email ("admin@sv.edu.vn").
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Alias của <see cref="Identifier"/> — dùng để tương thích client cũ gửi "email".
    /// </summary>
    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    /// <summary>
    /// Giá trị định danh đã chuẩn hoá (ưu tiên Identifier, fallback Email).
    /// </summary>
    public string ResolvedIdentifier =>
        !string.IsNullOrWhiteSpace(Identifier) ? Identifier!.Trim() : (Email ?? string.Empty).Trim();
}

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.ResolvedIdentifier)
            .NotEmpty().WithMessage("Email or account name cannot be left blank.")
            .MaximumLength(256).WithMessage("Account name cannot exceed 256 characters.")
            .Must(x => x.Contains('@') || System.Text.RegularExpressions.Regex.IsMatch(x, @"^[a-zA-Z0-9._]{3,30}$"))
            .WithMessage("Invalid account name.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password cannot be left blank.");
    }
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = null!;
}

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token cannot be left blank.");
    }
}

public class SendVerificationCodeRequestDto
{
    public string Email { get; set; } = null!;
}

public class SendVerificationCodeRequestDtoValidator : AbstractValidator<SendVerificationCodeRequestDto>
{
    public SendVerificationCodeRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.");
    }
}

public class VerifyEmailRequestDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class VerifyEmailRequestDtoValidator : AbstractValidator<VerifyEmailRequestDto>
{
    public VerifyEmailRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code cannot be left blank.")
            .Length(6).WithMessage("Verification code must be 6 digits.");
    }
}

public class ResendVerificationCodeRequestDto
{
    public string Email { get; set; } = null!;
}

public class ResendVerificationCodeRequestDtoValidator : AbstractValidator<ResendVerificationCodeRequestDto>
{
    public ResendVerificationCodeRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.");
    }
}
