using StudentFleaMarket.Application.DTOs.Trading;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Repository riêng cho ExchangeRequest (yêu cầu đổi đồ).
/// </summary>
public interface IExchangeRequestRepository : IRepository<ExchangeRequest>
{
    /// <summary>
    /// Lấy yêu cầu đổi đồ kèm quan hệ (Requester, RequestedItem, OfferedItem) đã load.
    /// </summary>
    Task<ExchangeRequest?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy yêu cầu đổi đồ dạng TRACKED (theo dõi thay đổi) kèm quan hệ đã load.
    /// Dùng khi cần sửa trạng thái (accept / reject / cancel) và lưu xuống DB.
    /// </summary>
    Task<ExchangeRequest?> GetTrackedDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Danh sách yêu cầu đổi đồ liên quan tới một người dùng, phân trang.
    /// </summary>
    /// <param name="direction">
    /// null = cả hai; "incoming" = người khác gửi cho user (user là chủ RequestedItem);
    /// "outgoing" = user gửi cho người khác (user là Requester).
    /// </param>
    Task<(List<ExchangeRequest> Requests, int TotalCount)> ListForUserAsync(
        Guid userId,
        ExchangeRequestQueryDto query,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra đã tồn tại yêu cầu đổi đồ "đang chờ" giữa 2 món đồ chưa.
    /// Dùng để chặn spam nhiều yêu cầu trùng nhau.
    /// </summary>
    Task<bool> ExistsPendingAsync(
        Guid requestedItemId,
        Guid offeredItemId,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra món đồ đã có yêu cầu đổi đồ "đang chờ" nào chưa (để tránh
    /// 2 người cùng đề nghị đổi lấy 1 món đồ).
    /// </summary>
    Task<bool> HasPendingRequestForItemAsync(Guid itemId, CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho Transaction (đơn giao dịch).
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Lấy đơn giao dịch kèm quan hệ (Initiator, Counterparty, Item, Payment) đã load.
    /// </summary>
    Task<Transaction?> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy đơn giao dịch dạng TRACKED (theo dõi thay đổi) kèm quan hệ đã load.
    /// Dùng khi cần sửa trạng thái (complete / cancel / xác nhận thanh toán).
    /// </summary>
    Task<Transaction?> GetTrackedDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Danh sách đơn giao dịch liên quan tới một người dùng, phân trang.
    /// </summary>
    /// <param name="direction">
    /// null = cả hai; "incoming" = user là Counterparty; "outgoing" = user là Initiator.
    /// </param>
    Task<(List<Transaction> Transactions, int TotalCount)> ListForUserAsync(
        Guid userId,
        TransactionQueryDto query,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra món đồ đã có đơn giao dịch "đang xử lý" nào chưa — tránh
    /// 2 người cùng mua 1 món đồ.
    /// </summary>
    Task<bool> HasPendingTransactionForItemAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Tìm đơn giao dịch đã tạo từ một yêu cầu đổi đồ (để tránh tạo trùng).
    /// </summary>
    Task<Transaction?> GetByExchangeRequestAsync(Guid exchangeRequestId, CancellationToken ct = default);

    /// <summary>
    /// Đếm số giao dịch ĐÃ HOÀN THÀNH của một tài khoản, tách 2 vai trò:
    ///   - SalesCount     : user là bên BÁN (Counterparty).
    ///   - PurchaseCount  : user là bên MUA (Initiator).
    ///
    /// Đây là TIÊU CHÍ CHÍNH để xếp mức uy tín — xem <c>TrustPolicy</c>.
    ///
    /// Chỉ tính đơn có trạng thái Completed (đơn Pending / Cancelled không tính).
    /// </summary>
    Task<(int SalesCount, int PurchaseCount)> CountCompletedForUserAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy các đơn đã HOÀN THÀNH mà người dùng này là 1 trong 2 bên,
    /// kèm đánh giá đã viết (nếu có) — dùng cho màn hình "Cần đánh giá".
    /// </summary>
    Task<List<Transaction>> ListCompletedWithReviewsAsync(
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho TransactionConfirmation (xác nhận bàn giao đơn đổi đồ).
/// </summary>
public interface ITransactionConfirmationRepository : IRepository<TransactionConfirmation>
{
    /// <summary>
    /// Lấy các xác nhận của một đơn giao dịch (kèm User đã load).
    /// </summary>
    Task<List<TransactionConfirmation>> GetByTransactionAsync(
        Guid transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy xác nhận của một người cho một đơn (null nếu chưa xác nhận).
    /// </summary>
    Task<TransactionConfirmation?> GetByTransactionAndUserAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho Payment (thanh toán).
/// </summary>
public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>
    /// Lấy thông tin thanh toán của một đơn giao dịch.
    /// </summary>
    Task<Payment?> GetByTransactionAsync(Guid transactionId, CancellationToken ct = default);
}
