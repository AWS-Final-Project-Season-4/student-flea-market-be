using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Chat;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Xử lý trò chuyện 1-1 giữa người mua và người bán, xoay quanh 1 món đồ.
///
/// QUY TẮC NGHIỆP VỤ
/// -----------------
/// 1. Mỗi cặp (món đồ, người mua) có ĐÚNG 1 hội thoại (unique index
///    ItemId + BuyerId). Bấm "Chat" nhiều lần vẫn trả về cùng 1 hội thoại.
/// 2. Người bán LUÔN là chủ món đồ (suy ra từ Item.SellerId) — không lấy từ
///    request để tránh chat nhầm người.
/// 3. Chỉ TẠO MỚI hội thoại khi món đồ còn "đang bán". Nếu hội thoại đã
///    tồn tại thì vẫn mở được và vẫn nhắn tin được — kể cả khi món đồ đã
///    bị gỡ / đã bán (giữ lịch sử chat).
/// 4. Chỉ 2 bên liên quan (Buyer / Seller) được xem và gửi tin trong hội thoại.
/// 5. Số tin chưa đọc đếm theo HƯỚNG NHẬN:
///    BuyerUnreadCount = tin của Seller mà Buyer chưa đọc.
/// 6. Chat KHÔNG bị khoá khi món đồ hết khả dụng — UI chỉ hiện cảnh báo.
/// </summary>
public class ChatService : IChatService
{
    /// <summary>Độ dài tối đa của nội dung tin nhắn.</summary>
    private const int MaxContentLength = 1000;

    /// <summary>Độ dài tối đa của nội dung xem trước (LastMessage).</summary>
    private const int MaxPreviewLength = 120;

    private readonly IChatRepository _chatRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUserRepository _userRepo;

    public ChatService(
        IChatRepository chatRepo,
        IMessageRepository messageRepo,
        IItemRepository itemRepo,
        IUserRepository userRepo)
    {
        _chatRepo = chatRepo;
        _messageRepo = messageRepo;
        _itemRepo = itemRepo;
        _userRepo = userRepo;
    }

    // =====================================================================
    // GET-OR-CREATE
    // =====================================================================
    public async Task<ConversationDto> GetOrCreateConversationAsync(
        Guid userId,
        CreateConversationDto request,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        // ----- Đã có hội thoại chưa? -----
        // Kiểm tra TRƯỚC khi check trạng thái món đồ: nếu hội thoại đã tồn
        // tại từ trước khi món đồ bị gỡ / bán thì người dùng vẫn mở được và
        // vẫn nhắn tin được (giữ lịch sử chat).
        var existing = await _chatRepo.GetByItemAndBuyerAsync(request.ItemId, userId, ct);
        if (existing is not null)
            return await BuildDtoAsync(existing.Id, userId, ct);

        // ----- Món đồ -----
        var item = await _itemRepo.GetDetailAsync(request.ItemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        if (item.SellerId == userId)
            throw new ValidationException(
                "Bạn không thể tự nhắn tin với chính mình.",
                "CANNOT_CHAT_WITH_SELF");

        if (item.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ này không còn đang bán nên không thể mở cuộc trò chuyện mới.",
                "ITEM_NOT_AVAILABLE");

        // ----- Tạo hội thoại mới -----
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            BuyerId = userId,
            SellerId = item.SellerId,
            BuyerUnreadCount = 0,
            SellerUnreadCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepo.AddAsync(conversation, ct);
        await _chatRepo.SaveChangesAsync(ct);

        return await BuildDtoAsync(conversation.Id, userId, ct);
    }

    // =====================================================================
    // LIST
    // =====================================================================
    public async Task<ConversationListDto> ListConversationsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (conversations, _) = await _chatRepo.ListForUserAsync(userId, page, pageSize, ct);
        var totalUnread = await _chatRepo.GetTotalUnreadAsync(userId, ct);

        return new ConversationListDto
        {
            Items = conversations.Select(c => MapDto(c, userId)).ToList(),
            TotalUnread = totalUnread
        };
    }

