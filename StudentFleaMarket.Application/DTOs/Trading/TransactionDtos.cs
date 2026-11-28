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

    /// <summary>Vĩ độ địa điểm giao nhận.</summary>
    public double? MeetingLatitude { get; set; }

    /// <summary>Kinh độ địa điểm giao nhận.</summary>
    public double? MeetingLongitude { get; set; }

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

    /// <summary>Số lượng mua (mặc định 1).</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Hình thức giao nhận: 1 = DirectMeetup (gặp trực tiếp / tự giao), 2 = ExternalShipper (đặt ship ngoài).
    /// Sàn không thu hay hạch toán phí ship bên ngoài.
    /// </summary>
    public int DeliveryMethod { get; set; } = (int)StudentFleaMarket.Domain.Enums.DeliveryMethod.DirectMeetup;
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

        RuleFor(x => x.MeetingLatitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -90d && v.Value <= 90d))
            .WithMessage("Vĩ độ không hợp lệ.");

        RuleFor(x => x.MeetingLongitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -180d && v.Value <= 180d))
            .WithMessage("Kinh độ không hợp lệ.");

        RuleFor(x => x)
            .Must(x => x.MeetingLatitude.HasValue == x.MeetingLongitude.HasValue)
            .WithMessage("Phải cung cấp đầy đủ vĩ độ và kinh độ.");

        RuleFor(x => x.MeetingLocation)
            .NotEmpty()
            .WithMessage("Phải nhập địa chỉ khi cung cấp tọa độ.")
            .When(x => x.MeetingLatitude.HasValue || x.MeetingLongitude.HasValue);

        RuleFor(x => x.PaymentMethod)
            .Must(m => Enum.IsDefined(typeof(PaymentMethod), m))
            .WithMessage("Phương thức thanh toán không hợp lệ.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Số tiền không hợp lệ.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Số lượng mua tối thiểu là 1.")
            .LessThanOrEqualTo(999).WithMessage("Số lượng mua không hợp lệ.");

        RuleFor(x => x.DeliveryMethod)
            .Must(m => Enum.IsDefined(typeof(DeliveryMethod), m))
            .WithMessage("Hình thức giao nhận không hợp lệ.");

        RuleFor(x => x.MeetingLocation)
            .NotEmpty()
            .WithMessage("Vui lòng nhập địa chỉ nhận hàng khi chọn đặt ship bên ngoài.")
            .When(x => x.DeliveryMethod == (int)DeliveryMethod.ExternalShipper);
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

    /// <summary>Vĩ độ địa điểm giao nhận.</summary>
    public double? MeetingLatitude { get; set; }

    /// <summary>Kinh độ địa điểm giao nhận.</summary>
    public double? MeetingLongitude { get; set; }

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

        RuleFor(x => x.MeetingLatitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -90d && v.Value <= 90d))
            .WithMessage("Vĩ độ không hợp lệ.");

        RuleFor(x => x.MeetingLongitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -180d && v.Value <= 180d))
            .WithMessage("Kinh độ không hợp lệ.");

        RuleFor(x => x)
            .Must(x => x.MeetingLatitude.HasValue == x.MeetingLongitude.HasValue)
            .WithMessage("Phải cung cấp đầy đủ vĩ độ và kinh độ.");

        RuleFor(x => x.MeetingLocation)
            .NotEmpty()
            .WithMessage("Phải nhập địa chỉ khi cung cấp tọa độ.")
            .When(x => x.MeetingLatitude.HasValue || x.MeetingLongitude.HasValue);

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

/// <summary>
/// Người bán duyệt đơn và (nếu giao trực tiếp) chốt lại địa điểm giao dịch đã thống nhất.
/// </summary>
public class AcceptTransactionDto
{
    public int? DeliveryMethod { get; set; }
    public string? MeetingLocation { get; set; }
    public double? MeetingLatitude { get; set; }
    public double? MeetingLongitude { get; set; }
    public string? DeliveryCarrier { get; set; }
    public string? TrackingCode { get; set; }
}

