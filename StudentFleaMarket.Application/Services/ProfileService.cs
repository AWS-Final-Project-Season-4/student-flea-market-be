using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Application.DTOs.Profile;

namespace StudentFleaMarket.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepo;
    private readonly IRepository<Domain.Entities.RefreshToken> _refreshTokenRepo;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileService(
        IUserRepository userRepo,
        IRepository<Domain.Entities.RefreshToken> refreshTokenRepo,
        IPasswordHasher passwordHasher)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");

        return AuthService.MapUser(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");

        // Chỉ cập nhật các trường được gửi lên (null = không đổi)
        if (request.FullName is not null)
            user.FullName = request.FullName.Trim();

        if (request.PhoneNumber is not null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.AvatarUrl is not null)
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

        if (request.SchoolName is not null)
            user.SchoolName = string.IsNullOrWhiteSpace(request.SchoolName) ? null : request.SchoolName.Trim();

        if (request.StudentCode is not null)
            user.StudentCode = string.IsNullOrWhiteSpace(request.StudentCode) ? null : request.StudentCode.Trim();

        if (request.CommonArea is not null)
            user.CommonArea = string.IsNullOrWhiteSpace(request.CommonArea) ? null : request.CommonArea.Trim();

        if (request.Dormitory is not null)
            user.Dormitory = string.IsNullOrWhiteSpace(request.Dormitory) ? null : request.Dormitory.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        return AuthService.MapUser(user);
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Invalid current password.", "INVALID_CURRENT_PASSWORD");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);

        // Thu hồi toàn bộ refresh token: các phiên cũ buộc phải đăng nhập lại
        var tokens = await _refreshTokenRepo.ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct);
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = "PASSWORD_CHANGED";
            _refreshTokenRepo.Update(t);
        }

        await _userRepo.SaveChangesAsync(ct);
        await _refreshTokenRepo.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { Message = "Password changed successfully. Please log in again." });
    }
}