    public async Task<ConversationDto> GetConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken ct = default)
    {
        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        return MapDto(conversation, userId);
    }

    // =====================================================================
    // MESSAGES
    // =====================================================================
    public async Task<MessageListDto> ListMessagesAsync(
        Guid userId,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var (messages, total) = await _chatRepo.ListMessagesAsync(
            conversationId, page, pageSize, ct);

        var pageNo = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => 50,
            > 100 => 100,
            _ => pageSize
        };

        return new MessageListDto
        {
            Items = messages.Select(MapMessage).ToList(),
            TotalCount = total,
            Page = pageNo,
            PageSize = size
        };
    }

    // =====================================================================
    // SEND
    // =====================================================================
    public async Task<MessageDto> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        SendMessageDto request,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            throw new ValidationException(
                "Nội dung tin nhắn không được để trống.",
                "MESSAGE_CONTENT_EMPTY");

        if (content.Length > MaxContentLength)
            throw new ValidationException(
                $"Nội dung tin nhắn không được vượt quá {MaxContentLength} ký tự.",
                "MESSAGE_CONTENT_TOO_LONG");

        if (request.ReplyToMessageId.HasValue)
        {
            var replyTarget = await _messageRepo.GetByIdAsync(request.ReplyToMessageId.Value, ct);
            if (replyTarget is null || replyTarget.ConversationId != conversationId)
                throw new NotFoundException(
                    "Không tìm thấy tin nhắn gốc để trả lời.",
                    "REPLY_MESSAGE_NOT_FOUND");
        }

        var now = DateTime.UtcNow;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,
            Content = content,
            IsRead = false,
            IsDeleted = false,
            SentAt = now,
            ReplyToMessageId = request.ReplyToMessageId
        };

        await _messageRepo.AddAsync(message, ct);
        await _messageRepo.SaveChangesAsync(ct);

        // Cập nhật preview + thời điểm tin mới nhất trên hội thoại.
        var trackedConv = await _chatRepo.GetByIdAsync(conversationId, ct);
        if (trackedConv is not null)
        {
            trackedConv.LastMessage = BuildPreview(content);
            trackedConv.LastMessageAt = now;
            await _chatRepo.SaveChangesAsync(ct);
        }

        // Tăng số tin chưa đọc của phía NHẬN (người gửi không tự đếm tin mình).
        var recipientId = userId == conversation.BuyerId
            ? conversation.SellerId
            : conversation.BuyerId;

        await _chatRepo.IncrementUnreadAsync(conversationId, recipientId, ct);

        // Nạp lại để có quan hệ Sender / ReplyToMessage cho DTO.
        var saved = await _messageRepo.GetWithSenderAsync(message.Id, ct);
        return MapMessage(saved ?? message);
    }

    // =====================================================================
    // EDIT / RECALL / PIN
    // =====================================================================
    public async Task<MessageDto> EditMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        EditMessageDto request,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var message = await _messageRepo.GetByIdAsync(messageId, ct);
        if (message is null || message.ConversationId != conversationId)
            throw new NotFoundException("Không tìm thấy tin nhắn.", "MESSAGE_NOT_FOUND");

        if (message.SenderId != userId)
            throw new ForbiddenException("Bạn chỉ có thể chỉnh sửa tin nhắn của chính mình.", "FORBIDDEN_MESSAGE_EDIT");

        if (message.IsDeleted)
            throw new ValidationException("Không thể chỉnh sửa tin nhắn đã thu hồi.", "MESSAGE_ALREADY_RECALLED");

        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            throw new ValidationException("Nội dung tin nhắn không được để trống.", "MESSAGE_CONTENT_EMPTY");

        if (content.Length > MaxContentLength)
            throw new ValidationException(
                $"Nội dung tin nhắn không được vượt quá {MaxContentLength} ký tự.",
                "MESSAGE_CONTENT_TOO_LONG");

        message.Content = content;
        message.EditedAt = DateTime.UtcNow;
        await _messageRepo.SaveChangesAsync(ct);

        await SyncConversationLastMessageAsync(conversationId, ct);

        var saved = await _messageRepo.GetWithSenderAsync(message.Id, ct);
        return MapMessage(saved ?? message);
    }

    public async Task<MessageDto> RecallMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var message = await _messageRepo.GetByIdAsync(messageId, ct);
        if (message is null || message.ConversationId != conversationId)
            throw new NotFoundException("Không tìm thấy tin nhắn.", "MESSAGE_NOT_FOUND");

        if (message.SenderId != userId)
            throw new ForbiddenException("Bạn chỉ có thể thu hồi tin nhắn của chính mình.", "FORBIDDEN_MESSAGE_RECALL");

        if (!message.IsDeleted)
        {
            message.IsDeleted = true;
            message.IsPinned = false;
            message.PinnedAt = null;
            message.PinnedById = null;
            await _messageRepo.SaveChangesAsync(ct);

            await SyncConversationLastMessageAsync(conversationId, ct);
        }

        var saved = await _messageRepo.GetWithSenderAsync(message.Id, ct);
        return MapMessage(saved ?? message);
    }

    public async Task<MessageDto> SetPinMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        bool isPinned,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var message = await _messageRepo.GetByIdAsync(messageId, ct);
        if (message is null || message.ConversationId != conversationId)
            throw new NotFoundException("Không tìm thấy tin nhắn.", "MESSAGE_NOT_FOUND");

        if (message.IsDeleted)
            throw new ValidationException("Không thể ghim tin nhắn đã thu hồi.", "MESSAGE_ALREADY_RECALLED");

        if (message.IsPinned != isPinned)
        {
            message.IsPinned = isPinned;
            message.PinnedAt = isPinned ? DateTime.UtcNow : null;
            message.PinnedById = isPinned ? userId : null;
            await _messageRepo.SaveChangesAsync(ct);
        }

        var saved = await _messageRepo.GetWithSenderAsync(message.Id, ct);
        return MapMessage(saved ?? message);
    }

    public async Task<List<MessageDto>> ListPinnedMessagesAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken ct = default)
    {
        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var pinned = await _chatRepo.ListPinnedMessagesAsync(conversationId, ct);
        return pinned.Select(MapMessage).ToList();
    }

    public async Task<MessageDto> ToggleReactionAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        ReactMessageDto request,
        CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(userId, ct);

        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var message = await _messageRepo.GetByIdAsync(messageId, ct);
        if (message is null || message.ConversationId != conversationId)
            throw new NotFoundException("Không tìm thấy tin nhắn.", "MESSAGE_NOT_FOUND");

        if (message.IsDeleted)
            throw new ValidationException("Không thể thả cảm xúc cho tin nhắn đã thu hồi.", "MESSAGE_ALREADY_RECALLED");

        var emoji = request.Emoji?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(emoji) || emoji.Length > 16)
            throw new ValidationException("Biểu tượng cảm xúc không hợp lệ.", "INVALID_REACTION_EMOJI");

        await _messageRepo.ToggleReactionAsync(messageId, userId, emoji, ct);

        var saved = await _messageRepo.GetWithSenderAsync(messageId, ct);
        return MapMessage(saved ?? message);
    }

    // =====================================================================
    // MARK AS READ
    // =====================================================================
    public async Task<MarkReadResultDto> MarkAsReadAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken ct = default)
    {
        var conversation = await GetDetailAsync(conversationId, ct);
        EnsureParticipant(conversation, userId);

        var readAt = DateTime.UtcNow;
        var marked = await _chatRepo.MarkConversationAsReadAsync(
            conversationId, userId, readAt, ct);

        // Reset bộ đếm unread của phía người đọc.
        if (conversation.BuyerId == userId)
            conversation.BuyerUnreadCount = 0;
        else
            conversation.SellerUnreadCount = 0;

        _chatRepo.Update(conversation);
        await _chatRepo.SaveChangesAsync(ct);

        var totalUnread = await _chatRepo.GetTotalUnreadAsync(userId, ct);

        return new MarkReadResultDto
        {
            ConversationId = conversationId,
            MarkedCount = marked,
            TotalUnread = totalUnread
        };
    }

    public Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default)
        => _chatRepo.GetTotalUnreadAsync(userId, ct);

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task<Conversation> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var conversation = await _chatRepo.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy cuộc trò chuyện.", "CONVERSATION_NOT_FOUND");

        return conversation;
    }

    /// <summary>
    /// Chỉ 2 bên liên quan (Buyer / Seller) được phép xem / gửi tin.
    /// </summary>
    private static void EnsureParticipant(Conversation conversation, Guid userId)
    {
        if (conversation.BuyerId != userId && conversation.SellerId != userId)
            throw new ForbiddenException(
                "Bạn không có quyền truy cập cuộc trò chuyện này.",
                "NOT_CONVERSATION_PARTICIPANT");
    }

    /// <summary>
    /// Chặn tài khoản bị Admin cấm.
    ///
    /// Lưu ý: middleware <c>BannedUserBlockMiddleware</c> KHÔNG áp dụng cho
    /// WebSocket của SignalR (handshake không đi qua pipeline HTTP), nên service
    /// phải tự kiểm tra — nếu không thì tài khoản bị cấm vẫn chat được.
    /// </summary>
    private async Task EnsureUserActiveAsync(Guid userId, CancellationToken ct)
    {
        var status = await _userRepo.GetStatusAsync(userId, ct);
        if (status.HasValue && status.Value.Status == (int)UserStatus.Banned)
            throw new ForbiddenException(
                "Tài khoản của bạn đã bị quản trị viên khoá nên không thể trò chuyện.",
                "ACCOUNT_BANNED");
    }

    /// <summary>
    /// Cắt ngắn nội dung để làm "xem trước" trong danh sách hội thoại.
    /// </summary>
    private static string BuildPreview(string content)
    {
        var normalized = content.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= MaxPreviewLength) return normalized;

        return normalized[..MaxPreviewLength] + "…";
    }

    private async Task SyncConversationLastMessageAsync(Guid conversationId, CancellationToken ct)
    {
        var latest = await _chatRepo.GetLatestMessageAsync(conversationId, ct);
        var trackedConv = await _chatRepo.GetByIdAsync(conversationId, ct);
        if (trackedConv is null || latest is null) return;

        trackedConv.LastMessage = latest.IsDeleted
            ? "Tin nhắn đã được thu hồi"
            : BuildPreview(latest.Content);
        trackedConv.LastMessageAt = latest.SentAt;
        await _chatRepo.SaveChangesAsync(ct);
    }

    private static MessageDto MapMessage(Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        SenderName = message.Sender?.FullName ?? "",
        SenderAvatarUrl = message.Sender?.AvatarUrl,
        Content = message.IsDeleted ? null : message.Content,
        IsDeleted = message.IsDeleted,
        IsRead = message.IsRead,
        SentAt = message.SentAt,
        ReadAt = message.ReadAt,
        EditedAt = message.EditedAt,
        IsPinned = !message.IsDeleted && message.IsPinned,
        PinnedAt = message.IsDeleted ? null : message.PinnedAt,
        PinnedById = message.IsDeleted ? null : message.PinnedById,
        PinnedByName = message.IsDeleted ? null : message.PinnedBy?.FullName,
        ReplyToMessageId = message.ReplyToMessageId,
        ReplyTo = message.ReplyToMessage is null
            ? null
            : new ReplyPreviewDto
            {
                Id = message.ReplyToMessage.Id,
                SenderId = message.ReplyToMessage.SenderId,
                SenderName = message.ReplyToMessage.Sender?.FullName ?? "",
                Content = message.ReplyToMessage.IsDeleted ? null : message.ReplyToMessage.Content,
                IsDeleted = message.ReplyToMessage.IsDeleted
            },
        Reactions = message.IsDeleted
            ? new List<MessageReactionDto>()
            : message.Reactions
                .OrderBy(r => r.ReactedAt)
                .Select(r => new MessageReactionDto
                {
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? "",
                    Emoji = r.Emoji,
                    ReactedAt = r.ReactedAt
                })
                .ToList()
    };

    private static ConversationDto MapDto(Conversation c, Guid viewerId)
    {
        var viewerIsBuyer = c.BuyerId == viewerId;
        var other = viewerIsBuyer ? c.Seller : c.Buyer;

        var item = c.Item;
        var itemStatus = item?.Status ?? (int)ItemStatus.Available;
        var isItemUnavailable = item is null
            || item.IsDeleted
            || itemStatus != (int)ItemStatus.Available;

        return new ConversationDto
        {
            Id = c.Id,
            ItemId = c.ItemId,
            SellerId = c.SellerId,
            BuyerId = c.BuyerId,
            ViewerIsBuyer = viewerIsBuyer,
            Item = new ChatItemDto
            {
                Id = item?.Id ?? c.ItemId,
                Title = item?.Title ?? "",
                ItemCode = item?.ItemCode ?? "",
                Slug = item?.Slug ?? "",
                Price = item?.Price ?? 0,
                PrimaryImageUrl = GetPrimaryImageUrl(item),
                Status = itemStatus,
                StatusName = ChatLabels.ItemStatusName(itemStatus)
            },
            OtherParty = new ChatPartyDto
            {
                Id = other?.Id ?? Guid.Empty,
                FullName = other?.FullName ?? "",
                Username = other?.Username,
                AvatarUrl = other?.AvatarUrl
            },
            LastMessage = c.LastMessage,
            LastMessageAt = c.LastMessageAt,
            UnreadCount = viewerIsBuyer ? c.BuyerUnreadCount : c.SellerUnreadCount,
            IsItemUnavailable = isItemUnavailable,
            CreatedAt = c.CreatedAt
        };
    }

    private static string? GetPrimaryImageUrl(Item? item)
    {
        if (item?.Images is null) return null;

        var primary = item.Images
            .OrderBy(i => i.IsPrimary ? 0 : 1)
            .ThenBy(i => i.Order)
            .FirstOrDefault();

        return primary?.ImageUrl;
    }

    private async Task<ConversationDto> BuildDtoAsync(Guid id, Guid viewerId, CancellationToken ct)
    {
        var conversation = await GetDetailAsync(id, ct);
        return MapDto(conversation, viewerId);
    }
}
