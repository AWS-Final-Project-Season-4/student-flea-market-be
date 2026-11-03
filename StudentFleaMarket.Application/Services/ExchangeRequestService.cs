using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Helpers;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Application.DTOs.Trading;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Services;

/// <summary>
/// Xử lý yêu cầu đổi đồ (Exchange Request).
///
/// QUY TẮC NGHIỆP VỤ
/// -----------------
/// 0. Người gửi nhập MÃ HÀNG (ItemCode, 8 ký tự) của 2 món đồ — không nhập Id.
///    Service tra cứu mã -> Item, rồi mới áp các luật bên dưới.
/// 1. Người gửi yêu cầu (Requester) phải là CHỦ của OfferedItem.
/// 2. Món đồ được nhận (RequestedItem) phải:
///    - tồn tại, còn "đang bán",
///    - BẬT cờ "cho phép đổi đồ" (IsExchangeEnabled),
///    - thuộc sở hữu của người KHÁC (không tự đổi với chính mình).
/// 3. Món đồ đem đổi (OfferedItem) phải là của người gửi và còn "đang bán".
/// 4. Chỉ CHỦ của RequestedItem mới được accept / reject.
/// 5. Mỗi món đồ chỉ có tối đa 1 yêu cầu đang chờ — tránh nhiều người
///    cùng đề nghị đổi lấy 1 món.
/// 6. Yêu cầu đã trả lời (Accepted / Rejected / Cancelled) thì không
///    thao tác lại được — tránh trạng thái mâu thuẫn.
/// </summary>
public class ExchangeRequestService : IExchangeRequestService
{
    private readonly IExchangeRequestRepository _erRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUserRepository _userRepo;
    private readonly IEmailSender _emailSender;

    public ExchangeRequestService(
        IExchangeRequestRepository erRepo,
        IItemRepository itemRepo,
        IUserRepository userRepo,
        IEmailSender emailSender)
    {
        _erRepo = erRepo;
        _itemRepo = itemRepo;
        _userRepo = userRepo;
        _emailSender = emailSender;
    }

