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
/// Cấu hình xác thực Google OAuth (OpenID Connect ID Token).
/// </summary>
public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public List<string> AdditionalClientIds { get; set; } = new();

    public IEnumerable<string> GetConfiguredClientIds()
    {
        if (!string.IsNullOrWhiteSpace(ClientId))
            yield return ClientId.Trim();

        foreach (var id in AdditionalClientIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
                yield return id.Trim();
        }
    }
}

/// <summary>
/// Thông tin người dùng đã xác thực chữ ký từ Google ID Token.
/// </summary>
public class GoogleUserInfo
{
    public string Subject { get; init; } = null!;
    public string Email { get; init; } = null!;
    public bool EmailVerified { get; init; }
    public string? FullName { get; init; }
    public string? Picture { get; init; }
}

/// <summary>
/// Xác thực Google ID Token (chữ ký RS256, issuer, audience, hạn dùng).
/// </summary>
public interface IGoogleTokenVerifier
{
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
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

    /// <summary>
    /// Gửi mã OTP đặt lại mật khẩu.
    /// </summary>
    Task SendPasswordResetOtpAsync(string to, string fullName, string code, CancellationToken ct = default);
}

/// <summary>
/// Service xử lý đăng ký / đăng nhập / refresh / xác thực email / quên mật khẩu / Google OAuth.
/// </summary>
public interface IAuthService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> GoogleLoginAsync(GoogleLoginRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> SendVerificationCodeAsync(SendVerificationCodeRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> VerifyResetCodeAsync(VerifyResetCodeRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
}
