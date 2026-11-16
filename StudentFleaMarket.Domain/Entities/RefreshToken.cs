namespace StudentFleaMarket.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Token chỉ lưu dạng hash (SHA-256) để phòng trường hợp database bị lộ.
    /// </summary>
    public string TokenHash { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm token bị thu hồi (logout, đổi mật khẩu, refresh token cũ bị thay thế).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    /// <summary>
    /// Id của refresh token mới được sinh ra khi token này được dùng để refresh.
    /// Dùng để phát hiện trường hợp refresh token cũ bị dùng lại (token theft).
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Token còn hợp lệ hay không (chưa hết hạn và chưa bị thu hồi).
    /// </summary>
    public bool IsActive => !IsExpired && !IsRevoked;
}