    // =====================================================================
    // CREATE
    // =====================================================================
    public async Task<ExchangeRequestDto> CreateAsync(
        Guid requesterId,
        CreateExchangeRequestDto request,
        CancellationToken ct = default)
    {
        var requester = await _userRepo.GetByIdAsync(requesterId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (requester.Role == (int)UserRole.AiService)
            throw new ForbiddenException(
                "Tài khoản dịch vụ AI không được gửi yêu cầu đổi đồ.",
                "AI_CANNOT_EXCHANGE");

        // ----- Món đồ được nhận (của bên kia) -----
        // Tra cứu theo MÃ HÀNG (ItemCode) mà người dùng nhập tay, không theo Id.
        // Dùng GetDetailAsync (có Include Seller) để lấy được thông tin
        // người bán cho việc gửi thông báo.
        var requestedItem = await _itemRepo.GetByCodeAsync(request.RequestedItemCode, ct)
            ?? throw new NotFoundException(
                $"Không tìm thấy món đồ có mã \"{ItemCodeHelper.Normalize(request.RequestedItemCode)}\".",
                "ITEM_CODE_NOT_FOUND");

        if (requestedItem.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ này không còn đang bán nên không thể đề nghị đổi.",
                "ITEM_NOT_AVAILABLE");

        if (!requestedItem.IsExchangeEnabled)
            throw new ValidationException(
                "Chủ món đồ không bật cho phép đổi đồ.",
                "ITEM_NOT_EXCHANGEABLE");

        if (requestedItem.SellerId == requesterId)
            throw new ValidationException(
                "Không thể gửi yêu cầu đổi lấy chính món đồ của bạn.",
                "CANNOT_EXCHANGE_OWN_ITEM");

        // ----- Món đồ đem đổi (của người gửi) -----
        var offeredItem = await _itemRepo.GetByCodeAsync(request.OfferedItemCode, ct)
            ?? throw new NotFoundException(
                $"Không tìm thấy món đồ có mã \"{ItemCodeHelper.Normalize(request.OfferedItemCode)}\".",
                "ITEM_CODE_NOT_FOUND");

        if (offeredItem.SellerId != requesterId)
            throw new ForbiddenException(
                "Bạn chỉ có thể đề nghị đổi bằng món đồ của chính mình.",
                "NOT_ITEM_OWNER");

        if (offeredItem.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ đem đổi không còn đang bán.",
                "ITEM_NOT_AVAILABLE");

        // ----- Trùng yêu cầu -----
        if (await _erRepo.ExistsPendingAsync(requestedItem.Id, offeredItem.Id, ct))
            throw new ConflictException(
                "Bạn đã gửi yêu cầu đổi cho món đồ này rồi. Hãy chờ phản hồi.",
                "EXCHANGE_REQUEST_ALREADY_EXISTS");

        // ----- Món đồ đã có người khác đề nghị đổi chưa -----
        if (await _erRepo.HasPendingRequestForItemAsync(requestedItem.Id, ct))
            throw new ConflictException(
                "Món đồ này hiện đã có một yêu cầu đổi đang chờ xử lý.",
                "ITEM_ALREADY_HAS_EXCHANGE_REQUEST");

        var exchangeRequest = new ExchangeRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            RequestedItemId = requestedItem.Id,
            OfferedItemId = offeredItem.Id,
            AdditionalAmount = request.AdditionalAmount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = (int)ExchangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _erRepo.AddAsync(exchangeRequest, ct);
        await _erRepo.SaveChangesAsync(ct);

        // Thông báo cho chủ món đồ được nhận.
        await SendMailSafeAsync(
            requestedItem.Seller?.Email,
            requestedItem.Seller?.FullName,
            "Bạn nhận được yêu cầu đổi đồ mới",
            BuildRequestMailBody(requester, requestedItem, offeredItem, request.AdditionalAmount, request.Note),
            ct);

        // Nạp lại để có đầy đủ quan hệ (Requester, RequestedItem, OfferedItem)
        // cho DTO trả về.
        return await BuildDtoAsync(exchangeRequest.Id, requesterId, ct);
    }

    // =====================================================================
    // GET
    // =====================================================================
    public async Task<ExchangeRequestDto> GetByIdAsync(
        Guid exchangeRequestId,
        Guid viewerId,
        CancellationToken ct = default)
    {
        var er = await _erRepo.GetDetailAsync(exchangeRequestId, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu đổi đồ.", "EXCHANGE_REQUEST_NOT_FOUND");

        EnsureParticipant(er, viewerId);

        return MapDto(er, viewerId);
    }

    public async Task<PagedResult<ExchangeRequestDto>> ListAsync(
        Guid userId,
        ExchangeRequestQueryDto query,
        CancellationToken ct = default)
    {
        var (requests, total) = await _erRepo.ListForUserAsync(userId, query, ct);

        return new PagedResult<ExchangeRequestDto>
        {
            Items = requests.Select(er => MapDto(er, userId)).ToList(),
            TotalCount = total,
            Page = query.Page < 1 ? 1 : query.Page,
            PageSize = query.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => query.PageSize
            }
        };
    }

    // =====================================================================
    // ACCEPT / REJECT / CANCEL
    // =====================================================================
    public async Task<ExchangeRequestDto> AcceptAsync(
        Guid userId,
        Guid exchangeRequestId,
        RespondExchangeRequestDto? request,
        CancellationToken ct = default)
    {
        var er = await GetTrackedAsync(exchangeRequestId, ct);

        // Chỉ chủ món đồ được nhận mới được accept.
        if (er.RequestedItem.SellerId != userId)
            throw new ForbiddenException(
                "Chỉ chủ món đồ được nhận mới có thể chấp nhận yêu cầu này.",
                "NOT_EXCHANGE_REQUEST_OWNER");

        EnsurePending(er);

        // Món đồ đem đổi phải còn "đang bán" — nếu chủ nó đã gỡ bán thì
        // yêu cầu không còn hiệu lực.
        if (er.OfferedItem.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ đem đổi của người gửi không còn đang bán.",
                "OFFERED_ITEM_NOT_AVAILABLE");

        er.Status = (int)ExchangeRequestStatus.Accepted;
        er.RespondedAt = DateTime.UtcNow;
        er.ResponseNote = string.IsNullOrWhiteSpace(request?.Note) ? null : request.Note.Trim();

        _erRepo.Update(er);
        await _erRepo.SaveChangesAsync(ct);

        await SendMailSafeAsync(
            er.Requester?.Email,
            er.Requester?.FullName,
            "Yêu cầu đổi đồ của bạn đã được chấp nhận",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(er.Requester?.FullName ?? "")}</b>,</p>
                <p>Yêu cầu đổi đồ của bạn đã được <b>chấp nhận</b>.</p>
                <p>Hãy vào mục <b>Giao dịch</b> để khởi tạo đơn hoàn tất đổi đồ.</p>",
            ct);

        return await BuildDtoAsync(er.Id, userId, ct);
    }

