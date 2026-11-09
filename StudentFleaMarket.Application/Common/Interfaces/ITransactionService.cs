using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Trading;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// Quản lý đơn giao dịch (Transaction) & thanh toán (Payment).
///
/// Luồng nghiệp vụ:
///   1. Khởi tạo đơn:
///        - CreatePurchaseAsync -> mua trực tiếp (Type = Purchase)
///        - CreateExchangeAsync -> hoàn tất đổi đồ (Type = Exchange), gắn với ExchangeRequest
///   2. Vòng đời đơn:
///        - Processing (Pending) -> Completed  (CompleteAsync)
///        - Processing (Pending) -> Cancelled (CancelAsync)
///   3. Thanh toán:
///        - COD: người mua xác nhận đã trả tiền mặt (ConfirmPaymentAsync)
///        - BankTransfer: người mua xác nhận đã chuyển khoản qua QR / cổng thanh toán
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Khởi tạo đơn giao dịch mua trực tiếp (BuyDirect).
    /// </summary>
    Task<TransactionDto> CreatePurchaseAsync(
        Guid initiatorId,
        CreatePurchaseTransactionDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Khởi tạo đơn giao dịch hoàn tất đổi đồ (Exchange) từ một yêu cầu đã Accepted.
    /// </summary>
    Task<TransactionDto> CreateExchangeAsync(
        Guid initiatorId,
        CreateExchangeTransactionDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết một đơn giao dịch (chỉ 2 bên liên quan được xem).
    /// </summary>
    Task<TransactionDto> GetByIdAsync(
        Guid transactionId,
        Guid viewerId,
        CancellationToken ct = default);

    /// <summary>
    /// Danh sách đơn giao dịch liên quan tới người dùng.
    /// </summary>
    Task<PagedResult<TransactionDto>> ListAsync(
        Guid userId,
        TransactionQueryDto query,
        CancellationToken ct = default);

    /// <summary>
    /// Hoàn thành đơn giao dịch — chuyển sang trạng thái Completed.
    /// Tin đăng tương ứng được đánh dấu "đã bán".
    /// </summary>
    Task<TransactionDto> CompleteAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Hủy đơn giao dịch — chuyển sang trạng thái Cancelled.
    /// Tin đăng được trả lại trạng thái "đang bán".
    /// </summary>
    Task<TransactionDto> CancelAsync(
        Guid userId,
        Guid transactionId,
        CancelTransactionDto? request,
        CancellationToken ct = default);

    /// <summary>
    /// Người trả tiền xác nhận đã thanh toán (COD hoặc chuyển khoản).
    /// </summary>
    Task<PaymentDto> ConfirmPaymentAsync(
        Guid userId,
        Guid transactionId,
        ConfirmPaymentDto? request,
        CancellationToken ct = default);

    /// <summary>
    /// Xác nhận bàn giao cho đơn ĐỔI ĐỒ (Exchange).
    ///
    /// Đơn Exchange bắt buộc CẢ 2 bên xác nhận thì mới hoàn thành:
    ///   - Bên thứ nhất xác nhận -> đơn sang trạng thái AwaitingConfirmation.
    ///   - Bên thứ hai xác nhận -> đơn hoàn thành, mở khoá đánh giá 2 chiều.
    ///
    /// Người dùng nhập mã hàng (8 ký tự) của món vừa nhận để hệ thống đối
    /// chiếu — phát hiện hành vi tráo hàng ngay tại chỗ, không cần Moderator.
    /// </summary>
    Task<HandoverConfirmationDto> ConfirmHandoverAsync(
        Guid userId,
        Guid transactionId,
        ConfirmHandoverDto? request,
        CancellationToken ct = default);

    /// <summary>
    /// Xem trạng thái xác nhận bàn giao của một đơn (bên nào đã xác nhận).
    /// </summary>
    Task<HandoverConfirmationDto> GetHandoverStatusAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default);
}
