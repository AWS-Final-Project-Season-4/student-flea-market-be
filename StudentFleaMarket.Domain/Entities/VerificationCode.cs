namespace StudentFleaMarket.Domain.Entities;

public class VerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Mã OTP dạng 6 chữ số (chỉ lưu hash SHA-256).
    /// </summary>
    public string CodeHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    /// <summary>
    /// null nghĩa là code thuộc về user đã đăng ký; có giá trị khi đăng ký email lạ chưa có user.
    /// </summary>
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; } = 0;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt is not null;
    public bool IsActive => !IsExpired && !IsUsed;
}
