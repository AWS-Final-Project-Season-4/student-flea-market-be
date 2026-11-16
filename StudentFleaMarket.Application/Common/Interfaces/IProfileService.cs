using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Application.DTOs.Profile;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý hồ sơ sinh viên.
/// </summary>
public interface IProfileService
{
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken ct = default);
}
