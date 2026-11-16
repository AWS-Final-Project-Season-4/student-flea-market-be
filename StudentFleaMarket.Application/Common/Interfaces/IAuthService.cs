namespace StudentFleaMarket.Application.Common.Interfaces;

using StudentFleaMarket.Application.DTOs.Auth;

/// <summary>
/// Cấu hình sinh Access Token (JWT).
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>
/// Kết quả sinh token.
/// </summary>
public class TokenPair
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}

/// <summary>
/// Sinh Access Token + Refresh Token.
/// </summary>
public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(Guid userId, string email, string fullName, int role);
}

/// <summary>
/// Băm mật khẩu (PBKDF2 / BCrypt).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

/// <summary>
/// Gửi email (OTP / link kích hoạt).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default);

    /// <summary>
    /// Gửi mã OTP xác thực email.
    /// </summary>
    Task SendOtpAsync(string to, string fullName, string code, CancellationToken ct = default);
}

/// <summary>
/// Service xử lý đăng ký / đăng nhập / refresh / xác thực email.
/// </summary>
public interface IAuthService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> SendVerificationCodeAsync(SendVerificationCodeRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request, CancellationToken ct = default);
}
