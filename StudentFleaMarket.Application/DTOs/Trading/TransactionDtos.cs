using FluentValidation;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Trading;

// ============================================================ REQUEST (đầu vào)

/// <summary>
/// Khởi tạo đơn giao dịch mua trực tiếp (BuyDirect).
///
/// Người mua (Initiator) chốt mua một tin đăng của người bán (Counterparty).
/// Số tiền mặc định lấy theo giá bán của tin đăng; có thể ghi đè nếu
/// hai bên thỏa thuận giá khác (vd: thương lượng).
///
/// Người mua nhập MÃ HÀNG (ItemCode, 8 ký tự) của tin đăng — không nhập Id.
/// </summary>
public class CreatePurchaseTransactionDto
{
    /// <summary>Mã hàng của tin đăng muốn mua.</summary>
    public string ItemCode { get; set; } = null!;

    /// <summary>
    /// Địa điểm gặp mặt để giao dịch trực tiếp.
    /// Bỏ trống -> lấy luôn địa điểm ghi trong tin đăng.
    /// </summary>
    public string? MeetingLocation { get; set; }

    /// <summary>
    /// Phương thức thanh toán: 1 = Cod (tiền mặt khi gặp), 2 = BankTransfer (QR / cổng thanh toán).
    /// Mặc định 1 (Cod).
    /// </summary>
    public int PaymentMethod { get; set; } = (int)StudentFleaMarket.Domain.Enums.PaymentMethod.Cod;

    /// <summary>
    /// Số tiền thực trả (nếu hai bên thỏa thuận khác giá niêm yết).
    /// Bỏ trống -> dùng giá bán của tin đăng.
    /// </summary>
    public decimal? Amount { get; set; }
}

public class CreatePurchaseTransactionDtoValidator : AbstractValidator<CreatePurchaseTransactionDto>
{
    public CreatePurchaseTransactionDtoValidator()
    {
        RuleFor(x => x.ItemCode)
            .NotEmpty().WithMessage("Phải nhập mã hàng tin đăng muốn mua.")
            .Must(ItemCodeHelper.IsValidFormat)
            .WithMessage("Mã hàng phải gồm đúng 8 ký tự chữ cái và chữ số.");

        RuleFor(x => x.MeetingLocation)
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLocation));

        RuleFor(x => x.PaymentMethod)
            .Must(m => Enum.IsDefined(typeof(PaymentMethod), m))
            .WithMessage("Phương thức thanh toán không hợp lệ.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Số tiền không hợp lệ.")
            .When(x => x.Amount.HasValue);
    }
}

/// <summary>
/// Khởi tạo đơn giao dịch hoàn tất đổi đồ (Exchange) từ một yêu cầu đã được Accept.
///
/// Điểm điểm: Amount mặc định lấy AdditionalAmount của yêu cầu đổi đồ.
/// </summary>
public class CreateExchangeTransactionDto
{
    /// <summary>Id yêu cầu đổi đồ đã được chấp nhận.</summary>
    public Guid ExchangeRequestId { get; set; }

    /// <summary>
    /// Địa điểm gặp mặt để trao đổi đồ.
    /// Bỏ trống -> lấy địa điểm trong tin đăng của món đồ được nhận.
    /// </summary>
    public string? MeetingLocation { get; set; }

    /// <summary>
    /// Phương thức thanh toán tiền bù chênh lệch: 1 = Cod, 2 = BankTransfer.
    /// Mặc định 1 (Cod).
    /// </summary>
    public int PaymentMethod { get; set; } = (int)StudentFleaMarket.Domain.Enums.PaymentMethod.Cod;

    /// <summary>
    /// Số tiền bù chênh lệch thực trả (nếu thỏa thuận khác với yêu cầu đổi đồ).
    /// Bỏ trống -> dùng AdditionalAmount của yêu cầu.
    /// </summary>
    public decimal? Amount { get; set; }
}

public class CreateExchangeTransactionDtoValidator : AbstractValidator<CreateExchangeTransactionDto>
{
    public CreateExchangeTransactionDtoValidator()
    {
        RuleFor(x => x.ExchangeRequestId)
            .NotEmpty().WithMessage("Phải chọn yêu cầu đổi đồ.");

        RuleFor(x => x.MeetingLocation)
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLocation));

        RuleFor(x => x.PaymentMethod)
            .Must(m => Enum.IsDefined(typeof(PaymentMethod), m))
            .WithMessage("Phương thức thanh toán không hợp lệ.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Số tiền không hợp lệ.")
            .When(x => x.Amount.HasValue);
    }
}

/// <summary>
/// Hủy đơn giao dịch — kèm lý do (không bắt buộc).
/// </summary>
public class CancelTransactionDto
{
    /// <summary>Lý do hủy đơn (không bắt buộc).</summary>
    public string? Reason { get; set; }
}

public class CancelTransactionDtoValidator : AbstractValidator<CancelTransactionDto>
{
    public CancelTransactionDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do hủy không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}

