using FluentValidation;
using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string FullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>
    /// Số điện thoại (bắt buộc).
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
}

internal static class AuthValidationRules
{
    public static IRuleBuilderOptions<T, string> ApplyStrongPasswordRules<T>(
        this IRuleBuilderInitial<T, string> rule,
        string label = "Password")
    {
        return rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage($"{label} cannot be left blank.")
            .MinimumLength(8).WithMessage($"{label} must be at least 8 characters long.")
            .MaximumLength(100).WithMessage($"{label} cannot exceed 100 characters.")
            .Matches(@"[A-Z]").WithMessage($"{label} must contain at least one uppercase letter (A-Z).")
            .Matches(@"[a-z]").WithMessage($"{label} must contain at least one lowercase letter (a-z).")
            .Matches(@"\d").WithMessage($"{label} must contain at least one digit (0-9).")
            .Matches(@"[\W_]").WithMessage($"{label} must contain at least one special character (!@#$%^&*...).");
    }
}

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name cannot be left blank.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username cannot be left blank.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(30).WithMessage("Username cannot exceed 30 characters.")
            .Matches(@"^[a-zA-Z0-9._]{3,30}$").WithMessage("Username must be 3-30 characters long and contain only letters, numbers, dots and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password).ApplyStrongPasswordRules("Password");

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

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = null!;
}

public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");
    }
}

public class VerifyResetCodeRequestDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class VerifyResetCodeRequestDtoValidator : AbstractValidator<VerifyResetCodeRequestDto>
{
    public VerifyResetCodeRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code cannot be left blank.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must be 6 digits.");
    }
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be left blank.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code cannot be left blank.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must be 6 digits.");

        RuleFor(x => x.NewPassword).ApplyStrongPasswordRules("New password");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Confirm password does not match.");
    }
}

public class GoogleLoginRequestDto
{
    /// <summary>
    /// Google OpenID Connect ID Token (JWT từ Google Identity Services).
    /// </summary>
    public string? IdToken { get; set; }

    /// <summary>
    /// Alias của IdToken (tương thích field "credential" mặc định của Google GIS).
    /// </summary>
    public string? Credential { get; set; }

    public string ResolvedIdToken =>
        !string.IsNullOrWhiteSpace(IdToken) ? IdToken!.Trim() : (Credential ?? string.Empty).Trim();
}

public class GoogleLoginRequestDtoValidator : AbstractValidator<GoogleLoginRequestDto>
{
    public GoogleLoginRequestDtoValidator()
    {
        RuleFor(x => x.ResolvedIdToken)
            .NotEmpty().WithMessage("Google ID token cannot be left blank.");
    }
}