    public async Task<ExchangeRequestDto> RejectAsync(
        Guid userId,
        Guid exchangeRequestId,
        RespondExchangeRequestDto? request,
        CancellationToken ct = default)
    {
        var er = await GetTrackedAsync(exchangeRequestId, ct);

        if (er.RequestedItem.SellerId != userId)
            throw new ForbiddenException(
                "Chỉ chủ món đồ được nhận mới có thể từ chối yêu cầu này.",
                "NOT_EXCHANGE_REQUEST_OWNER");

        EnsurePending(er);

        er.Status = (int)ExchangeRequestStatus.Rejected;
        er.RespondedAt = DateTime.UtcNow;
        er.ResponseNote = string.IsNullOrWhiteSpace(request?.Note) ? null : request.Note.Trim();

        _erRepo.Update(er);
        await _erRepo.SaveChangesAsync(ct);

        await SendMailSafeAsync(
            er.Requester?.Email,
            er.Requester?.FullName,
            "Yêu cầu đổi đồ của bạn đã bị từ chối",
            $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(er.Requester?.FullName ?? "")}</b>,</p>
                <p>Yêu cầu đổi đồ của bạn đã bị <b>từ chối</b>.</p>
                <p>Bạn có thể đề nghị trao đổi với món đồ khác.</p>",
            ct);

