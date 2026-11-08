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
/// Xử lý đơn giao dịch (Transaction) & thanh toán (Payment).
///
/// QUY TẮC NGHIỆP VỤ
/// -----------------
/// 1. Mua trực tiếp (Purchase):
///    - Chỉ mua được tin đăng đang "đang bán" và không phải của chính mình.
///    - Mỗi tin chỉ có 1 đơn đang xử lý tại một thời điểm.
///    - Số tiền mặc định = giá bán của tin đăng.
/// 2. Đổi đồ (Exchange):
///    - Chỉ tạo được từ một yêu cầu đổi đồ đã được ACCEPT.
///    - 1 yêu cầu -> tối đa 1 đơn giao dịch.
///    - Số tiền = AdditionalAmount của yêu cầu (tiền bù chênh lệch).
/// 3. Vòng đời đơn:
///    - Pending (Đang xử lý) -> Completed (Hoàn thành) -> tin đánh dấu "Đã bán".
///    - Pending (Đang xử lý) -> Cancelled (Hủy)        -> tin trở lại "Đang bán".
///    - Chỉ 1 trong 2 bên liên quan được complete / cancel đơn.
/// 4. Thanh toán:
///    - COD (1): trả tiền mặt khi gặp nhận đồ.
///    - BankTransfer (2): chuyển khoản qua QR ngân hàng / cổng thanh toán.
///    - Mỗi đơn có tối đa 1 Payment (quan hệ 1-1).
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _txRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IExchangeRequestRepository _erRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUserRepository _userRepo;
    private readonly IEmailSender _emailSender;
    private readonly ITransactionConfirmationRepository _confirmRepo;

    public TransactionService(
        ITransactionRepository txRepo,
        IPaymentRepository paymentRepo,
        IExchangeRequestRepository erRepo,
        IItemRepository itemRepo,
        IUserRepository userRepo,
        IEmailSender emailSender,
        ITransactionConfirmationRepository confirmRepo)
    {
        _txRepo = txRepo;
        _paymentRepo = paymentRepo;
        _erRepo = erRepo;
        _itemRepo = itemRepo;
        _userRepo = userRepo;
        _emailSender = emailSender;
        _confirmRepo = confirmRepo;
    }

    // =====================================================================
    // CREATE — MUA TRỰC TIẾP
    // =====================================================================
    public async Task<TransactionDto> CreatePurchaseAsync(
        Guid initiatorId,
        CreatePurchaseTransactionDto request,
        CancellationToken ct = default)
    {
        var initiator = await _userRepo.GetByIdAsync(initiatorId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (initiator.Role == (int)UserRole.AiService)
            throw new ForbiddenException(
                "Tài khoản dịch vụ AI không được tạo đơn giao dịch.",
                "AI_CANNOT_TRANSACT");

        var item = await _itemRepo.GetByCodeAsync(request.ItemCode, ct)
            ?? throw new NotFoundException(
                $"Không tìm thấy tin đăng có mã \"{ItemCodeHelper.Normalize(request.ItemCode)}\".",
                "ITEM_CODE_NOT_FOUND");

        if (item.SellerId == initiatorId)
            throw new ValidationException(
                "Bạn không thể mua chính tin đăng của mình.",
                "CANNOT_BUY_OWN_ITEM");

        if (item.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Tin đăng này không còn đang bán.",
                "ITEM_NOT_AVAILABLE");

        // Mỗi tin chỉ có 1 đơn đang xử lý — tránh 2 người cùng mua.
        if (await _txRepo.HasPendingTransactionForItemAsync(item.Id, ct))
            throw new ConflictException(
                "Tin đăng này đang có một đơn giao dịch khác được xử lý.",
                "ITEM_ALREADY_HAS_TRANSACTION");

        var counterpartyId = item.SellerId;
        var amount = request.Amount ?? item.Price;

        // Tin giá 0 -> giao dịch "nhận miễn phí", không cần Payment.
        var type = item.Price == 0 && request.Amount is null
            ? (int)TransactionType.Free
            : (int)TransactionType.Purchase;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            CounterpartyId = counterpartyId,
            ItemId = item.Id,
            ExchangeRequestId = null,
            Type = type,
            Amount = amount,
            Status = (int)TransactionStatus.Pending,
            MeetingLocation = string.IsNullOrWhiteSpace(request.MeetingLocation)
                ? item.Location
                : request.MeetingLocation.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _txRepo.AddAsync(transaction, ct);

        // Tạo Payment nếu có tiền (mua trực tiếp có tiền, nhận miễn phí thì không).
        if (type == (int)TransactionType.Purchase)
        {
            await _paymentRepo.AddAsync(new Payment
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                PayerId = initiatorId,
                Amount = amount,
                Method = request.PaymentMethod,
                Status = (int)PaymentStatus.Pending,
                TransactionRef = BuildTransactionRef(transaction.Id),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        await _txRepo.SaveChangesAsync(ct);

        await NotifyCounterpartyAsync(
            item.Seller?.Email,
            item.Seller?.FullName,
            initiator.FullName,
            item.Title,
            type,
            ct,
            itemCode: item.ItemCode);

        return await BuildDtoAsync(transaction.Id, initiatorId, ct);
    }

    // =====================================================================
    // CREATE — HOÀN TẤT ĐỔI ĐỒ
    // =====================================================================
    public async Task<TransactionDto> CreateExchangeAsync(
        Guid initiatorId,
        CreateExchangeTransactionDto request,
        CancellationToken ct = default)
    {
        var initiator = await _userRepo.GetByIdAsync(initiatorId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (initiator.Role == (int)UserRole.AiService)
            throw new ForbiddenException(
                "Tài khoản dịch vụ AI không được tạo đơn giao dịch.",
                "AI_CANNOT_TRANSACT");

        var er = await _erRepo.GetDetailAsync(request.ExchangeRequestId, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu đổi đồ.", "EXCHANGE_REQUEST_NOT_FOUND");

        // Chỉ người gửi yêu cầu mới khởi tạo được đơn đổi đồ.
        if (er.RequesterId != initiatorId)
            throw new ForbiddenException(
                "Chỉ người gửi yêu cầu mới có thể khởi tạo đơn đổi đồ.",
                "NOT_EXCHANGE_REQUEST_OWNER");

        if ((ExchangeRequestStatus)er.Status != ExchangeRequestStatus.Accepted)
            throw new ValidationException(
                "Yêu cầu đổi đồ chưa được chấp nhận nên chưa thể tạo đơn giao dịch.",
                "EXCHANGE_REQUEST_NOT_ACCEPTED");

        // 1 yêu cầu -> tối đa 1 đơn.
        if (await _txRepo.GetByExchangeRequestAsync(er.Id, ct) is not null)
            throw new ConflictException(
                "Yêu cầu đổi đồ này đã có đơn giao dịch.",
                "TRANSACTION_ALREADY_EXISTS");

        var requestedItem = er.RequestedItem;

        // Món đồ được nhận phải còn "đang bán".
        if (requestedItem.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ cần nhận không còn đang bán.",
                "ITEM_NOT_AVAILABLE");

        // Món đồ đem đổi phải còn "đang bán".
        if (er.OfferedItem.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Món đồ đem đổi không còn đang bán.",
                "OFFERED_ITEM_NOT_AVAILABLE");

        if (await _txRepo.HasPendingTransactionForItemAsync(requestedItem.Id, ct))
            throw new ConflictException(
                "Món đồ này đang có một đơn giao dịch khác được xử lý.",
                "ITEM_ALREADY_HAS_TRANSACTION");

        var amount = request.Amount ?? er.AdditionalAmount;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            // Bên còn lại là chủ món đồ được nhận.
            CounterpartyId = requestedItem.SellerId,
            ItemId = requestedItem.Id,
            ExchangeRequestId = er.Id,
            Type = (int)TransactionType.Exchange,
            Amount = amount,
            Status = (int)TransactionStatus.Pending,
            MeetingLocation = string.IsNullOrWhiteSpace(request.MeetingLocation)
                ? requestedItem.Location
                : request.MeetingLocation.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _txRepo.AddAsync(transaction, ct);

        // Tiền bù chênh lệch = 0 -> không cần Payment.
        if (amount > 0)
        {
            await _paymentRepo.AddAsync(new Payment
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                PayerId = initiatorId,
                Amount = amount,
                Method = request.PaymentMethod,
                Status = (int)PaymentStatus.Pending,
                TransactionRef = BuildTransactionRef(transaction.Id),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        await _txRepo.SaveChangesAsync(ct);

        await NotifyCounterpartyAsync(
            requestedItem.Seller?.Email,
            requestedItem.Seller?.FullName,
            initiator.FullName,
            requestedItem.Title,
            (int)TransactionType.Exchange,
            ct,
            itemCode: requestedItem.ItemCode);

        return await BuildDtoAsync(transaction.Id, initiatorId, ct);
    }

    // =====================================================================
    // GET
    // =====================================================================
    public async Task<TransactionDto> GetByIdAsync(
        Guid transactionId,
        Guid viewerId,
        CancellationToken ct = default)
    {
        var tx = await _txRepo.GetDetailAsync(transactionId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        EnsureParticipant(tx, viewerId);

        return MapDto(tx, viewerId);
    }

    public async Task<PagedResult<TransactionDto>> ListAsync(
        Guid userId,
        TransactionQueryDto query,
        CancellationToken ct = default)
    {
        var (transactions, total) = await _txRepo.ListForUserAsync(userId, query, ct);

        return new PagedResult<TransactionDto>
        {
            Items = transactions.Select(t => MapDto(t, userId)).ToList(),
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
    // VÒNG ĐỜI ĐƠN
    // =====================================================================
    public async Task<TransactionDto> CompleteAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default)
    {
        var tx = await GetTrackedAsync(transactionId, ct);

        EnsureParticipant(tx, userId);

        if ((TransactionStatus)tx.Status != TransactionStatus.Pending)
            throw new ConflictException(
                $"Đơn giao dịch đang ở trạng thái \"{TransactionLabels.StatusName(tx.Status)}\" nên không thể hoàn thành.",
                "TRANSACTION_NOT_PENDING");

        // Món đồ phải còn "đang bán" để hoàn tất giao dịch.
        var item = await _itemRepo.GetByIdAsync(tx.ItemId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        if (item.Status != (int)ItemStatus.Available)
            throw new ValidationException(
                "Tin đăng không còn ở trạng thái đang bán.",
                "ITEM_NOT_AVAILABLE");

        // ---------------------------------------------------------------
        // ĐƠN ĐỔI ĐỒ: BẮT BUỘC CẢ 2 BÊN XÁC NHẬN.
        //
        // Ở Exchange có 2 món đi ngược chiều. Nếu chỉ 1 bên bấm "Hoàn tất"
        // thì bên đó có thể chiếm đoạt món của mình mà không cần gặp mặt —
        // đồng thời tạo ra một đánh giá 5 sao giả. Vì vậy bước "hoàn thành"
        // chỉ được ghi nhận là ĐÃ XÁC NHẬN của 1 bên; đơn chuyển sang
        // AwaitingConfirmation và chờ bên kia xác nhận.
        //
        // Đơn mua trực tiếp / nhận miễn phí không có 2 món nên vẫn hoàn
        // thành ngay 1 bước như cũ.
        // ---------------------------------------------------------------
        if (tx.Type == (int)TransactionType.Exchange)
        {
            // KHÔNG ghi nhận xác nhận ở bước này.
            // Nút "Hoàn tất" chỉ nghĩa là "tôi đã gặp mặt và đồ đúng là của tôi",
            // chưa có gì để đối chiếu mã hàng. Việc xác nhận bàn giao (kèm
            // kiểm tra mã hàng) phải là hành động riêng của MỖI bên qua
            // endpoint /handover — nếu ghi nhận ở đây thì bên bấm "Hoàn tất"
            // sẽ tự động "xác nhận đúng món" mà không hề kiểm tra mã.
            tx.Status = (int)TransactionStatus.AwaitingConfirmation;

            _txRepo.Update(tx);
            await _txRepo.SaveChangesAsync(ct);

            await NotifyCounterpartyAsync(
                tx.Counterparty?.Email,
                tx.Counterparty?.FullName,
                tx.Initiator?.FullName ?? "",
                item.Title,
                tx.Type,
                ct,
                subject: "Giao dịch đổi đồ đang chờ bạn xác nhận",
                intro: "Bạn đã xác nhận giao dịch đổi đồ với món đồ <b>{0}</b>. Vui lòng xác nhận bàn giao để hoàn tất giao dịch.",
                itemCode: item.ItemCode);

            return await BuildDtoAsync(tx.Id, userId, ct);
        }

        tx.Status = (int)TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;

        _txRepo.Update(tx);
        await _txRepo.SaveChangesAsync(ct);

        // Đánh dấu tin đã bán + đính chính tên chủ tin (đổi chủ khi đổi đồ).
        await MarkItemSoldAsync(item, tx, ct);

        // Với đơn đổi đồ: món đồ đem đổi cũng chuyển sang "đã bán"
        // (đã trao đổi xong -> không còn trên sàn).
        if (tx.ExchangeRequestId.HasValue)
        {
            var er = await _erRepo.GetDetailAsync(tx.ExchangeRequestId.Value, ct);
            if (er?.OfferedItem is not null)
            {
                var offered = await _itemRepo.GetByIdAsync(er.OfferedItem.Id, ct);
                if (offered is not null && offered.Status == (int)ItemStatus.Available)
                {
                    offered.Status = (int)ItemStatus.Sold;
                    offered.UpdatedAt = DateTime.UtcNow;
                    _itemRepo.Update(offered);
                    await _itemRepo.SaveChangesAsync(ct);
                }
            }
        }

        await NotifyCounterpartyAsync(
            tx.Counterparty?.Email,
            tx.Counterparty?.FullName,
            tx.Initiator?.FullName ?? "",
            item.Title,
            tx.Type,
            ct,
            subject: "Giao dịch đã hoàn thành",
            intro: "Giao dịch với món đồ <b>{0}</b> đã được đánh dấu hoàn thành.",
            itemCode: item.ItemCode);

        return await BuildDtoAsync(tx.Id, userId, ct);
    }

    // =====================================================================
    // XÁC NHẬN BÀN GIAO (đơn đổi đồ)
    // =====================================================================
    public async Task<HandoverConfirmationDto> ConfirmHandoverAsync(
        Guid userId,
        Guid transactionId,
        ConfirmHandoverDto? request,
        CancellationToken ct = default)
    {
        var tx = await GetTrackedAsync(transactionId, ct);

        EnsureParticipant(tx, userId);

        if (tx.Type != (int)TransactionType.Exchange)
            throw new ValidationException(
                "Chỉ giao dịch đổi đồ mới cần xác nhận bàn giao.",
                "NOT_EXCHANGE_TRANSACTION");

        if ((TransactionStatus)tx.Status is not (TransactionStatus.Pending or TransactionStatus.AwaitingConfirmation))
            throw new ConflictException(
                $"Đơn giao dịch đang ở trạng thái \"{TransactionLabels.StatusName(tx.Status)}\" nên không thể xác nhận.",
                "TRANSACTION_NOT_PENDING");

        // ---------------------------------------------------------------
        // MÓN ĐỒ ĐÁNG LẼ BÊN NÀY PHẢI NHẬN.
        //
        // VỚI ĐƠN ĐỔI ĐỒ có 2 món đi ngược chiều:
        //   - tx.Item      = món của BÊN KIA mang đến cho mình (người nhận yêu cầu).
        //   - OfferedItem  = món của CHÍNH MÌNH mang đến cho bên kia.
        //
        // Nên mỗi bên phải nhận món CỦA BÊN KIA. Nếu dùng chung tx.Item cho
        // cả 2 bên thì bên nhận sẽ phải nhập mã của chính món mình mang
        // đi -> luôn khớp -> mất hết tác dụng phát hiện tráo hàng.
        // ---------------------------------------------------------------
        var expectedItem = await ResolveExpectedReceivedItemAsync(tx, userId, ct)
            ?? throw new NotFoundException("Không tìm thấy tin đăng.", "ITEM_NOT_FOUND");

        // Bên đã xác nhận rồi thì không xác nhận lại được (chống sửa nhiều lần).
        var existing = await _confirmRepo.GetByTransactionAndUserAsync(tx.Id, userId, ct);
        if (existing is not null)
            throw new ConflictException(
                "Bạn đã xác nhận giao dịch này rồi.",
                "ALREADY_CONFIRMED_HANDOVER");

        var receivedOk = request?.ItemReceivedOk ?? true;
        var enteredCode = string.IsNullOrWhiteSpace(request?.ItemCodeVerified)
            ? null
            : ItemCodeHelper.Normalize(request.ItemCodeVerified);

        // ---------------------------------------------------------------
        // ĐỐI CHIẾU MÃ HÀNG — phát hiện tráo hàng ngay tại chỗ.
        // Nếu mã nhập vào KHÔNG khớp mã món đáng lẽ phải nhận, thì bên này
        // đã mang món khác đến. Ta KHÔNG cho xác nhận "đã nhận đúng".
        // ---------------------------------------------------------------
        bool? codeMatched = null;

        if (enteredCode is not null)
        {
            codeMatched = string.Equals(enteredCode, expectedItem.ItemCode, StringComparison.OrdinalIgnoreCase);

            if (!codeMatched.Value && receivedOk)
                throw new ValidationException(
                    $"Mã hàng bạn nhập (\"{enteredCode}\") không khớp với món đồ đáng lẽ được nhận " +
                    $"(\"{expectedItem.ItemCode}\"). Có thể bạn đã nhận nhầm món hoặc bị trả hàng sai. " +
                    "Vui lòng đánh dấu là không đúng món để gửi báo cáo.",
                    "ITEM_CODE_MISMATCH");
        }

        await RecordConfirmationAsync(tx, userId, receivedOk, enteredCode, ct);

        // ---------------------------------------------------------------
        // Đếm số bên đã xác nhận -> quyết định đơn hoàn thành hay chờ.
        // ---------------------------------------------------------------
        var confirmations = await _confirmRepo.GetByTransactionAsync(tx.Id, ct);
        var confirmedCount = confirmations.Count;

        if (confirmedCount >= 2)
        {
            // Cả 2 bên đã xác nhận -> hoàn thành thật sự.
            tx.Status = (int)TransactionStatus.Completed;
            tx.CompletedAt = DateTime.UtcNow;

            _txRepo.Update(tx);
            await _txRepo.SaveChangesAsync(ct);

            var item = await _itemRepo.GetByIdAsync(tx.ItemId, ct);
            if (item is not null)
                await MarkItemSoldAsync(item, tx, ct);

            // Món đồ đem đổi cũng chuyển sang "đã bán".
            if (tx.ExchangeRequestId.HasValue)
            {
                var er = await _erRepo.GetDetailAsync(tx.ExchangeRequestId.Value, ct);
                if (er?.OfferedItem is not null)
                {
                    var offered = await _itemRepo.GetByIdAsync(er.OfferedItem.Id, ct);
                    if (offered is not null && offered.Status == (int)ItemStatus.Available)
                    {
                        offered.Status = (int)ItemStatus.Sold;
                        offered.UpdatedAt = DateTime.UtcNow;
                        _itemRepo.Update(offered);
                        await _itemRepo.SaveChangesAsync(ct);
                    }
                }
            }

            await NotifyCounterpartyAsync(
                tx.Counterparty?.Email,
                tx.Counterparty?.FullName,
                tx.Initiator?.FullName ?? "",
                tx.Item?.Title ?? "",
                tx.Type,
                ct,
                subject: "Giao dịch đổi đồ đã hoàn thành",
                intro: "Cả hai bên đã xác nhận bàn giao món đồ <b>{0}</b>. Giao dịch hoàn thành, hai bên có thể đánh giá nhau.",
                itemCode: tx.Item?.ItemCode);
        }
        else
        {
            // Chỉ 1 bên xác nhận -> chờ bên kia.
            tx.Status = (int)TransactionStatus.AwaitingConfirmation;
            _txRepo.Update(tx);
            await _txRepo.SaveChangesAsync(ct);

            await NotifyCounterpartyAsync(
                tx.Counterparty?.Email,
                tx.Counterparty?.FullName,
                tx.Initiator?.FullName ?? "",
                tx.Item?.Title ?? "",
                tx.Type,
                ct,
                subject: "Giao dịch đổi đồ đang chờ bạn xác nhận",
                intro: "Bạn đã xác nhận giao dịch đổi đồ với món đồ <b>{0}</b>. Vui lòng xác nhận bàn giao để hoàn tất giao dịch.",
                itemCode: tx.Item?.ItemCode);
        }

        var result = await BuildHandoverDtoAsync(tx.Id, userId, ct);

        return result;
    }

    public async Task<HandoverConfirmationDto> GetHandoverStatusAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct = default)
    {
        var tx = await GetTrackedAsync(transactionId, ct);

        EnsureParticipant(tx, userId);

        return await BuildHandoverDtoAsync(tx.Id, userId, ct);
    }

    /// <summary>
    /// Ghi nhận xác nhận của một bên vào bảng TransactionConfirmations.
    /// </summary>
    private async Task RecordConfirmationAsync(
        Transaction tx,
        Guid userId,
        bool? itemReceivedOk,
        string? itemCodeVerified,
        CancellationToken ct)
    {
        var confirmation = new TransactionConfirmation
        {
            Id = Guid.NewGuid(),
            TransactionId = tx.Id,
            UserId = userId,
            ConfirmedAt = DateTime.UtcNow,
            ItemReceivedOk = itemReceivedOk ?? true,
            ItemCodeVerified = itemCodeVerified,
            Note = null
        };

        await _confirmRepo.AddAsync(confirmation, ct);
        await _confirmRepo.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Xác định món đồ mà bên <paramref name="userId"/> ĐÁNG LẼ phải nhận.
    ///
    /// Đơn Exchange có 2 món đi ngược chiều:
    ///   - tx.Item     = món của bên kia mang đến cho mình.
    ///   - OfferedItem = món của chính mình mang đến cho bên kia.
    ///
    /// Nên:
    ///   - Bên nhận yêu cầu (chủ RequestedItem) đáng lẽ nhận OfferedItem
    ///     (món của người gửi yêu cầu mang đến).
    ///   - Bên gửi yêu cầu đáng lẽ nhận RequestedItem (tx.Item).
    ///
    /// Nếu cả 2 bên dùng chung tx.Item thì mã hàng luôn khớp với chính món
    /// mình mang đi -> mất hết tác dụng phát hiện tráo hàng.
    /// </summary>
    private async Task<Item?> ResolveExpectedReceivedItemAsync(
        Transaction tx,
        Guid userId,
        CancellationToken ct)
    {
        if (tx.Type != (int)TransactionType.Exchange || !tx.ExchangeRequestId.HasValue)
        {
            // Đơn mua trực tiếp: chỉ có 1 món, và người mua là người nhận.
            return await _itemRepo.GetByIdAsync(tx.ItemId, ct);
        }

        var er = await _erRepo.GetDetailAsync(tx.ExchangeRequestId.Value, ct);
        if (er is null)
            return await _itemRepo.GetByIdAsync(tx.ItemId, ct);

        var offeredItem = er.OfferedItem;

        // Người gửi yêu cầu mang món của mình (OfferedItem) đến cho bên kia,
        // nên họ đáng lẽ phải nhận món của bên kia (RequestedItem = tx.Item).
        if (er.RequesterId == userId)
            return await _itemRepo.GetByIdAsync(tx.ItemId, ct);

        // Bên nhận yêu cầu mang món của bên kia (RequestedItem) đến,
        // nên họ đáng lẽ phải nhận OfferedItem.
        return offeredItem is null
            ? await _itemRepo.GetByIdAsync(tx.ItemId, ct)
            : await _itemRepo.GetByIdAsync(offeredItem.Id, ct);
    }

    private async Task<HandoverConfirmationDto> BuildHandoverDtoAsync(
        Guid transactionId,
        Guid viewerId,
        CancellationToken ct)
    {
        var tx = await _txRepo.GetDetailAsync(transactionId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        var confirmations = await _confirmRepo.GetByTransactionAsync(tx.Id, ct);

        var myConfirmation = confirmations.FirstOrDefault(c => c.UserId == viewerId);

        var expectedItem = await ResolveExpectedReceivedItemAsync(tx, viewerId, ct);

        bool? codeMatched = null;
        if (myConfirmation?.ItemCodeVerified is not null && expectedItem is not null)
            codeMatched = string.Equals(
                myConfirmation.ItemCodeVerified,
                expectedItem.ItemCode,
                StringComparison.OrdinalIgnoreCase);

        var isCompleted = (TransactionStatus)tx.Status == TransactionStatus.Completed;

        string? suggestion = null;
        if (myConfirmation is not null && !myConfirmation.ItemReceivedOk)
            suggestion = "Bạn đã đánh dấu món nhận được không đúng. Hãy gửi báo cáo để nhân viên xử lý.";
        else if (myConfirmation is null && !isCompleted)
            suggestion = "Hãy xác nhận bàn giao để hoàn tất giao dịch.";
        else if (isCompleted)
            suggestion = "Giao dịch đã hoàn thành. Hai bên có thể đánh giá nhau.";

        return new HandoverConfirmationDto
        {
            TransactionId = tx.Id,
            Status = tx.Status,
            StatusName = TransactionLabels.StatusName(tx.Status),
            ConfirmedCount = confirmations.Count,
            RequiredCount = 2,
            IsCompleted = isCompleted,
            ItemCodeMatched = codeMatched,
            ExpectedItemCode = expectedItem?.ItemCode,
            Suggestion = suggestion
        };
    }

    public async Task<TransactionDto> CancelAsync(
        Guid userId,
        Guid transactionId,
        CancelTransactionDto? request,
        CancellationToken ct = default)
    {
        var tx = await GetTrackedAsync(transactionId, ct);

        EnsureParticipant(tx, userId);

        if ((TransactionStatus)tx.Status != TransactionStatus.Pending)
            throw new ConflictException(
                $"Đơn giao dịch đang ở trạng thái \"{TransactionLabels.StatusName(tx.Status)}\" nên không thể hủy.",
                "TRANSACTION_NOT_PENDING");

        tx.Status = (int)TransactionStatus.Cancelled;
        tx.CancelledAt = DateTime.UtcNow;
        tx.CancelReason = string.IsNullOrWhiteSpace(request?.Reason) ? null : request.Reason.Trim();

        _txRepo.Update(tx);
        await _txRepo.SaveChangesAsync(ct);

        // Thanh toán đã xong nhưng đơn bị hủy -> hoàn tiền.
        var payment = await _paymentRepo.GetByTransactionAsync(tx.Id, ct);
        if (payment is not null && payment.Status == (int)PaymentStatus.Paid)
        {
            var trackedPayment = await _paymentRepo.GetByIdAsync(payment.Id, ct);
            if (trackedPayment is not null)
            {
                trackedPayment.Status = (int)PaymentStatus.Refunded;
                _paymentRepo.Update(trackedPayment);
                await _paymentRepo.SaveChangesAsync(ct);
            }
        }

        return await BuildDtoAsync(tx.Id, userId, ct);
    }

    // =====================================================================
    // THANH TOÁN
    // =====================================================================
    public async Task<PaymentDto> ConfirmPaymentAsync(
        Guid userId,
        Guid transactionId,
        ConfirmPaymentDto? request,
        CancellationToken ct = default)
    {
        var tx = await GetTrackedAsync(transactionId, ct);

        EnsureParticipant(tx, userId);

        // Chỉ bên trả tiền mới xác nhận được.
        if (tx.InitiatorId != userId)
            throw new ForbiddenException(
                "Chỉ người trả tiền mới có thể xác nhận thanh toán.",
                "NOT_PAYMENT_PAYER");

        if ((TransactionStatus)tx.Status != TransactionStatus.Pending)
            throw new ConflictException(
                $"Đơn giao dịch đang ở trạng thái \"{TransactionLabels.StatusName(tx.Status)}\" nên không thể xác nhận thanh toán.",
                "TRANSACTION_NOT_PENDING");

        var payment = await _paymentRepo.GetByTransactionAsync(tx.Id, ct)
            ?? throw new NotFoundException("Đơn giao dịch này không có thông tin thanh toán.", "PAYMENT_NOT_FOUND");

        if (payment.Status == (int)PaymentStatus.Paid)
            throw new ConflictException(
                "Giao dịch này đã được đánh dấu thanh toán rồi.",
                "PAYMENT_ALREADY_PAID");

        var trackedPayment = await _paymentRepo.GetByIdAsync(payment.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin thanh toán.", "PAYMENT_NOT_FOUND");

        trackedPayment.Status = (int)PaymentStatus.Paid;
        trackedPayment.PaidAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request?.TransactionRef))
            trackedPayment.TransactionRef = request.TransactionRef.Trim();

        var payerName = tx.Initiator?.FullName ?? "";

        _paymentRepo.Update(trackedPayment);
        await _paymentRepo.SaveChangesAsync(ct);

        // Đơn vẫn giữ trạng thái "Đang xử lý" — người mua xác nhận thanh toán
        // chưa đồng nghĩa giao dịch đã xong. Chỉ khi bên kia bấm "Hoàn thành"
        // thì đơn mới chuyển sang "Hoàn thành" (Completed) và tin đăng mới
        // được đánh dấu "Đã bán".
        await NotifyCounterpartyAsync(
            tx.Counterparty?.Email,
            tx.Counterparty?.FullName,
            tx.Initiator?.FullName ?? "",
            tx.Item?.Title ?? "",
            tx.Type,
            ct,
            subject: "Đơn giao dịch đã được thanh toán",
            intro: "Người mua <b>{0}</b> đã thanh toán cho món đồ <b>{1}</b>.");

        return MapPayment(trackedPayment, payerName);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task<Transaction> GetTrackedAsync(Guid id, CancellationToken ct)
    {
        var tx = await _txRepo.GetTrackedDetailAsync(id, ct);
        if (tx is null)
            throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        return tx;
    }

    private static void EnsureParticipant(Transaction tx, Guid userId)
    {
        if (tx.InitiatorId != userId && tx.CounterpartyId != userId)
            throw new ForbiddenException(
                "Bạn không có quyền thao tác lên đơn giao dịch này.",
                "NOT_TRANSACTION_PARTICIPANT");
    }

    /// <summary>
    /// Đánh dấu tin đăng "đã bán" khi giao dịch hoàn tất.
    /// Với đơn đổi đồ: món đồ được nhận chuyển sang chủ mới (người gửi yêu cầu)
    /// để giữ đúng ý nghĩa "đã trao đổi xong".
    /// </summary>
    private async Task MarkItemSoldAsync(Item item, Transaction tx, CancellationToken ct)
    {
        item.Status = (int)ItemStatus.Sold;
        item.UpdatedAt = DateTime.UtcNow;

        // Đơn đổi đồ: tin đổi chủ sang người gửi yêu cầu (người nhận món này).
        if (tx.Type == (int)TransactionType.Exchange && tx.ExchangeRequestId.HasValue)
        {
            var er = await _erRepo.GetDetailAsync(tx.ExchangeRequestId.Value, ct);
            if (er is not null)
                item.SellerId = er.RequesterId;
        }

        _itemRepo.Update(item);
        await _itemRepo.SaveChangesAsync(ct);
    }
    private async Task<TransactionDto> BuildDtoAsync(Guid id, Guid viewerId, CancellationToken ct)
    {
        var tx = await _txRepo.GetDetailAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn giao dịch.", "TRANSACTION_NOT_FOUND");

        return MapDto(tx, viewerId);
    }

    private static TransactionDto MapDto(Transaction tx, Guid viewerId)
    {
        var offeredItem = tx.ExchangeRequest?.OfferedItem;

        return new TransactionDto
        {
            Id = tx.Id,
            Type = tx.Type,
            TypeName = TransactionLabels.TypeName(tx.Type),
            Status = tx.Status,
            StatusName = TransactionLabels.StatusName(tx.Status),
            Initiator = MapParty(tx.Initiator),
            Counterparty = MapParty(tx.Counterparty),
            Item = MapItem(tx.Item),
            OfferedItem = offeredItem is null ? null : MapItem(offeredItem),
            ExchangeRequestId = tx.ExchangeRequestId,
            Amount = tx.Amount,
            MeetingLocation = tx.MeetingLocation,
            Payment = tx.Payment is null ? null : MapPayment(tx.Payment, tx.Payment.Payer?.FullName ?? ""),
            CreatedAt = tx.CreatedAt,
            CompletedAt = tx.CompletedAt,
            CancelledAt = tx.CancelledAt,
            CancelReason = tx.CancelReason,
            // Chỉ được thao tác khi đơn còn ở trạng thái "đang xử lý".
            // Với đơn Exchange đã có 1 bên xác nhận (AwaitingConfirmation) thì
            // bên còn lại VẪN được bấm xác nhận — nên cần cho phép.
            CanManage = (tx.Status == (int)TransactionStatus.Pending
                        || tx.Status == (int)TransactionStatus.AwaitingConfirmation)
                        && (tx.InitiatorId == viewerId || tx.CounterpartyId == viewerId)
        };
    }

    private static TradingPartyDto MapParty(ApplicationUser? user) => new()
    {
        Id = user?.Id ?? Guid.Empty,
        FullName = user?.FullName ?? "",
        AvatarUrl = user?.AvatarUrl,
        PhoneNumber = user?.PhoneNumber
    };

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

    private static PaymentDto MapPayment(Payment payment, string payerName) => new()
    {
        Id = payment.Id,
        Method = payment.Method,
        MethodName = TransactionLabels.PaymentMethodName(payment.Method),
        Status = payment.Status,
        StatusName = TransactionLabels.PaymentStatusName(payment.Status),
        Amount = payment.Amount,
        TransactionRef = payment.TransactionRef,
        PayerId = payment.PayerId,
        PayerName = payerName,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };

    /// <summary>
    /// Sinh mã tham chiếu giao dịch để đối soát khi chuyển khoản.
    /// </summary>
    private static string BuildTransactionRef(Guid transactionId)
        => $"SFM-{transactionId.ToString("N")[..10].ToUpperInvariant()}";

    private async Task NotifyCounterpartyAsync(
        string? to,
        string? fullName,
        string initiatorName,
        string itemTitle,
        int txType,
        CancellationToken ct,
        string subject = "Bạn có giao dịch mới",
        string? intro = null,
        string? itemCode = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        // Mã hàng để 2 bên trao đổi mã khi gặp mặt.
        var codeHtml = string.IsNullOrWhiteSpace(itemCode)
            ? string.Empty
            : $"<br/>Mã hàng: <b>{System.Net.WebUtility.HtmlEncode(itemCode)}</b>";

        var body = intro is null
            ? $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(fullName ?? "")}</b>,</p>
                <p><b>{System.Net.WebUtility.HtmlEncode(initiatorName)}</b> đã khởi tạo giao dịch
                (<b>{TransactionLabels.TypeName(txType)}</b>) với món đồ
                <b>{System.Net.WebUtility.HtmlEncode(itemTitle)}</b>{codeHtml}.</p>
                <p>Hãy vào mục <b>Giao dịch</b> để xem chi tiết.</p>"
            : $@"<p>Chào <b>{System.Net.WebUtility.HtmlEncode(fullName ?? "")}</b>,</p>
                <p>{string.Format(System.Globalization.CultureInfo.InvariantCulture, intro, System.Net.WebUtility.HtmlEncode(initiatorName), System.Net.WebUtility.HtmlEncode(itemTitle))}{codeHtml}</p>
                <p>Hãy vào mục <b>Giao dịch</b> để xem chi tiết.</p>";


        try
        {
            await _emailSender.SendAsync(to, subject, body, true, ct);
        }
        catch (Exception)
        {
            // Bỏ qua — lỗi SMTP không được làm hỏng luồng nghiệp vụ.
        }
    }
}
