using System.Security.Cryptography;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Quản lý tài khoản dịch vụ AI: tạo, thu hồi và xác thực bằng API key.
/// </summary>
public interface IServiceAccountService
{
    /// <summary>
    /// Tạo tài khoản dịch vụ mới. Trả về (serviceAccount, apiKeyPlaintext).
    /// API key thô chỉ hiển thị đúng 1 lần — DB chỉ lưu hash.
    /// </summary>
    Task<(ServiceAccount account, string apiKey)> CreateAsync(
        string name,
        Guid? createdByUserId,
        DateTime? expiresAt,
        CancellationToken ct = default);

    /// <summary>
    /// Xác thực API key. Trả null nếu key không hợp lệ / đã thu hồi / hết hạn.
    /// </summary>
    Task<ServiceAccount?> AuthenticateAsync(string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Thu hồi API key của một tài khoản dịch vụ.
    /// </summary>
    Task RevokeAsync(Guid serviceAccountId, string reason, CancellationToken ct = default);
}

public class ServiceAccountService : IServiceAccountService
{
    private const string KeyPrefixMarker = "sfm_";

    private readonly IServiceAccountRepository _repo;
    private readonly IRepository<ApplicationUser> _userRepo;

    public ServiceAccountService(IServiceAccountRepository repo, IRepository<ApplicationUser> userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    public async Task<(ServiceAccount account, string apiKey)> CreateAsync(
        string name,
        Guid? createdByUserId,
        DateTime? expiresAt,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Tên dịch vụ không được để trống.", "VALIDATION_ERROR");

        var trimmedName = name.Trim();

        if (await _repo.ExistsAsync(s => s.Name == trimmedName, ct))
            throw new ConflictException("Tên dịch vụ đã tồn tại.", "SERVICE_NAME_EXISTS");

        // API key: "sfm_" + 48 byte ngẫu nhiên base64url (~64 ký tự)
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        var apiKey = KeyPrefixMarker + raw;

        var account = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            ApiKeyHash = HashHelper.Sha256(apiKey),
            KeyPrefix = apiKey[..(KeyPrefixMarker.Length + 8)],
            Role = (int)UserRole.AiService,
            Status = (int)UserStatus.Active,
            CreatedByUserId = createdByUserId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(account, ct);
        await _repo.SaveChangesAsync(ct);

        return (account, apiKey);
    }

    public async Task<ServiceAccount?> AuthenticateAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var account = await _repo.GetByApiKeyHashAsync(HashHelper.Sha256(apiKey.Trim()), ct);

        if (account is null || !account.IsActive || account.Status != (int)UserStatus.Active)
            return null;

        // Ghi nhận lần dùng gần nhất (audit)
        account.LastUsedAt = DateTime.UtcNow;
        _repo.Update(account);
        await _repo.SaveChangesAsync(ct);

        return account;
    }

    public async Task RevokeAsync(Guid serviceAccountId, string reason, CancellationToken ct = default)
    {
        var account = await _repo.GetByIdAsync(serviceAccountId, ct);

        if (account is null)
            throw new NotFoundException("Không tìm thấy tài khoản dịch vụ.", "SERVICE_ACCOUNT_NOT_FOUND");

        if (account.IsRevoked)
            return;

        account.RevokedAt = DateTime.UtcNow;
        account.RevokedReason = reason;
        account.Status = (int)UserStatus.Inactive;

        _repo.Update(account);
        await _repo.SaveChangesAsync(ct);
    }
}