        return await BuildDtoAsync(er.Id, userId, ct);
    }

    public async Task<ExchangeRequestDto> CancelAsync(
        Guid userId,
        Guid exchangeRequestId,
        CancellationToken ct = default)
    {
        var er = await GetTrackedAsync(exchangeRequestId, ct);

        // Chỉ người GỬI mới hủy được yêu cầu của mình.
        if (er.RequesterId != userId)
            throw new ForbiddenException(
                "Chỉ người gửi yêu cầu mới có thể hủy yêu cầu này.",
                "NOT_EXCHANGE_REQUEST_OWNER");

        EnsurePending(er);

        er.Status = (int)ExchangeRequestStatus.Cancelled;
        er.RespondedAt = DateTime.UtcNow;

        _erRepo.Update(er);
        await _erRepo.SaveChangesAsync(ct);

        return await BuildDtoAsync(er.Id, userId, ct);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Lấy yêu cầu theo id (tracked) kèm quan hệ cần thiết để kiểm tra quyền.
    /// </summary>
    private async Task<ExchangeRequest> GetTrackedAsync(Guid id, CancellationToken ct)
    {
        var er = await _erRepo.GetTrackedDetailAsync(id, ct);
        if (er is null)
            throw new NotFoundException("Không tìm thấy yêu cầu đổi đồ.", "EXCHANGE_REQUEST_NOT_FOUND");

        return er;
    }

    private static void EnsurePending(ExchangeRequest er)
    {
        if ((ExchangeRequestStatus)er.Status != ExchangeRequestStatus.Pending)
            throw new ConflictException(
                $"Yêu cầu này đã ở trạng thái \"{ExchangeRequestLabels.StatusName(er.Status)}\" nên không thể thao tác lại.",
                "EXCHANGE_REQUEST_NOT_PENDING");
    }

    private static void EnsureParticipant(ExchangeRequest er, Guid viewerId)
    {
        if (er.RequesterId != viewerId && er.RequestedItem?.SellerId != viewerId)
            throw new ForbiddenException(
                "Bạn không có quyền xem yêu cầu đổi đồ này.",
                "NOT_EXCHANGE_REQUEST_PARTICIPANT");
    }

    private async Task<ExchangeRequestDto> BuildDtoAsync(Guid id, Guid viewerId, CancellationToken ct)
    {
        var er = await _erRepo.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu đổi đồ.", "EXCHANGE_REQUEST_NOT_FOUND");

        return MapDto(er, viewerId);
    }

    private static ExchangeRequestDto MapDto(ExchangeRequest er, Guid viewerId)
    {
        var requestedSellerId = er.RequestedItem?.SellerId ?? Guid.Empty;
        var isPending = (ExchangeRequestStatus)er.Status == ExchangeRequestStatus.Pending;

        return new ExchangeRequestDto
        {
            Id = er.Id,
            RequesterId = er.RequesterId,
            RequesterName = er.Requester?.FullName ?? "",
            RequestedItem = MapItem(er.RequestedItem),
            OfferedItem = MapItem(er.OfferedItem),
            AdditionalAmount = er.AdditionalAmount,
            Note = er.Note,
            ResponseNote = er.ResponseNote,
            Status = er.Status,
            StatusName = ExchangeRequestLabels.StatusName(er.Status),
            CreatedAt = er.CreatedAt,
            RespondedAt = er.RespondedAt,
            // Chỉ khi yêu cầu còn "đang chờ" thì chủ tin mới có thể accept / reject.
            CanRespond = isPending && requestedSellerId == viewerId && er.RequesterId != viewerId,
            // Tương tự, chỉ người gửi mới hủy được khi yêu cầu còn chờ.
            CanCancel = isPending && er.RequesterId == viewerId
        };
    }

    private static TradingItemDto MapItem(Item? item)
    {
        if (item is null)
            return new TradingItemDto
            {
                Id = Guid.Empty,
                Title = "",
                Slug = "",
                ItemCode = "",
                Location = "",
                SellerId = Guid.Empty,
                SellerName = ""
            };

        var primary = item.Images
            .OrderBy(i => i.IsPrimary ? 0 : 1)
            .ThenBy(i => i.Order)
            .FirstOrDefault();

        return new TradingItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            ItemCode = item.ItemCode,
            Price = item.Price,
            PrimaryImageUrl = primary?.ImageUrl,
            Location = item.Location,
            SellerId = item.SellerId,
            SellerName = item.Seller?.FullName ?? ""
        };
    }

    private static string BuildRequestMailBody(
        ApplicationUser requester,
        Item requestedItem,
        Item offeredItem,
        decimal additionalAmount,
        string? note)
    {
        var noteHtml = string.IsNullOrWhiteSpace(note)
            ? string.Empty
            : $"<p><b>Ghi chú:</b> {System.Net.WebUtility.HtmlEncode(note.Trim())}</p>";

        return $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(requestedItem.Seller?.FullName ?? "")}</b>,</p>
            <p><b>{System.Net.WebUtility.HtmlEncode(requester.FullName)}</b> muốn đề nghị đổi đồ với bạn:</p>
            <ul>
                <li>Bạn nhận: <b>{System.Net.WebUtility.HtmlEncode(requestedItem.Title)}</b>
                    — mã hàng: <b>{System.Net.WebUtility.HtmlEncode(requestedItem.ItemCode)}</b></li>
                <li>Người gửi đề nghị: <b>{System.Net.WebUtility.HtmlEncode(offeredItem.Title)}</b>
                    — mã hàng: <b>{System.Net.WebUtility.HtmlEncode(offeredItem.ItemCode)}</b></li>
                <li>Tiền bù chênh lệch: <b>{additionalAmount:N0} đ</b></li>
            </ul>
            {noteHtml}
            <p>Hãy vào mục <b>Yêu cầu đổi đồ</b> để phản hồi.</p>";
    }

    /// <summary>
    /// Gửi mail nhưng nuốt lỗi — lỗi SMTP không được làm hỏng luồng nghiệp vụ.
    /// </summary>
    private async Task SendMailSafeAsync(
        string? to,
        string? fullName,
        string subject,
        string body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        try
        {
            await _emailSender.SendAsync(to, subject, body, true, ct);
        }
        catch (Exception)
        {
            // Bỏ qua — nghiệp vụ chính đã được ghi nhận vào DB.
        }
    }
}
