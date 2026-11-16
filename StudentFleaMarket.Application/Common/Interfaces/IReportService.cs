using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Trust;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Gửi & theo dõi report vi phạm (phía người dùng).
///
/// Xử lý report (phía nhân viên) nằm ở <see cref="IModerationService"/>.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gửi report vi phạm lên hệ thống.
    ///
    /// Ba dạng report (xem <c>CreateReportDto</c>):
    ///   1. Báo cáo bài đăng — cần ItemCode, KHÔNG cần giao dịch.
    ///   2. Báo cáo trong giao dịch — cần TransactionId (bắt buộc có đơn).
    ///   3. Báo cáo qua hội thoại — cần ConversationId.
    /// </summary>
    Task<MyReportDto> CreateAsync(Guid reporterId, CreateReportDto request, CancellationToken ct = default);

    /// <summary>
    /// Danh sách report do chính người dùng gửi (để theo dõi kết quả xử lý).
    /// </summary>
    Task<PagedResult<MyReportDto>> ListMineAsync(
        Guid reporterId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Chi tiết một report do chính người dùng gửi.
    /// </summary>
    Task<MyReportDto> GetMineAsync(Guid reporterId, Guid reportId, CancellationToken ct = default);
}
