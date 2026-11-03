namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Phương thức thanh toán cho một đơn giao dịch (Transaction).
///
/// 1 = Cod       -> Trả tiền mặt khi gặp nhận đồ (mặc định của sàn sinh viên).
/// 2 = BankTransfer -> Chuyển khoản qua QR ngân hàng / cổng thanh toán.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Trả tiền mặt khi gặp nhận đồ.</summary>
    Cod = 1,

    /// <summary>Chuyển khoản qua QR ngân hàng / cổng thanh toán.</summary>
    BankTransfer = 2
}
