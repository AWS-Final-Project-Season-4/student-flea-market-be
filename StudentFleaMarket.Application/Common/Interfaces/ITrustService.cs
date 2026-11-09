using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Tính toán & truy vấn tín hiệu uy tín của tài khoản.
///
/// Dùng cho: profile người bán (GET /api/Sellers/{id}/profile), hiển thị badge
/// uy tín cạnh tên người bán trong danh sách tin đăng, và bảng kiểm duyệt.
///
/// NGƯỠNG XẾP MỨC nằm ở <c>TrustPolicy</c> — sửa ở đó, không sửa ở đây.
/// </summary>
public interface ITrustService
{
    /// <summary>
    /// Tính toàn bộ tín hiệu uy tín của một tài khoản.
    /// </summary>
    Task<TrustMetricsDto> GetMetricsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy tín hiệu uy tín cho NHIỀU tài khoản cùng lúc — dùng khi hiển thị
    /// danh sách tin đăng (tránh N+1).
    /// </summary>
    Task<Dictionary<Guid, TrustMetricsDto>> GetMetricsBatchAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default);
}
