using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Common;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;
    private readonly IRepository<VerificationCode> _verificationCodeRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepo,
        IRepository<RefreshToken> refreshTokenRepo,
        IRepository<VerificationCode> verificationCodeRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IGoogleTokenVerifier googleTokenVerifier,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _verificationCodeRepo = verificationCodeRepo;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _googleTokenVerifier = googleTokenVerifier;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    // ---------------------------------------------------------------- Register

    public async Task<RegisterResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = NormalizeUsername(request.Username);

        if (await _userRepo.ExistsAsync(u => u.Email == email, ct))
            throw new ConflictException("Email already exists.", "EMAIL_EXISTS");

        // Username là định danh đăng nhập — phải unique toàn hệ thống.
        if (username is not null && await _userRepo.ExistsAsync(u => u.Username == username, ct))
            throw new ConflictException("Username already exists. Please choose another name.", "USERNAME_EXISTS");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            SchoolName = request.SchoolName?.Trim(),
            StudentCode = request.StudentCode?.Trim(),
            Role = (int)UserRole.User,
            Status = (int)UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveChangesAsync(ct);

        // Sinh & gửi OTP xác thực email ngay sau khi đăng ký
        await CreateAndSendVerificationCodeAsync(user, VerificationCodePurpose.EmailVerification, ct);

        // Không sinh token: người dùng phải xác thực email rồi mới đăng nhập được.
        return new RegisterResultDto
        {
            Message = "Registration successful. Please check your email to verify your account before logging in.",
            User = MapUser(user),
            VerificationEmail = user.Email
        };
    }

    // ------------------------------------------------------------------- Login

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        // Định danh đăng nhập có thể là username ("admin") hoặc email ("admin@sv.edu.vn").
        var user = await _userRepo.GetByUsernameOrEmailAsync(request.ResolvedIdentifier, ct);

        // Thông báo chung để không lộ email nào tồn tại
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email, tên tài khoản hoặc mật khẩu không đúng.", "INVALID_CREDENTIALS");

        EnsureUserCanSignIn(user);

        // Chưa xác thực email thì chưa được cấp token.
        if (!user.IsEmailVerified)
            throw new ForbiddenException(
                "Email chưa được xác thực. Vui lòng kiểm tra hộp thư và xác thực mã OTP trước khi đăng nhập.",
                "EMAIL_NOT_VERIFIED");

        return await BuildAuthResultAsync(user, ct);
    }

    // ------------------------------------------------------------ Google OAuth

    public async Task<AuthResultDto> GoogleLoginAsync(GoogleLoginRequestDto request, CancellationToken ct = default)
    {
        var googleUser = await _googleTokenVerifier.VerifyIdTokenAsync(request.ResolvedIdToken, ct);

        if (!googleUser.EmailVerified)
            throw new ForbiddenException("Google account email is not verified.", "GOOGLE_EMAIL_NOT_VERIFIED");

        var email = googleUser.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByGoogleIdAsync(googleUser.Subject, ct)
            ?? await _userRepo.GetByEmailAsync(email, ct);

        if (user is not null)
        {
            EnsureUserCanSignIn(user);

            if (!string.IsNullOrEmpty(user.GoogleId)
                && !string.Equals(user.GoogleId, googleUser.Subject, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    "This email is already linked to another Google account.",
                    "GOOGLE_ACCOUNT_MISMATCH");
            }

            var updated = false;
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleUser.Subject;
                updated = true;
            }

            if (!user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(googleUser.Picture))
            {
                user.AvatarUrl = Truncate(googleUser.Picture.Trim(), 500);
                updated = true;
            }

            if (updated)
            {
                user.UpdatedAt = DateTime.UtcNow;
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync(ct);
            }
        }
        else
        {
            var fallbackName = email.Split('@')[0];
            var displayName = string.IsNullOrWhiteSpace(googleUser.FullName)
                ? fallbackName
                : googleUser.FullName.Trim();

            var randomSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = Truncate(displayName, 150) ?? fallbackName,
                Email = email,
                Username = null,
                PasswordHash = _passwordHasher.Hash(randomSecret),
                AvatarUrl = Truncate(googleUser.Picture?.Trim(), 500),
                GoogleId = googleUser.Subject,
                Role = (int)UserRole.User,
                Status = (int)UserStatus.Active,
                SellerStatus = (int)SellerStatus.None,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user, ct);
            await _userRepo.SaveChangesAsync(ct);
        }

        return await BuildAuthResultAsync(user, ct);
    }

    // ------------------------------------------------------------ Refresh token

    public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.Sha256(request.RefreshToken);

        var stored = await _refreshTokenRepo.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (stored is null)
            throw new UnauthorizedException("Refresh token không hợp lệ.", "INVALID_REFRESH_TOKEN");

        if (stored.IsRevoked)
        {
            // Token cũ bị dùng lại sau khi đã thu hồi => có khả năng token bị lộ.
            // Thu hồi toàn bộ session của user cho an toàn.
            await RevokeAllUserTokensAsync(stored.UserId, "REUSE_DETECTED", ct);
            throw new UnauthorizedException("Refresh token đã bị thu hồi. Vui lòng đăng nhập lại.", "REFRESH_TOKEN_REVOKED");
        }

        if (stored.IsExpired)
            throw new UnauthorizedException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.", "REFRESH_TOKEN_EXPIRED");

        var user = await _userRepo.GetByIdAsync(stored.UserId, ct);
        if (user is null)
            throw new UnauthorizedException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (user.Status != (int)UserStatus.Active)
            throw new UnauthorizedException("Tài khoản không hoạt động.", "ACCOUNT_INACTIVE");

        // Rotate token: thu hồi token cũ, sinh token mới
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = "ROTATED";
        _refreshTokenRepo.Update(stored);

        var result = await BuildAuthResultAsync(user, ct);

        // Liên kết token mới với token cũ
        var newToken = await _refreshTokenRepo.FirstOrDefaultAsync(
            t => t.TokenHash == HashHelper.Sha256(result.RefreshToken), ct);

        if (newToken is not null)
        {
            stored.ReplacedByTokenId = newToken.Id;
            _refreshTokenRepo.Update(stored);
            await _refreshTokenRepo.SaveChangesAsync(ct);
        }

        return result;
    }

    // ------------------------------------------------------------------ Logout

    public async Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.Sha256(request.RefreshToken);
        var stored = await _refreshTokenRepo.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (stored is null)
            throw new UnauthorizedException("Refresh token không hợp lệ.", "INVALID_REFRESH_TOKEN");

        if (!stored.IsRevoked)
        {
            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedReason = "LOGOUT";
            _refreshTokenRepo.Update(stored);
            await _refreshTokenRepo.SaveChangesAsync(ct);
        }
    }

    // ------------------------------------------------------- Email verification

    public async Task<ApiResponse<object>> SendVerificationCodeAsync(SendVerificationCodeRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        // Không lộ thông tin email có tồn tại hay không
        if (user is null || user.IsEmailVerified || user.Role == (int)UserRole.AiService)
            return ApiResponse<object>.Ok(new { Message = "Nếu email hợp lệ, mã xác thực đã được gửi." });

        await CreateAndSendVerificationCodeAsync(user, VerificationCodePurpose.EmailVerification, ct);
        return ApiResponse<object>.Ok(new { Message = "Mã xác thực đã được gửi tới email." });
    }

    public async Task<ApiResponse<object>> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request, CancellationToken ct = default)
    {
        return await SendVerificationCodeAsync(new SendVerificationCodeRequestDto { Email = request.Email }, ct);
    }

    public async Task<ApiResponse<object>> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default)
    {
        var code = await ValidateVerificationCodeAsync(
            request.Email,
            request.Code,
            VerificationCodePurpose.EmailVerification,
            consume: true,
            ct);

        if (code.UserId is Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId, ct);
            if (user is not null && !user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                _userRepo.Update(user);
            }
        }

        await _verificationCodeRepo.SaveChangesAsync(ct);
        await _userRepo.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { Message = "Xác thực email thành công." });
    }

    // ---------------------------------------------------------- Forgot password

    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        // Không tiết lộ email có tồn tại hoặc trạng thái tài khoản
        if (user is not null
            && user.Role != (int)UserRole.AiService
            && user.Status == (int)UserStatus.Active)
        {
            await CreateAndSendVerificationCodeAsync(user, VerificationCodePurpose.PasswordReset, ct);
        }

        const string msg = "If the email is registered, a 6-digit password reset code has been sent.";
        return ApiResponse<object>.Ok(new { Message = msg }, msg);
    }

    public async Task<ApiResponse<object>> VerifyResetCodeAsync(VerifyResetCodeRequestDto request, CancellationToken ct = default)
    {
        await ValidateVerificationCodeAsync(
            request.Email,
            request.Code,
            VerificationCodePurpose.PasswordReset,
            consume: false,
            ct);

        const string msg = "Verification code is valid.";
        return ApiResponse<object>.Ok(new { Message = msg, Valid = true }, msg);
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var code = await ValidateVerificationCodeAsync(
            email,
            request.Code,
            VerificationCodePurpose.PasswordReset,
            consume: true,
            ct);

        var user = (code.UserId is Guid userId
                ? await _userRepo.GetByIdAsync(userId, ct)
                : await _userRepo.GetByEmailAsync(email, ct))
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");

        EnsureUserCanSignIn(user);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        if (!user.IsEmailVerified)
            user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);

        await _verificationCodeRepo.SaveChangesAsync(ct);
        await _userRepo.SaveChangesAsync(ct);

        // Thu hồi toàn bộ refresh token cũ để buộc đăng nhập lại bằng mật khẩu mới
        await RevokeAllUserTokensAsync(user.Id, "PASSWORD_RESET", ct);

        const string msg = "Password has been reset successfully. Please log in with your new password.";
        return ApiResponse<object>.Ok(new { Message = msg }, msg);
    }

    // ------------------------------------------------------------------ Helpers

    private static void EnsureUserCanSignIn(ApplicationUser user)
    {
        if (user.Status == (int)UserStatus.Banned)
        {
            var reason = string.IsNullOrWhiteSpace(user.BanReason)
                ? string.Empty
                : $" Lý do: {user.BanReason}";

            throw new UnauthorizedException(
                $"Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.{reason}",
                "ACCOUNT_BANNED");
        }

        if (user.Status == (int)UserStatus.Inactive)
            throw new UnauthorizedException("Tài khoản chưa được kích hoạt.", "ACCOUNT_INACTIVE");

        if (user.Role == (int)UserRole.AiService)
            throw new ForbiddenException(
                "Tài khoản dịch vụ AI phải xác thực bằng API key (header X-Api-Key).",
                "SERVICE_ACCOUNT_REQUIRES_API_KEY");
    }

    private async Task<VerificationCode> ValidateVerificationCodeAsync(
        string email,
        string plainCode,
        VerificationCodePurpose purpose,
        bool consume,
        CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var codeHash = HashHelper.Sha256(plainCode.Trim());

        var codes = await _verificationCodeRepo.ListAsync(
            c => c.Email == normalizedEmail && c.Purpose == (int)purpose, ct);

        var latest = codes.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

        if (latest is null)
            throw new ValidationException("Mã xác thực không đúng.", "INVALID_CODE");

        if (latest.IsUsed)
            throw new ValidationException("Mã xác thực đã được sử dụng.", "CODE_ALREADY_USED");

        if (latest.IsExpired)
            throw new ValidationException("Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.", "CODE_EXPIRED");

        if (latest.AttemptCount >= 5)
            throw new ValidationException("Bạn đã nhập sai mã quá nhiều lần. Vui lòng yêu cầu mã mới.", "TOO_MANY_ATTEMPTS");

        if (!string.Equals(latest.CodeHash, codeHash, StringComparison.Ordinal))
        {
            latest.AttemptCount += 1;
            _verificationCodeRepo.Update(latest);
            await _verificationCodeRepo.SaveChangesAsync(ct);

            if (latest.AttemptCount >= 5)
                throw new ValidationException("Bạn đã nhập sai mã quá nhiều lần. Vui lòng yêu cầu mã mới.", "TOO_MANY_ATTEMPTS");

            throw new ValidationException("Mã xác thực không đúng.", "INVALID_CODE");
        }

        if (consume)
        {
            latest.UsedAt = DateTime.UtcNow;
            _verificationCodeRepo.Update(latest);
        }

        return latest;
    }

    private async Task CreateAndSendVerificationCodeAsync(
        ApplicationUser user,
        VerificationCodePurpose purpose,
        CancellationToken ct)
    {
        // Vô hiệu hóa các mã cũ cùng mục đích chưa dùng
        var oldCodes = await _verificationCodeRepo.ListAsync(
            c => c.Email == user.Email && c.Purpose == (int)purpose && c.UsedAt == null, ct);

        foreach (var old in oldCodes)
        {
            old.UsedAt = DateTime.UtcNow;
            _verificationCodeRepo.Update(old);
        }

        var plainCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var verification = new VerificationCode
        {
            Id = Guid.NewGuid(),
            CodeHash = HashHelper.Sha256(plainCode),
            Email = user.Email,
            UserId = user.Id,
            Purpose = (int)purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await _verificationCodeRepo.AddAsync(verification, ct);
        await _verificationCodeRepo.SaveChangesAsync(ct);

        try
        {
            if (purpose == VerificationCodePurpose.PasswordReset)
                await _emailSender.SendPasswordResetOtpAsync(user.Email, user.FullName, plainCode, ct);
            else
                await _emailSender.SendOtpAsync(user.Email, user.FullName, plainCode, ct);
        }
        catch (Exception ex)
        {
            // Không để lỗi gửi email làm hỏng luồng xác thực
            _logger.LogError(ex, "Không gửi được OTP ({Purpose}) tới {Email}", purpose, user.Email);
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private async Task<AuthResultDto> BuildAuthResultAsync(ApplicationUser user, CancellationToken ct)
    {
        var tokenPair = _jwtTokenService.GenerateTokenPair(user.Id, user.Email, user.FullName, user.Role);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = HashHelper.Sha256(tokenPair.RefreshToken),
            UserId = user.Id,
            ExpiresAt = tokenPair.RefreshTokenExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepo.AddAsync(refreshToken, ct);
        await _refreshTokenRepo.SaveChangesAsync(ct);

        return new AuthResultDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            TokenType = "Bearer",
            AccessTokenExpiresAt = tokenPair.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = tokenPair.RefreshTokenExpiresAt,
            User = MapUser(user)
        };
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken ct)
    {
        var tokens = await _refreshTokenRepo.ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct);

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = reason;
            _refreshTokenRepo.Update(t);
        }

        if (tokens.Count > 0)
            await _refreshTokenRepo.SaveChangesAsync(ct);
    }

    public static UserDto MapUser(ApplicationUser user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Username = user.Username,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        SchoolName = user.SchoolName,
        StudentCode = user.StudentCode,
        CommonArea = user.CommonArea,
        Dormitory = user.Dormitory,
        Role = user.Role,
        RoleName = RoleNameOf(user.Role),
        IsAdmin = user.Role == (int)UserRole.Admin,
        IsModerator = user.Role == (int)UserRole.Moderator,
        IsStaff = StaffPermission.IsStaff(user),
        Status = user.Status,
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        // ----- Seller (năng lực bán hàng do Admin cấp quyền) -----
        SellerStatus = user.SellerStatus,
        SellerStatusName = SellerService.StatusNameOf(user.SellerStatus),
        CanSell = SellerPermission.CanSell(user),
        // ----- Ban (Admin khoá tài khoản) -----
        BanReason = user.BanReason,
        IsBanned = user.Status == (int)UserStatus.Banned
    };

    /// <summary>
    /// Tên vai trò tương ứng với mã số.
    /// Nguồn sự thật duy nhất: <see cref="RoleNames"/> (Domain).
    /// </summary>
    public static string RoleNameOf(int role) => RoleNames.Of(role);

    /// <summary>
    /// Chuẩn hoá username: trim + lowercase. Trả null nếu không được nhập.
    /// </summary>
    private static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        return username.Trim().ToLowerInvariant();
    }
}
