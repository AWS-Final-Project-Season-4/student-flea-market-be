using FluentValidation;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.DTOs.Chat;

// ============================================================ REQUEST (đầu vào)

/// <summary>
/// Mở (hoặc tạo) hội thoại chat về một món đồ.
///
/// Người dùng bấm nút "Chat với người bán" trên trang chi tiết tin đăng.
/// Người bán KHÔNG cần gửi — hệ thống luôn suy ra từ chủ tin đăng.
/// </summary>
public class CreateConversationDto
{
    /// <summary>Id (Guid) của món đồ cần chat.</summary>
    public Guid ItemId { get; set; }
}

public class CreateConversationDtoValidator : AbstractValidator<CreateConversationDto>
{
    public CreateConversationDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Phải chọn món đồ cần trò chuyện.");
    }
}

/// <summary>
/// Gửi một tin nhắn vào hội thoại.
///
/// Endpoint REST này là ĐƯỜNG DỰ PHÒNG khi WebSocket không hoạt động
/// (mất mạng, app chưa mở SignalR). Cả hai đường đều gọi chung service.
/// </summary>
public class SendMessageDto
{
    /// <summary>Nội dung tin nhắn (tối đa 1000 ký tự).</summary>
    public string Content { get; set; } = null!;
}

public class SendMessageDtoValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống.")
            .MaximumLength(1000).WithMessage("Nội dung tin nhắn không được vượt quá 1000 ký tự.");
    }
}

// ============================================================ RESPONSE (đầu ra)

/// <summary>
/// Một tin nhắn trong hội thoại.
/// </summary>
public class MessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }

    /// <summary>Id người gửi.</summary>
    public Guid SenderId { get; init; }

    /// <summary>Tên người gửi (hiển thị trong UI).</summary>
    public string SenderName { get; init; } = null!;

    /// <summary>Ảnh đại diện người gửi (nếu có).</summary>
    public string? SenderAvatarUrl { get; init; }

    /// <summary>Nội dung tin nhắn. Nếu tin đã bị xoá thì trả về null.</summary>
    public string? Content { get; init; }

    /// <summary>Tin nhắn đã bị xoá hay không (soft delete).</summary>
    public bool IsDeleted { get; init; }

    /// <summary>Bên nhận đã đọc tin này chưa.</summary>
    public bool IsRead { get; init; }

    /// <summary>Thời điểm gửi tin.</summary>
    public DateTime SentAt { get; init; }

    /// <summary>Thời điểm được đọc (null = chưa đọc).</summary>
    public DateTime? ReadAt { get; init; }
}

/// <summary>
/// Bên kia trong cuộc trò chuyện (người mua hoặc người bán).
/// </summary>
public class ChatPartyDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Tóm tắt món đồ trong hội thoại (để UI hiển thị header + ảnh).
/// </summary>
public class ChatItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;

    /// <summary>Mã hàng 8 ký tự — để 2 bên dễ trao đổi ngoài đời.</summary>
    public string ItemCode { get; init; } = null!;

    public string Slug { get; init; } = null!;
    public decimal Price { get; init; }
    public string? PrimaryImageUrl { get; init; }

    /// <summary>ItemStatus: 1 = Đang bán, 2 = Đã gỡ, 3 = Đã đặt, 4 = Đã bán.</summary>
    public int Status { get; init; }
    public string StatusName { get; init; } = null!;
}

/// <summary>
/// Một hội thoại trong danh sách / chi tiết.
///
/// Cờ <see cref="IsItemUnavailable"/> cho biết món đồ không còn "đang bán"
/// (đã gỡ / đã bán / đã đặt). Lưu ý: hội thoại KHÔNG bị khoá — hai bên
/// vẫn nhắn tin được, chỉ là UI nên hiện cảnh báo cho người dùng.
/// </summary>
public class ConversationDto
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public ChatItemDto Item { get; init; } = null!;

    /// <summary>Id chủ tin đăng (người bán).</summary>
    public Guid SellerId { get; init; }

    /// <summary>Id người mua.</summary>
    public Guid BuyerId { get; init; }

    /// <summary>Bên kia (đối tác trò chuyện) từ góc nhìn của người đang xem.</summary>
    public ChatPartyDto OtherParty { get; init; } = null!;

    /// <summary>
    /// Người đang xem là người mua hay người bán trong hội thoại này.
    /// Dùng để client biết tin nào là "của mình" khi hiển thị.
    /// </summary>
    public bool ViewerIsBuyer { get; init; }

    /// <summary>Nội dung xem trước của tin nhắn mới nhất.</summary>
    public string? LastMessage { get; init; }

    /// <summary>Thời điểm tin nhắn mới nhất.</summary>
    public DateTime? LastMessageAt { get; init; }

    /// <summary>Số tin chưa đọc của người đang xem trong hội thoại này.</summary>
    public int UnreadCount { get; init; }

    /// <summary>
    /// Món đồ không còn khả dụng (đã gỡ / đã bán / đã đặt) — UI hiện cảnh báo.
    /// </summary>
    public bool IsItemUnavailable { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Danh sách hội thoại + tổng số tin chưa đọc (dùng cho badge toàn app).
/// </summary>
public class ConversationListDto
{
    public List<ConversationDto> Items { get; init; } = new();

    /// <summary>TỔNG số tin chưa đọc của người dùng trên tất cả hội thoại.</summary>
    public int TotalUnread { get; init; }
}

/// <summary>
/// Lịch sử tin nhắn của một hội thoại (phân trang).
/// </summary>
public class MessageListDto
{
    public List<MessageDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// Kết quả đánh dấu đã đọc — dùng để client đồng bộ badge.
/// </summary>
public class MarkReadResultDto
{
    public Guid ConversationId { get; init; }

    /// <summary>Số tin vừa được đánh dấu đã đọc.</summary>
    public int MarkedCount { get; init; }

    /// <summary>Tổng số tin chưa đọc của người dùng (để cập nhật badge toàn app).</summary>
    public int TotalUnread { get; init; }
}

/// <summary>
/// Nhãn tiếng Việt cho trạng thái món đồ (hiển thị trong hội thoại).
/// </summary>
public static class ChatLabels
{
    public static string ItemStatusName(int status) => (ItemStatus)status switch
    {
        ItemStatus.Available => "Đang bán",
        ItemStatus.Removed => "Đã gỡ",
        ItemStatus.Reserved => "Đã đặt",
        ItemStatus.Sold => "Đã bán",
        _ => "Không xác định"
    };
}