/// <summary>
/// Xác nhận đã thanh toán (dùng cho BankTransfer / COD).
/// Người trả tiền báo "đã chuyển khoản" hoặc "đã trả tiền mặt" qua endpoint này.
/// </summary>
public class ConfirmPaymentDto
{
    /// <summary>
    /// Mã tham chiếu giao dịch do người trả nhập (mã chuyển khoản / nội dung chuyển khoản).
    /// Bỏ trống -> giữ mã hệ thống sinh sẵn.
    /// </summary>
    public string? TransactionRef { get; set; }
}

public class ConfirmPaymentDtoValidator : AbstractValidator<ConfirmPaymentDto>
{
    public ConfirmPaymentDtoValidator()
    {
        RuleFor(x => x.TransactionRef)
            .MaximumLength(150).WithMessage("Mã tham chiếu không được vượt quá 150 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.TransactionRef));
    }
}

// ============================================================ RESPONSE (đầu ra)

/// <summary>
/// Thông tin thanh toán của đơn giao dịch.
/// </summary>
public class PaymentDto
{
    public Guid Id { get; init; }

    /// <summary>PaymentMethod: 1 = Cod, 2 = BankTransfer.</summary>
    public int Method { get; init; }
    public string MethodName { get; init; } = null!;

    /// <summary>PaymentStatus: 1 = Pending, 2 = Paid, 3 = Failed, 4 = Refunded.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    public decimal Amount { get; init; }

    /// <summary>Mã tham chiếu để đối soát khi chuyển khoản.</summary>
    public string TransactionRef { get; init; } = null!;

    public Guid PayerId { get; init; }
    public string PayerName { get; init; } = null!;

    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Chi tiết một đơn giao dịch.
/// </summary>
public class TransactionDto
{
    public Guid Id { get; init; }

    /// <summary>TransactionType: 1 = Purchase, 2 = Exchange, 3 = Free.</summary>
    public int Type { get; init; }
    public string TypeName { get; init; } = null!;

    /// <summary>TransactionStatus: 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Completed.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    /// <summary>Người khởi tạo đơn (người mua / người gửi yêu cầu đổi).</summary>
    public TradingPartyDto Initiator { get; init; } = null!;

    /// <summary>Bên còn lại (người bán / người nhận yêu cầu đổi).</summary>
    public TradingPartyDto Counterparty { get; init; } = null!;

    /// <summary>Món đồ chính của giao dịch.</summary>
    public TradingItemDto Item { get; init; } = null!;

    /// <summary>Món đồ được đề nghị đổi lấy (chỉ với đơn loại Exchange).</summary>
    public TradingItemDto? OfferedItem { get; init; }

    /// <summary>Id yêu cầu đổi đồ gốc (chỉ với đơn loại Exchange).</summary>
    public Guid? ExchangeRequestId { get; init; }

    /// <summary>Tổng số tiền của đơn.</summary>
    public decimal Amount { get; init; }

    /// <summary>Địa điểm gặp mặt giao dịch.</summary>
    public string MeetingLocation { get; init; } = null!;

    /// <summary>Thông tin thanh toán (null nếu đơn giá 0 / không cần thanh toán).</summary>
    public PaymentDto? Payment { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }

    /// <summary>Lý do hủy giao dịch (nếu có).</summary>
    public string? CancelReason { get; init; }

    /// <summary>Người đang xem có quyền thao tác (complete / cancel) lên đơn này không.</summary>
    public bool CanManage { get; init; }
}

/// <summary>
/// Tham số lọc danh sách đơn giao dịch.
/// </summary>
public class TransactionQueryDto
{
    /// <summary>
    /// null = tất cả; TransactionStatus: 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Completed.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// TransactionType: 1 = Purchase, 2 = Exchange, 3 = Free.
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// "incoming" = đơn người khác mở với mình (mình là Counterparty).
    /// "outgoing" = đơn mình mở (mình là Initiator).
    /// null = cả hai.
    /// </summary>
    public string? Direction { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Nhãn tiếng Việt cho trạng thái / loại giao dịch.
/// </summary>
public static class TransactionLabels
{
    public static string StatusName(int status) => status switch
    {
        (int)TransactionStatus.Pending => "Đang xử lý",
        (int)TransactionStatus.Accepted => "Đã thỏa thuận",
        (int)TransactionStatus.Rejected => "Đã từ chối",
        (int)TransactionStatus.Cancelled => "Đã hủy giao dịch",
        (int)TransactionStatus.Completed => "Đã hoàn thành",
        _ => "Không xác định"
    };

    public static string TypeName(int type) => type switch
    {
        (int)TransactionType.Purchase => "Mua trực tiếp",
        (int)TransactionType.Exchange => "Đổi đồ",
        (int)TransactionType.Free => "Nhận miễn phí",
        _ => "Không xác định"
    };

    public static string PaymentMethodName(int method) => method switch
    {
        (int)PaymentMethod.Cod => "Tiền mặt khi gặp (COD)",
        (int)PaymentMethod.BankTransfer => "Chuyển khoản QR ngân hàng / cổng thanh toán",
        _ => "Không xác định"
    };

    public static string PaymentStatusName(int status) => status switch
    {
        (int)PaymentStatus.Pending => "Chờ thanh toán",
        (int)PaymentStatus.Paid => "Đã thanh toán",
        (int)PaymentStatus.Failed => "Thanh toán thất bại",
        (int)PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => "Không xác định"
    };
}
