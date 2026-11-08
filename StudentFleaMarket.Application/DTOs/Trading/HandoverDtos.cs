using FluentValidation;
using StudentFleaMarket.Application.Common.Helpers;

namespace StudentFleaMarket.Application.DTOs.Trading;

// ============================================================ XÁC NHẬN BÀN GIAO (Exchange)

/// <summary>
/// Xác nhận bàn giao cho đơn ĐỔI ĐỒ.
///
/// VÌ SAO CẦN BƯỚC NÀY?
///   Ở đơn Exchange có 2 món đồ đi ngược chiều. Nếu chỉ 1 bên bấm "Hoàn
///   thành" thì bên đó có thể chiếm đoạt món của mình mà không cần gặp mặt —
///   đồng thời tạo ra một lời đánh giá 5 sao giả. Vì vậy đơn Exchange bắt buộc
///   CẢ 2 BÊN phải xác nhận thì đơn mới thực sự hoàn thành và mới mở khoá
///   đánh giá.
///
/// MÃ HÀNG (ItemCode) — CƠ CHẾ XÁC MINH RẺ VÀ HIỆU QUẢ NHẤT:
///   Bên nhận nhập mã hàng (8 ký tự) của món vừa nhận; hệ thống so với mã
///   hàng của món ĐÁNG LẼ phải nhận. Kẻ mang món khác đến (hành vi tráo hàng
///   phổ biến nhất) sẽ bị phát hiện NGAY TẠI CHỖ, không cần Moderator.
///
///   Nếu mã không khớp -> hệ thống KHÔNG cho xác nhận "đã nhận đúng" và
///   gợi ý tạo report.
/// </summary>
public class ConfirmHandoverDto
{
    /// <summary>
    /// Món đồ nhận được có đúng với mô tả / ảnh đăng không?
    /// false = có vấn đề -> hệ thống gợi ý tạo report.
    /// </summary>
    public bool ItemReceivedOk { get; set; } = true;

    /// <summary>
    /// Mã hàng (8 ký tự) của món vừa nhận. Hệ thống đối chiếu với món đáng
    /// lẽ phải nhận.
    ///
    /// Bắt buộc khi <see cref="ItemReceivedOk"/> = true (để xác minh chắc chắn).
    /// </summary>
    public string? ItemCodeVerified { get; set; }

    /// <summary>Ghi chú (không bắt buộc) — vd: "nhận sai sách, bìa rách".</summary>
    public string? Note { get; set; }
}

public class ConfirmHandoverDtoValidator : AbstractValidator<ConfirmHandoverDto>
{
    public ConfirmHandoverDtoValidator()
    {
        RuleFor(x => x.ItemCodeVerified)
            .Must(c => string.IsNullOrWhiteSpace(c) || ItemCodeHelper.IsValidFormat(c))
            .WithMessage("Mã hàng phải gồm đúng 8 ký tự chữ cái và chữ số.")
            .When(x => !string.IsNullOrWhiteSpace(x.ItemCodeVerified));

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

/// <summary>
/// Kết quả xác nhận bàn giao — trả về cho client để biết trạng thái đơn và
/// bước tiếp theo.
/// </summary>
public class HandoverConfirmationDto
{
    public Guid TransactionId { get; init; }

    /// <summary>Trạng thái đơn sau khi xác nhận (có thể sang AwaitingConfirmation).</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;

    /// <summary>Số bên đã xác nhận / tổng số bên cần xác nhận.</summary>
    public int ConfirmedCount { get; init; }
    public int RequiredCount { get; init; }

    /// <summary>Đơn đã hoàn thành chưa (cả 2 bên đã xác nhận).</summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Mã hàng đã xác minh có khớp với món đáng lẽ phải nhận không.
    /// null = người dùng không nhập mã (chỉ nhận "đúng mô tả" chứ không xác minh).
    /// </summary>
    public bool? ItemCodeMatched { get; init; }

    /// <summary>
    /// Mã hàng hệ thống ĐANG CHỜ người dùng nhập để đối chiếu (nếu có).
    /// Hiển thị cho người dùng để họ nhìn mã trên món thật rồi nhập vào.
    /// </summary>
    public string? ExpectedItemCode { get; init; }

    /// <summary>
    /// Gợi ý cho người dùng: nếu món nhận được có vấn đề, hãy tạo report.
    /// </summary>
    public string? Suggestion { get; init; }
}
