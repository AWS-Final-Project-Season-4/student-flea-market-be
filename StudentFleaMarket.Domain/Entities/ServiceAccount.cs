namespace StudentFleaMarket.Domain.Entities;

/// <summary>
/// Tài khoản dịch vụ (AI Service) — kiểm duyệt nội dung, gợi ý sản phẩm, hỗ trợ hội thoại...
/// Không đăng nhập bằng email/password; xác thực bằng API key trong header X-Api-Key.
/// </summary>
public class ServiceAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tên hiển thị của dịch vụ (vd: "ai-moderation").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// API key chỉ lưu dạng hash (SHA-256). Bản thô chỉ hiển thị 1 lần lúc tạo.
    /// </summary>
    public string ApiKeyHash { get; set; } = null!;

    /// <summary>
    /// Tiền tố của API key, dùng để tra cứu nhanh (vd: "sk_live_a1b2...").
    /// </summary>
    public string KeyPrefix { get; set; } = null!;

    /// <summary>
    /// Vai trò của tài khoản dịch vụ — luôn là AiService.
    /// </summary>
    public int Role { get; set; } = (int)Enums.UserRole.AiService;

    /// <summary>
    /// 1: Active, 2: Inactive, 3: Banned
    /// </summary>
    public int Status { get; set; } = (int)Enums.UserStatus.Active;

    /// <summary>
    /// Người tạo (quản trị viên).
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Lần cuối được sử dụng — phục vụ audit & phát hiện key không còn dùng.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    public bool IsExpired => ExpiresAt is not null && DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;
}