public class AcceptTransactionDtoValidator : AbstractValidator<AcceptTransactionDto>
{
    public AcceptTransactionDtoValidator()
    {
        RuleFor(x => x.DeliveryMethod)
            .Must(m => m.HasValue && Enum.IsDefined(typeof(DeliveryMethod), m.Value))
            .WithMessage("Hình thức giao nhận không hợp lệ.")
            .When(x => x.DeliveryMethod.HasValue);

        RuleFor(x => x.MeetingLocation)
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLocation));

        RuleFor(x => x.MeetingLatitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -90d && v.Value <= 90d))
            .WithMessage("Vĩ độ không hợp lệ.");

        RuleFor(x => x.MeetingLongitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -180d && v.Value <= 180d))
            .WithMessage("Kinh độ không hợp lệ.");

        RuleFor(x => x)
            .Must(x => x.MeetingLatitude.HasValue == x.MeetingLongitude.HasValue)
            .WithMessage("Phải cung cấp đầy đủ vĩ độ và kinh độ.");

        RuleFor(x => x.DeliveryCarrier)
            .MaximumLength(100).WithMessage("Tên đơn vị giao hàng không quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.DeliveryCarrier));

        RuleFor(x => x.TrackingCode)
            .MaximumLength(100).WithMessage("Mã vận đơn / thông tin shipper không quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.TrackingCode));
    }
}

/// <summary>
/// Người bán chuyển đơn sang trạng thái Đang giao (tự giao hoặc đã giao cho shipper ngoài).
/// </summary>
public class StartDeliveryDto
{
    public string? MeetingLocation { get; set; }
    public double? MeetingLatitude { get; set; }
    public double? MeetingLongitude { get; set; }
    public string? DeliveryCarrier { get; set; }
    public string? TrackingCode { get; set; }
}

public class StartDeliveryDtoValidator : AbstractValidator<StartDeliveryDto>
{
    public StartDeliveryDtoValidator()
    {
        RuleFor(x => x.MeetingLocation)
            .MinimumLength(2).WithMessage("Địa điểm gặp mặt phải có ít nhất 2 ký tự.")
            .MaximumLength(255).WithMessage("Địa điểm gặp mặt không được vượt quá 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingLocation));

        RuleFor(x => x.MeetingLatitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -90d && v.Value <= 90d))
            .WithMessage("Vĩ độ không hợp lệ.");

        RuleFor(x => x.MeetingLongitude)
            .Must(v => !v.HasValue || (double.IsFinite(v.Value) && v.Value >= -180d && v.Value <= 180d))
            .WithMessage("Kinh độ không hợp lệ.");

        RuleFor(x => x)
            .Must(x => x.MeetingLatitude.HasValue == x.MeetingLongitude.HasValue)
            .WithMessage("Phải cung cấp đầy đủ vĩ độ và kinh độ.");

        RuleFor(x => x.DeliveryCarrier)
            .MaximumLength(100).WithMessage("Tên đơn vị giao hàng không quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.DeliveryCarrier));

        RuleFor(x => x.TrackingCode)
            .MaximumLength(100).WithMessage("Mã vận đơn / thông tin shipper không quá 100 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.TrackingCode));
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

    /// <summary>Số lượng sản phẩm trong đơn.</summary>
    public int Quantity { get; init; } = 1;

    /// <summary>Địa điểm gặp mặt giao dịch.</summary>
    public string MeetingLocation { get; init; } = null!;

    public double? MeetingLatitude { get; init; }

    public double? MeetingLongitude { get; init; }

    public int DeliveryMethod { get; init; }
    public string DeliveryMethodName { get; init; } = null!;
    public string? DeliveryCarrier { get; init; }
    public string? TrackingCode { get; init; }
    public string? InspectionNote { get; init; }

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
        (int)TransactionStatus.Pending => "Chờ xác nhận",
        (int)TransactionStatus.Accepted => "Đã duyệt / Chuẩn bị giao",
        (int)TransactionStatus.Rejected => "Từ chối nhận khi đồng kiểm",
        (int)TransactionStatus.Cancelled => "Đã hủy giao dịch",
        (int)TransactionStatus.Completed => "Đã hoàn thành",
        (int)TransactionStatus.AwaitingConfirmation => "Chờ xác nhận đồng kiểm",
        (int)TransactionStatus.Delivering => "Đang giao hàng",
        _ => "Không xác định"
    };

    public static string DeliveryMethodName(int method) => method switch
    {
        (int)DeliveryMethod.DirectMeetup => "Gặp trực tiếp / Tự giao",
        (int)DeliveryMethod.ExternalShipper => "Đặt ship ngoài (2 bên tự thoả thuận cước)",
        _ => "Gặp trực tiếp / Tự giao"
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
