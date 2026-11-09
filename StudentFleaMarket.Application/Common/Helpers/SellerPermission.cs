using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// LUẬT "ĐƯỢC PHÉP ĐĂNG TIN BÁN" — nguồn sự thật duy nhất.
///
/// Vì sao tách ra helper?
///   Luật này được dùng ở nhiều nơi: chặn khi tạo tin, chặn khi sửa tin,
///   và hiển thị cờ `canSell` cho frontend. Nếu mỗi nơi tự viết một lần,
///   rất dễ xảy ra lệch logic (ví dụ một nơi quên mất trường hợp Admin).
///   Gom vào đây để đổi luật chỉ sửa 1 chỗ.
///
/// LUẬT:
///   - Tài khoản Admin  -> luôn được phép (Admin là bên quản lý, không cần
///     tự xin duyệt; xem SellerService.RequestAsync -> ADMIN_ALREADY_CAN_SELL).
///   - Tài khoản User   -> CHỈ được phép khi SellerStatus = Approved, tức là
///     đã được Admin duyệt qua POST /api/Admin/seller-requests/{id}/approve.
///   - Tài khoản dịch vụ AI -> không được phép (đã chặn riêng ở ItemService).
///
/// Lưu ý: trạng thái được đọc TRỰC TIẾP từ DB (không từ JWT), nên việc
/// Admin duyệt / thu hồi quyền bán có hiệu lực tức thì, không cần đăng nhập lại.
/// </summary>
public static class SellerPermission
{
    /// <summary>
    /// Tài khoản này có được phép đăng tin bán không?
    /// </summary>
    public static bool CanSell(ApplicationUser user)
        => user.Role == (int)UserRole.Admin
           || user.SellerStatus == (int)SellerStatus.Approved;

    /// <summary>
    /// Ném 403 nếu tài khoản không được phép đăng tin bán.
    /// Thông báo thay đổi theo trạng thái để user biết phải làm gì tiếp theo.
    /// </summary>
    public static void EnsureCanSell(ApplicationUser user)
    {
        if (CanSell(user))
            return;

        switch ((SellerStatus)user.SellerStatus)
        {
            case SellerStatus.Pending:
                throw new ForbiddenException(
                    "Your request to become a seller is awaiting Admin approval. " +
                    "Please wait for approval before posting",
                    "SELLER_REQUEST_PENDING");

            case SellerStatus.Rejected:
                throw new ForbiddenException(
                    "Your request to become a seller has been rejected. " +
                    "You may resubmit your request for review.",
                    "SELLER_REQUEST_REJECTED");

            default:
                throw new ForbiddenException(
                    "You have not been granted permission to list items for sale. " +
                    "Submit your request to become a seller and wait for the admin to approve it.",
                    "SELLER_NOT_APPROVED");
        }
    }
}
