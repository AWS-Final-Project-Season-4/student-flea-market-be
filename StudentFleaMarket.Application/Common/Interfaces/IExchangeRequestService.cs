using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Trading;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý yêu cầu đổi đồ (Exchange Request).
///
/// Luồng nghiệp vụ:
///   1. Người dùng gửi yêu cầu  -> CreateAsync     (RequesterId = chủ OfferedItem)
///   2. Chủ RequestedItem phản hồi:
///        - Accept -> AcceptAsync   (Status = Accepted, có thể tạo đơn Exchange)
///        - Reject -> RejectAsync   (Status = Rejected)
///   3. Người gửi hủy yêu cầu    -> CancelAsync    (Status = Cancelled)
/// </summary>
public interface IExchangeRequestService
{
    /// <summary>
    /// Gửi yêu cầu đổi đồ.
    /// </summary>
    Task<ExchangeRequestDto> CreateAsync(
        Guid requesterId,
        CreateExchangeRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết một yêu cầu đổi đồ (chỉ 2 bên liên quan được xem).
    /// </summary>
    Task<ExchangeRequestDto> GetByIdAsync(
        Guid exchangeRequestId,
        Guid viewerId,
        CancellationToken ct = default);

    /// <summary>
    /// Danh sách yêu cầu đổi đồ liên quan tới người dùng (nhận được hoặc đã gửi).
    /// </summary>
    Task<PagedResult<ExchangeRequestDto>> ListAsync(
        Guid userId,
        ExchangeRequestQueryDto query,
        CancellationToken ct = default);

    /// <summary>
    /// Chủ món đồ được nhận chấp nhận yêu cầu đổi đồ.
    /// </summary>
    Task<ExchangeRequestDto> AcceptAsync(
        Guid userId,
        Guid exchangeRequestId,
        RespondExchangeRequestDto? request,
        CancellationToken ct = default);

    /// <summary>
    /// Chủ món đồ được nhận từ chối yêu cầu đổi đồ.
    /// </summary>
    Task<ExchangeRequestDto> RejectAsync(
        Guid userId,
        Guid exchangeRequestId,
        RespondExchangeRequestDto? request,
        CancellationToken ct = default);

    /// <summary>
    /// Người gửi hủy yêu cầu đổi đồ đang chờ phản hồi.
    /// </summary>
    Task<ExchangeRequestDto> CancelAsync(
        Guid userId,
        Guid exchangeRequestId,
        CancellationToken ct = default);
}
