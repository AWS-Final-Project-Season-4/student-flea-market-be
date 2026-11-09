using FluentValidation;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Trading;

// ============================================================ REQUEST (đầu vào)

/// <summary>
/// Tạo yêu cầu đổi đồ.
///
/// Người gửi (requester) chọn:
///   - RequestedItemCode: mã hàng (8 ký tự) của món đồ của bên kia muốn nhận
///     (món đồ đó phải bật "cho phép đổi đồ").
///   - OfferedItemCode: mã hàng của món đồ của chính mình dùng để đề nghị đổi lấy.
///   - AdditionalAmount: số tiền bù chênh lệch (>= 0).
///   - Note: ghi chú thương lượng.
///
/// MÃ HÀNG vs ID
/// -------------
/// Người dùng nhập MÃ HÀNG (ItemCode) — chuỗi 8 ký tự A-Z0-9 dễ đọc/trao đổi.
/// Hệ thống tự tra cứu mã đó sang Id (Guid) nội bộ; Id không lộ ra nữa.
/// </summary>
public class CreateExchangeRequestDto
{
    /// <summary>Mã hàng của món đồ của bên kia mà muốn nhận (chủ: người nhận yêu cầu).</summary>
    public string RequestedItemCode { get; set; } = null!;

    /// <summary>Mã hàng của món đồ của chính mình dùng để đề nghị đổi lấy.</summary>
    public string OfferedItemCode { get; set; } = null!;

    /// <summary>Số tiền bù chênh lệch — không được âm.</summary>
    public decimal AdditionalAmount { get; set; }

    /// <summary>Ghi chú thương lượng (không bắt buộc).</summary>
    public string? Note { get; set; }
}

public class CreateExchangeRequestDtoValidator : AbstractValidator<CreateExchangeRequestDto>
{
    public CreateExchangeRequestDtoValidator()
    {
        RuleFor(x => x.RequestedItemCode)
            .NotEmpty().WithMessage("Phải nhập mã hàng món đồ muốn đổi.")
            .Must(ItemCodeHelper.IsValidFormat)
            .WithMessage("Mã hàng phải gồm đúng 8 ký tự chữ cái và chữ số.");

        RuleFor(x => x.OfferedItemCode)
            .NotEmpty().WithMessage("Phải nhập mã hàng món đồ đem đổi.")
            .Must(ItemCodeHelper.IsValidFormat)
            .WithMessage("Mã hàng phải gồm đúng 8 ký tự chữ cái và chữ số.");

        RuleFor(x => x.AdditionalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền bù chênh lệch không thể âm.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Số tiền bù chênh lệch không hợp lệ.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x)
            .Must(x => !string.Equals(
                ItemCodeHelper.Normalize(x.RequestedItemCode),
                ItemCodeHelper.Normalize(x.OfferedItemCode),
                StringComparison.Ordinal))
            .WithMessage("Không thể đổi một món đồ lấy chính nó.");
    }
}

/// <summary>
/// Phản hồi một yêu cầu đổi đồ (Accept / Reject).
/// </summary>
public class RespondExchangeRequestDto
{
    /// <summary>Ghi chú phản hồi (không bắt buộc) — hiển thị cho người gửi yêu cầu.</summary>
    public string? Note { get; set; }
}

public class RespondExchangeRequestDtoValidator : AbstractValidator<RespondExchangeRequestDto>
{
    public RespondExchangeRequestDtoValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

// ============================================================ RESPONSE (đầu ra)

/// <summary>
/// Thông tin tóm tắt một món đồ nằm trong yêu cầu đổi / đơn giao dịch.
/// </summary>
public class TradingItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;

    /// <summary>
    /// Mã hàng 8 ký tự — mã người dùng giao tiếp với hệ thống.
    /// </summary>
    public string ItemCode { get; init; } = null!;

    public decimal Price { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public string Location { get; init; } = null!;

    /// <summary>Id chủ tin đăng.</summary>
    public Guid SellerId { get; init; }

    /// <summary>Tên chủ tin đăng.</summary>
    public string SellerName { get; init; } = null!;
}

/// <summary>
/// Một bên trong giao dịch (người gửi yêu cầu / người nhận yêu cầu).
/// </summary>
public class TradingPartyDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Chi tiết một yêu cầu đổi đồ.
/// </summary>
public class ExchangeRequestDto
{
    public Guid Id { get; init; }

    /// <summary>Người gửi yêu cầu (chủ của OfferedItem).</summary>
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = null!;

    /// <summary>Món đồ của bên kia mà người gửi muốn nhận.</summary>
    public TradingItemDto RequestedItem { get; init; } = null!;

    /// <summary>Món đồ của người gửi dùng để đề nghị đổi lấy.</summary>
    public TradingItemDto OfferedItem { get; init; } = null!;

    /// <summary>Số tiền bù chênh lệch do người gửi trả thêm cho người nhận.</summary>
    public decimal AdditionalAmount { get; init; }

    /// <summary>Ghi chú thương lượng của người gửi.</summary>
    public string? Note { get; init; }

    /// <summary>Ghi chú phản hồi của người nhận yêu cầu.</summary>
    public string? ResponseNote { get; init; }

    /// <summary>ExchangeRequestStatus: 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Expired.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }

    /// <summary>
    /// Người đang xem là chủ của món đồ được nhận (RequestedItem) — tức là
    /// người có quyền accept / reject yêu cầu này.
    /// </summary>
    public bool CanRespond { get; init; }

    /// <summary>Người đang xem là người gửi yêu cầu — có quyền hủy yêu cầu.</summary>
    public bool CanCancel { get; init; }
}

/// <summary>
/// Tham số lọc danh sách yêu cầu đổi đồ.
/// </summary>
public class ExchangeRequestQueryDto
{
    /// <summary>
    /// null = tất cả; 1 = Pending, 2 = Accepted, 3 = Rejected, 4 = Cancelled, 5 = Expired.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// "incoming" = các yêu cầu người khác gửi cho mình (mình có quyền accept/reject).
    /// "outgoing" = các yêu cầu mình gửi cho người khác.
    /// null = cả hai.
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// "incoming" | "outgoing" — định hướng lọc (alias thân thiện của Direction).
    /// </summary>
    public string? Incoming { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Nhãn tiếng Việt cho trạng thái yêu cầu đổi đồ.
/// </summary>
public static class ExchangeRequestLabels
{
    public static string StatusName(int status) => status switch
    {
        (int)ExchangeRequestStatus.Pending => "Đang chờ phản hồi",
        (int)ExchangeRequestStatus.Accepted => "Đã được chấp nhận",
        (int)ExchangeRequestStatus.Rejected => "Bị từ chối",
        (int)ExchangeRequestStatus.Cancelled => "Đã hủy",
        (int)ExchangeRequestStatus.Expired => "Đã hết hạn",
        _ => "Không xác định"
    };
}
