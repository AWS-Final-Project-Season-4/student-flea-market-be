using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Domain.Entities;
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
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepo,
        IRepository<RefreshToken> refreshTokenRepo,
        IRepository<VerificationCode> verificationCodeRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _verificationCodeRepo = verificationCodeRepo;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    // ---------------------------------------------------------------- Register

    public async Task<RegisterResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = NormalizeUsername(request.Username);

        if (await _userRepo.ExistsAsync(u => u.Email == email, ct))
            throw new ConflictException("Email đã được đăng ký.", "EMAIL_EXISTS");

        // Username là định danh đăng nhập — phải unique toàn hệ thống.
        if (username is not null && await _userRepo.ExistsAsync(u => u.Username == username, ct))
            throw new ConflictException("Tên tài khoản đã tồn tại. Vui lòng chọn tên khác.", "USERNAME_EXISTS");

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
        await CreateAndSendVerificationCodeAsync(user, ct);

        // Không sinh token: người dùng phải xác thực email rồi mới đăng nhập được.
        return new RegisterResultDto
        {
            Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản trước khi đăng nhập.",
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

        if (user.Status == (int)UserStatus.Banned)
        {
            // Nêu rõ lý do cấm (nếu Admin đã ghi) để user biết phải làm gì.
            var reason = string.IsNullOrWhiteSpace(user.BanReason)
                ? string.Empty
                : $" Lý do: {user.BanReason}";

            throw new UnauthorizedException(
                $"Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.{reason}",
                "ACCOUNT_BANNED");
        }

        if (user.Status == (int)UserStatus.Inactive)
            throw new UnauthorizedException("Tài khoản chưa được kích hoạt.", "ACCOUNT_INACTIVE");

        // Tài khoản dịch vụ AI không đăng nhập qua email/password.
        if (user.Role == (int)UserRole.AiService)
            throw new ForbiddenException(
                "Tài khoản dịch vụ AI phải xác thực bằng API key (header X-Api-Key).",
                "SERVICE_ACCOUNT_REQUIRES_API_KEY");

        // Chưa xác thực email thì chưa được cấp token.
        if (!user.IsEmailVerified)
            throw new ForbiddenException(
                "Email chưa được xác thực. Vui lòng kiểm tra hộp thư và xác thực mã OTP trước khi đăng nhập.",
                "EMAIL_NOT_VERIFIED");

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
        if (user is null || user.IsEmailVerified)
            return ApiResponse<object>.Ok(new { Message = "Nếu email hợp lệ, mã xác thực đã được gửi." });

        await CreateAndSendVerificationCodeAsync(user, ct);
        return ApiResponse<object>.Ok(new { Message = "Mã xác thực đã được gửi tới email." });
    }

    public async Task<ApiResponse<object>> ResendVerificationCodeAsync(ResendVerificationCodeRequestDto request, CancellationToken ct = default)
    {
        return await SendVerificationCodeAsync(new SendVerificationCodeRequestDto { Email = request.Email }, ct);
    }

    public async Task<ApiResponse<object>> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var codeHash = HashHelper.Sha256(request.Code.Trim());

        var codes = await _verificationCodeRepo.ListAsync(c => c.Email == email && c.CodeHash == codeHash, ct);
        var code = codes.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

        if (code is null)
            throw new ValidationException("Mã xác thực không đúng.", "INVALID_CODE");

        if (code.IsUsed)
            throw new ValidationException("Mã xác thực đã được sử dụng.", "CODE_ALREADY_USED");

        if (code.IsExpired)
            throw new ValidationException("Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.", "CODE_EXPIRED");

        if (code.AttemptCount >= 5)
            throw new ValidationException("Bạn đã nhập sai mã quá nhiều lần. Vui lòng yêu cầu mã mới.", "TOO_MANY_ATTEMPTS");

        code.UsedAt = DateTime.UtcNow;
        _verificationCodeRepo.Update(code);

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

    // ------------------------------------------------------------------ Helpers

    private async Task CreateAndSendVerificationCodeAsync(ApplicationUser user, CancellationToken ct)
    {
        // Vô hiệu hóa các mã cũ chưa dùng
        var oldCodes = await _verificationCodeRepo.ListAsync(c => c.Email == user.Email && c.UsedAt == null, ct);

        foreach (var old in oldCodes)
        {
            old.UsedAt = DateTime.UtcNow;
            _verificationCodeRepo.Update(old);
        }

        var plainCode = Random.Shared.Next(100000, 999999).ToString();
        var verification = new VerificationCode
        {
            Id = Guid.NewGuid(),
            CodeHash = HashHelper.Sha256(plainCode),
            Email = user.Email,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await _verificationCodeRepo.AddAsync(verification, ct);
        await _verificationCodeRepo.SaveChangesAsync(ct);

        try
        {
            await _emailSender.SendOtpAsync(user.Email, user.FullName, plainCode, ct);
        }
        catch (Exception ex)
        {
            // Không để lỗi gửi email làm hỏng luồng đăng ký
            _logger.LogError(ex, "Không gửi được OTP tới {Email}", user.Email);
        }
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
    /// </summary>
    public static string RoleNameOf(int role) => role switch
    {
        (int)UserRole.Admin => "Admin",
        (int)UserRole.AiService => "AiService",
        _ => "User"
    };

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
