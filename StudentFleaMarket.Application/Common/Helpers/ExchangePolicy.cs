namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// LUẬT GIAO DỊCH ĐỔI ĐỒ — nguồn sự thật duy nhất cho các ngưỡng của luồng Exchange.
///
/// Tách ra helper vì luật này được dùng ở nhiều nơi: khi tạo đơn, khi chốt
/// lịch gặp mặt, khi background job quét đơn quá hạn và khi hiển thị cảnh
/// báo cho người dùng. Nếu mỗi nơi tự viết một lần rất dễ lệch logic.
///
/// BA LUẬT CỐT LÕI:
///
/// 1) CHỐT LỊCH GẶP MẶT:
///    Hai bên thống nhất lịch qua khung chat, rồi MỘT BÊN nhập vào hệ thống
///    (thời gian + địa điểm). Từ thời điểm đó, hệ thống mở thông báo giao
///    dịch cho cả 2 bên, trong đó có SỐ ĐIỆN THOẠI của cả hai — vì từ đây
///    hai bên cần liên lạc trực tiếp để gặp mặt.
///
/// 2) XÁC NHẬN 2 CHIỀU:
///    Đơn Exchange bắt buộc CẢ 2 BÊN xác nhận bàn giao thì mới hoàn thành.
///    Bên xác nhận nhập mã hàng của món vừa nhận để hệ thống đối chiếu với
///    món ĐÁNG LẼ phải nhận -&gt; phát hiện tráo hàng ngay tại chỗ.
///
/// 3) TỰ HỦY SAU 7 NGÀY:
///    Tính từ THỜI ĐIỂM GẶP MẶT ĐÃ CHỐT (ScheduledAt). Nếu quá hạn mà chưa
///    đủ 2 bên xác nhận, đơn bị hủy tự động và 2 tin đăng được trả lại
///    trạng thái "đang bán" — tránh trường hợp món bị giữ chiếm vô thời
///    hạn vì một bên không xuất hiện.
/// </summary>
public static class ExchangePolicy
{
    /// <summary>
    /// Số ngày được phép để 2 bên xác nhận bàn giao, tính từ thời điểm gặp
    /// mặt đã chốt. Quá hạn -&gt; đơn bị hủy tự động.
    /// </summary>
    public const int AutoCancelDays = 7;

    /// <summary>
    /// Số ngày tối đa được chốt lịch gặp mặt trong tương lai.
    /// Chặn nhập nhầm ngày quá xa.
    /// </summary>
    public const int MaxScheduleDaysAhead = 365;

    /// <summary>
    /// Chốt lịch gặp mặt cho đơn đổi đồ.
    ///
    /// Điều kiện:
    ///   - Thời điểm gặp phải nằm trong tương lai (không quá xa).
    ///   - Địa điểm phải có nội dung.
    /// </summary>
    /// <returns>null nếu hợp lệ, hoặc thông điệp lỗi để hiển thị cho người dùng.</returns>
    public static string? ValidateSchedule(DateTime scheduledAt, string? meetingLocation, DateTime now)
    {
        if (scheduledAt <= now)
            return "Thời gian gặp mặt phải nằm trong tương lai.";

        if (scheduledAt > now.AddDays(MaxScheduleDaysAhead))
            return $"Thời gian gặp mặt không được vượt quá {MaxScheduleDaysAhead} ngày.";

        if (string.IsNullOrWhiteSpace(meetingLocation))
            return "Địa điểm gặp mặt không được để trống.";

        if (meetingLocation.Trim().Length > 255)
            return "Địa điểm gặp mặt không được vượt quá 255 ký tự.";

        return null;
    }

    /// <summary>
    /// Mốc hết hạn của một đơn đổi đồ = thời điểm gặp mặt đã chốt + 7 ngày.
    ///
    /// Ưu tiên dùng <c>ScheduledAt</c> (thời điểm gặp mặt thật sự). Nếu
    /// chưa chốt lịch thì chưa có hạn — đơn không tự hủy.
    /// </summary>
    public static DateTime? GetDeadline(DateTime? scheduledAt)
        => scheduledAt?.AddDays(AutoCancelDays);

    /// <summary>
    /// Đơn đã quá hạn chưa?
    /// Chỉ đơn đã chốt lịch mới có hạn — đơn chưa chốt lịch thì không tự hủy.
    /// </summary>
    public static bool IsExpired(DateTime? scheduledAt, DateTime now)
    {
        var deadline = GetDeadline(scheduledAt);
        return deadline.HasValue && now > deadline.Value;
    }
}
