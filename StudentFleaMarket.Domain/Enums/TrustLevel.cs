namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Mức uy tín của một tài khoản — badge hiển thị trên profile.
///
/// Nguồn sự thật duy nhất cho ngưỡng xếp mức:
/// <c>StudentFleaMarket.Application.Common.Helpers.TrustPolicy</c>.
///
/// Vì sao KHÔNG dùng 1 con số 0–100?
///   - Con số tổng hợp dễ bị "mua" bằng vài giao dịch giá trị thấp.
///   - Người dùng không hiểu trọng số 60/25/15 nghĩa là gì.
///   - Badge mức là thứ người dùng tự quyết định có nên tin hay không.
///
/// Thứ tự ưu tiên: <see cref="Warning"/> ghi đè mọi mức khác.
/// </summary>
public enum TrustLevel
{
    /// <summary>Mới tham gia — chưa đủ giao dịch để đánh giá.</summary>
    New = 1,

    /// <summary>Đã thi lập — có giao dịch nhưng chưa nhiều.</summary>
    Member = 2,

    /// <summary>Uy tín — nhiều giao dịch thành công, không có vi phạm.</summary>
    Trusted = 3,

    /// <summary>Cần cảnh giác — đã có vi phạm được nhân viên xác nhận.</summary>
    Warning = 4
}
