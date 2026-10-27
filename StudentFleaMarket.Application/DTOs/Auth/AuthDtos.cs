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
    public string? PhoneNumber { get; set; }

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
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MinimumLength(2).WithMessage("Họ tên phải có ít nhất 2 ký tự.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự.")
            .Matches(@"[A-Za-z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ cái.")
            .Matches(@"\d").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Số điện thoại không hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).WithMessage("Tên trường không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchoolName));

        RuleFor(x => x.StudentCode)
            .MaximumLength(50).WithMessage("Mã sinh viên không được vượt quá 50 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode));

        RuleFor(x => x.Username)
            .Matches(@"^[a-zA-Z0-9._]{3,30}$").WithMessage("Tên tài khoản phải gồm 3-30 ký tự, chỉ gồm chữ, số, dấu chấm và gạch dưới.")
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
            .NotEmpty().WithMessage("Email hoặc tên tài khoản không được để trống.")
            .MaximumLength(256).WithMessage("Định danh đăng nhập không được vượt quá 256 ký tự.")
            .Must(x => x.Contains('@') || System.Text.RegularExpressions.Regex.IsMatch(x, @"^[a-zA-Z0-9._]{3,30}$"))
            .WithMessage("Định danh đăng nhập không hợp lệ.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
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
            .NotEmpty().WithMessage("Refresh token không được để trống.");
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
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");
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
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã xác thực không được để trống.")
            .Length(6).WithMessage("Mã xác thực phải gồm 6 chữ số.");
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
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");
    }
}
