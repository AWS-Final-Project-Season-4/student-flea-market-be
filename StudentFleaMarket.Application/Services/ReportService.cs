using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trust;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Gửi report vi phạm (phía người dùng).
///
/// BA DẠNG REPORT — mỗi dạng có bằng chứng bắt buộc riêng:
///
/// 1) BÁO CÁO BÀI ĐĂNG (không cần giao dịch):
///    - Cần: ItemCode. Không cần TransactionId.
///    - Dùng cho: tin sai sự thật, lừa đảo, ảnh giả.
///
/// 2) BÁO CÁO TRONG GIAO DỊCH (bắt buộc có đơn):
///    - Cần: TransactionId. ItemCode tuỳ chọn (món nào bị vấn đề).
///    - Bắt buộc: người bị report phải là BÊN CÒN LẠI của đơn, và người báo
///      phải là 1 trong 2 bên. Đây là cơ chế CHỐNG REPORT OAN — không ai có
///      thể report một người lạ chỉ vì biết tên.
///
/// 3) BÁO CÁO QUA HỘI THOẠI (quấy rối):
///    - Cần: ConversationId. Không cần TransactionId.
///    - Bắt buộc: người bị report phải là bên còn lại của hội thoại.
///
/// CHỐNG SPAM:
///   Unique index (ReporterId, TransactionId / ItemId / ConversationId) ở tầng
///   DB đảm bảo 1 người chỉ report 1 lần cho mỗi đối tượng. Ở tầng service
///   ta kiểm tra trước để trả về thông báo thân thiện thay vì lỗi DB.
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepo;
    private readonly IItemRepository _itemRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IChatRepository _chatRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IUserRepository _userRepo;

    public ReportService(
        IReportRepository reportRepo,
        IItemRepository itemRepo,
        ITransactionRepository txRepo,
        IChatRepository chatRepo,
        IMessageRepository messageRepo,
        IUserRepository userRepo)
    {
        _reportRepo = reportRepo;
        _itemRepo = itemRepo;
        _txRepo = txRepo;
        _chatRepo = chatRepo;
        _messageRepo = messageRepo;
        _userRepo = userRepo;
    }

    // =====================================================================
    // CREATE
    // =====================================================================
    public async Task<MyReportDto> CreateAsync(
        Guid reporterId,
        CreateReportDto request,
        CancellationToken ct = default)
    {
        var item = await ResolveItemAsync(request, ct);
        var transaction = await ResolveTransactionAsync(reporterId, request, ct);
        var (conversation, reportedMessage) = await ResolveConversationAndMessageAsync(reporterId, request, ct);

        // Người bị report là BÊN CÒN LẠI của người báo (không phải của người
        // khởi tạo đơn). Nếu lấy nhầm theo Initiator/Counterparty cố định thì
        // khi NGƯỜI BÁN báo cáo người mua, hệ thống sẽ tự báo cáo chính mình.
        var reportedUserId = ResolveReportedUserId(reporterId, transaction, conversation);

        if (reportedUserId is null && item is not null && item.SellerId != reporterId)
            reportedUserId = item.SellerId;

        // Chặn spam: 1 người chỉ report 1 lần cho mỗi đối tượng.
        await EnsureNotDuplicateAsync(reporterId, item, transaction, conversation, reportedMessage, ct);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            ReportedUserId = reportedUserId,
            ItemId = item?.Id,
            TransactionId = transaction?.Id,
            ConversationId = conversation?.Id,
            MessageId = reportedMessage?.Id,
            MessageEvidence = reportedMessage?.Content,
            Reason = request.Reason,
            Description = request.Description.Trim(),
            Status = (int)ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _reportRepo.AddAsync(report, ct);
        await _reportRepo.SaveChangesAsync(ct);

        return MapDto(report, item?.Title, reportedUserId is null ? null : await GetUserNameAsync(reportedUserId.Value, ct));
    }

    // =====================================================================
    // LIST / GET
    // =====================================================================
    public async Task<PagedResult<MyReportDto>> ListMineAsync(
        Guid reporterId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (reports, total) = await _reportRepo.ListByReporterAsync(reporterId, page, pageSize, ct);

        var items = new List<MyReportDto>(reports.Count);
        foreach (var r in reports)
        {
            items.Add(MapDto(
                r,
                r.Item?.Title,
                r.ReportedUser?.FullName));
        }

        return new PagedResult<MyReportDto>
        {
            Items = items,
            TotalCount = total,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => pageSize
            }
        };
    }

    public async Task<MyReportDto> GetMineAsync(Guid reporterId, Guid reportId, CancellationToken ct = default)
    {
        var report = await _reportRepo.GetDetailAsync(reportId, ct)
            ?? throw new NotFoundException("Không tìm thấy báo cáo.", "REPORT_NOT_FOUND");

        if (report.ReporterId != reporterId)
            throw new ForbiddenException("Bạn không có quyền xem báo cáo của người khác.", "NOT_REPORT_OWNER");

        return MapDto(report, report.Item?.Title, report.ReportedUser?.FullName);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Xác định người bị report = BÊN CÒN LẠI của người báo.
    ///
    /// VÌ SAO PHẢI TÍNH THEO NGƯỜI BÁO?
    ///   Một đơn giao dịch có 2 bên: Initiator (người mở đơn) và Counterparty
    ///   (bên còn lại). Nếu cứ lấy "CounterpartyId" thì khi NGƯỜI BÁN là người
    ///   báo cáo, hệ thống sẽ trỏ về chính người báo và ném lỗi
    ///   CANNOT_REPORT_SELF — tức là tính năng "người bán báo cáo người mua
    ///   bùng hàng / tráo hàng" (đúng nhu cầu chính của feature) sẽ không
    ///   bao giờ chạy được.
    /// </summary>
    private static Guid? ResolveReportedUserId(
        Guid reporterId,
        Transaction? transaction,
        Conversation? conversation)
    {
        if (transaction is not null)
        {
            if (transaction.InitiatorId == reporterId)
                return transaction.CounterpartyId;

            if (transaction.CounterpartyId == reporterId)
                return transaction.InitiatorId;

            // Người báo không tham gia đơn này (đã bị chặn ở tầng trên).
            return null;
        }

        if (conversation is not null)
        {
            if (conversation.BuyerId == reporterId)
                return conversation.SellerId;

            if (conversation.SellerId == reporterId)
                return conversation.BuyerId;

            return null;
        }

        return null;
    }

    /// <summary>
    /// Xác định tin đăng bị báo cáo (nếu có).
    /// </summary>
    private async Task<Item?> ResolveItemAsync(CreateReportDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ItemCode))
            return null;

        var code = ItemCodeHelper.Normalize(request.ItemCode);

        return await _itemRepo.GetByCodeAsync(code, ct)
            ?? throw new NotFoundException(
                $"Không tìm thấy tin đăng có mã \"{code}\".",
                "ITEM_CODE_NOT_FOUND");
    }

    /// <summary>
    /// Xác định đơn giao dịch liên quan (nếu có) và kiểm tra người báo /
    /// người bị report có thực sự là 2 bên của đơn không.
    ///
    /// CHỐNG REPORT OAN: người báo phải là 1 trong 2 bên của đơn. Nhờ vậy
    /// không ai có thể report một người lạ chỉ vì biết tên — chỉ người đã
    /// thực sự giao dịch với họ mới có tư cách report.
    /// </summary>
    private async Task<Transaction?> ResolveTransactionAsync(
        Guid reporterId,
        CreateReportDto request,
        CancellationToken ct)
    {
        if (!request.TransactionId.HasValue)
            return null;

        var tx = await _txRepo.GetDetailAsync(request.TransactionId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        if (tx.InitiatorId != reporterId && tx.CounterpartyId != reporterId)
            throw new ForbiddenException(
                "Bạn chỉ có thể báo cáo trong giao dịch mà bạn tham gia.",
                "NOT_TRANSACTION_PARTICIPANT");

        return tx;
    }

    /// <summary>
    /// Xác định hội thoại và tin nhắn vi phạm liên quan (nếu có), đồng thời kiểm
    /// tra người báo thực sự tham gia hội thoại (chống report oan).
    /// </summary>
    private async Task<(Conversation? Conversation, Message? Message)> ResolveConversationAndMessageAsync(
        Guid reporterId,
        CreateReportDto request,
        CancellationToken ct)
    {
        Message? message = null;
        if (request.MessageId.HasValue)
        {
            message = await _messageRepo.GetWithSenderAsync(request.MessageId.Value, ct)
                ?? throw new NotFoundException("Không tìm thấy tin nhắn cần báo cáo.", "MESSAGE_NOT_FOUND");

            if (message.IsDeleted)
                throw new ValidationException("Không thể báo cáo tin nhắn đã được thu hồi.", "MESSAGE_ALREADY_RECALLED");

            if (message.SenderId == reporterId)
                throw new ValidationException("Bạn không thể tự báo cáo tin nhắn của chính mình.", "CANNOT_REPORT_SELF");

            if (request.ConversationId.HasValue && request.ConversationId.Value != message.ConversationId)
                throw new ValidationException("Tin nhắn không thuộc hội thoại đã chọn.", "MESSAGE_CONVERSATION_MISMATCH");
        }

        var conversationId = request.ConversationId ?? message?.ConversationId;
        if (!conversationId.HasValue)
            return (null, null);

        var conversation = await _chatRepo.GetDetailAsync(conversationId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy hội thoại.", "CONVERSATION_NOT_FOUND");

        if (conversation.BuyerId != reporterId && conversation.SellerId != reporterId)
            throw new ForbiddenException(
                "Bạn chỉ có thể báo cáo trong hội thoại mà bạn tham gia.",
                "NOT_CONVERSATION_PARTICIPANT");

        return (conversation, message);
    }

    /// <summary>
    /// Chống spam: mỗi người chỉ report 1 lần cho mỗi đối tượng.
    /// Khi báo cáo theo MessageId cụ thể thì cho phép report nhiều tin nhắn vi
    /// phạm khác nhau trong cùng hội thoại, nhưng không report trùng cùng 1 tin.
    /// </summary>
    private async Task EnsureNotDuplicateAsync(
        Guid reporterId,
        Item? item,
        Transaction? transaction,
        Conversation? conversation,
        Message? reportedMessage,
        CancellationToken ct)
    {
        var txId = transaction?.Id;
        var itemId = item?.Id;
        var messageId = reportedMessage?.Id;
        var convId = messageId is null ? conversation?.Id : null;

        var existing = await _reportRepo.ListAsync(
            r => r.ReporterId == reporterId
                 && ((txId != null && r.TransactionId == txId)
                     || (itemId != null && r.ItemId == itemId)
                     || (messageId != null && r.MessageId == messageId)
                     || (convId != null && r.ConversationId == convId && r.MessageId == null)),
            ct);

        if (existing.Count > 0)
            throw new ConflictException(
                "Bạn đã gửi báo cáo cho đối tượng này rồi. Hệ thống đang xử lý.",
                "DUPLICATE_REPORT");
    }

    private async Task<string?> GetUserNameAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        return user?.FullName;
    }

    private static MyReportDto MapDto(Report report, string? itemTitle, string? reportedUserName) => new()
    {
        Id = report.Id,
        Reason = report.Reason,
        ReasonName = ReportLabels.ReasonName(report.Reason),
        Status = report.Status,
        StatusName = ReportLabels.StatusName(report.Status),
        Description = report.Description,
        AdminNote = report.AdminNote,
        ItemTitle = itemTitle,
        ReportedUserName = reportedUserName,
        MessageId = report.MessageId,
        MessageEvidence = report.MessageEvidence,
        CreatedAt = report.CreatedAt,
        ResolvedAt = report.ResolvedAt
    };
}
