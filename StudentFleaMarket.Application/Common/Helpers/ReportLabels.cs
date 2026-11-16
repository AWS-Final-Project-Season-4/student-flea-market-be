using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// Nhãn tiếng Việt cho lý do & trạng thái report.
///
/// Gom vào 1 chỗ để backend trả về và frontend hiển thị thống nhất —
/// tránh mỗi màn hình tự map lại dẫn tới lệch nhãn.
/// </summary>
public static class ReportLabels
{
    /// <summary>
    /// Tên lý do báo cáo.
    /// </summary>
    public static string ReasonName(int reason) => reason switch
    {
        (int)ReportReason.NoShow => "Bùng hàng (không ra giao dịch)",
        (int)ReportReason.ItemMismatch => "Trả hàng sai / không đúng mô tả",
        (int)ReportReason.NotPaid => "Không trả tiền",
        (int)ReportReason.Harassment => "Quấy rối, lăng nhăng",
        (int)ReportReason.Fraud => "Lừa đảo, giả mạo",
        (int)ReportReason.FakeListing => "Tin đăng sai sự thật",
        (int)ReportReason.Other => "Vi phạm khác",
        _ => "Không xác định"
    };

    /// <summary>
    /// Tên trạng thái report.
    /// </summary>
    public static string StatusName(int status) => status switch
    {
        (int)ReportStatus.Pending => "Chờ xử lý",
        (int)ReportStatus.Reviewing => "Đang xem xét",
        (int)ReportStatus.Resolved => "Đã xử lý",
        (int)ReportStatus.Rejected => "Đã bác",
        _ => "Không xác định"
    };
}
