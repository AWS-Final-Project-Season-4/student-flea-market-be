using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// CHÍNH SÁCH ĐIỂM UY TÍN — nguồn sự thật duy nhất cho ngưỡng xếp mức uy tín.
///
/// VÌ SAO KHÔNG DÙNG MỘT CON SỐ 0–100?
///   Con số tổng hợp (vd: 60% giao dịch + 25% đánh giá + 15% không report)
///   có vẻ đẹp nhưng rất dễ bị "mua" bằng vài giao dịch giá trị thấp, và
///   người dùng không hiểu trọng số 60/25/15 nghĩa là gì. Badge mức là thứ
///   người dùng tự quyết định có nên tin hay không.
///
/// VÌ SAO LẤY "SỐ GIAO DỊCH HOÀN THÀNH" LÀM TIÊU CHÍ CHÍNH?
///   Đó là tín hiệu khó giả mạo nhất trong một sàn C2C: mỗi giao dịch hoàn
///   thành đều để lại dấu vết và phải có 2 bên tham gia. Nhưng để tránh
///   "tín dụng" bằng giao dịch giá trị thấp, ta dùng ngưỡng theo SỐ GIAO
///   DỊCH chứ không theo tổng giá trị.
///
/// VÌ SAO VẪN GIỮ ĐÁNH GIÁ VÀ SỐ REPORT?
///   Vì hai tín hiệu đó bổ sung cho nhau:
///     - Nhiều giao dịch nhưng đánh giá tệ  -> hàng hoá chất lượng thấp,
///       người mua nhiều nhưng không hài lòng (có thể do chủ quan).
///     - Ít giao dịch nhưng có report xác nhận -> người mới nhưng đã có vi
///       phạm, phải cảnh báo ngay.
///
/// THỨ TỰ ƯU TIÊN:
///   Warning > Trusted > Member > New
///   (Warning ghi đè mọi mức khác — đã có vi phạm thì không được coi là uy tín)
///
/// Toàn bộ ngưỡng nằm ở đây để chỉ sửa 1 chỗ khi cần tinh chỉnh.
/// </summary>
public static class TrustPolicy
{
    // ------------------------------------------------------- NGUỒNG NGƯỠNG

    /// <summary>
    /// Tối thiểu giao dịch hoàn thành để lên mức "Đã thiết lập" (🥈 Member).
    /// Dưới ngưỡng này -> "Mới tham gia" (🥉 New).
    /// </summary>
    public const int MemberMinCompletedTransactions = 3;

    /// <summary>
    /// Tối thiểu giao dịch hoàn thành để lên mức "Uy tín" (🥇 Trusted).
    /// Đây là ngưỡng chính — mốc đáng tin cậy trong một sàn C2C.
    /// </summary>
    public const int TrustedMinCompletedTransactions = 10;

    /// <summary>
    /// Số report ĐÃ ĐƯỢC NHÂN VIÊN XÁC NHẬN là có lý do mà tài khoản
    /// này bị đánh dấu Warning. Từ 1 report đã xác nhận là đủ để cảnh báo.
    ///
    /// Chỉ tính report có hành động (action taken), không tính report bị bác.
    /// </summary>
    public const int WarningMinConfirmedReports = 1;

    // ------------------------------------------------------- API xếp mức

    /// <summary>
    /// Xếp mức uy tín của một tài khoản.
    /// </summary>
    /// <param name="completedTransactions">Tổng số giao dịch đã hoàn thành (mua + bán).</param>
    /// <param name="confirmedReports">Số report đã được xác nhận là có lý do.</param>
    /// <param name="averageRating">Điểm đánh giá trung bình (null nếu chưa có đánh giá).</param>
    public static TrustLevel DetermineLevel(
        int completedTransactions,
        int confirmedReports,
        decimal? averageRating)
    {
        // Đã có vi phạm được xác nhận -> cảnh báo, ghi đè mọi mức khác.
        if (confirmedReports >= WarningMinConfirmedReports)
            return TrustLevel.Warning;

        if (completedTransactions >= TrustedMinCompletedTransactions)
            return TrustLevel.Trusted;

        if (completedTransactions >= MemberMinCompletedTransactions)
            return TrustLevel.Member;

        return TrustLevel.New;
    }

    /// <summary>
    /// Nhãn hiển thị kèm emoji cho badge uy tín.
    /// </summary>
    public static string LevelBadge(TrustLevel level) => level switch
    {
        TrustLevel.New => "🥉 Mới tham gia",
        TrustLevel.Member => "🥈 Đã thiết lập",
        TrustLevel.Trusted => "🥇 Uy tín",
        TrustLevel.Warning => "⚠ Cần cảnh giác",
        _ => "Chưa xếp hạng"
    };

    /// <summary>
    /// Nhãn ngắn (không emoji) để dùng trong danh sách / bảng quản trị.
    /// </summary>
    public static string LevelName(TrustLevel level) => level switch
    {
        TrustLevel.New => "Mới tham gia",
        TrustLevel.Member => "Đã thiết lập",
        TrustLevel.Trusted => "Uy tín",
        TrustLevel.Warning => "Cần cảnh giác",
        _ => "Chưa xếp hạng"
    };

    /// <summary>
    /// Màu badge gợi ý cho frontend (mã màu hex).
    /// </summary>
    public static string LevelColor(TrustLevel level) => level switch
    {
        TrustLevel.New => "#9CA3AF",      // xám
        TrustLevel.Member => "#3B82F6",   // xanh dương
        TrustLevel.Trusted => "#10B981",  // xanh lá
        TrustLevel.Warning => "#EF4444",  // đỏ
        _ => "#9CA3AF"
    };

    /// <summary>
    /// Ngưỡng "đánh giá tệ" — dùng để gợi ý cho người dùng trước khi giao dịch.
    /// </summary>
    public const decimal LowRatingThreshold = 3.0m;
}
