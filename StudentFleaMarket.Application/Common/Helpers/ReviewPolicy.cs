namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// LUẬT ĐÁNH GIÁ — nguồn sự thật duy nhất cho vòng đời đánh giá.
///
/// Tách ra helper vì luật này áp dụng ở nhiều nơi: khi tạo đánh giá, khi sửa
/// đánh giá, khi hiển thị nút "viết đánh giá" và khi tính điểm uy tín.
/// Nếu mỗi nơi tự viết một lần rất dễ lệch logic.
///
/// BA LUẬT CỐT LÕI:
///
/// 1) PHẠM VI ĐÁNH GIÁ:
///    - Purchase / Free : 1 CHIỀU — chỉ người MUA (Initiator) đánh giá người BÁN.
///    - Exchange        : 2 CHIỀU — cả 2 bên cùng đánh giá nhau.
///      Lý do: ở đổi đồ, cả hai đều mang hàng ra và nhận hàng về; bên chịu
///      rủi ro mất món đắt phải có quyền phát biểu. Ở mua trực tiếp thì
///      người bán không rủi ro gì ngoài việc không nhận được tiền — mà việc đó
///      đã có report riêng xử lý.
///
/// 2) THỜI HẠN SỬA:
///    Đánh giá được sửa trong 14 ngày kể từ khi viết. Sau đó đánh giá bị
///    "đóng băng" để tránh tình trạng người dùng đổi ý kiến theo bốc đồng
///    (vd: mua xong hài lòng viết 5 sao, giao dịch sau lại không ưng nên
///    sửa thành 1 sao để trả thù người bán).
///
/// 3) GIAO DỊCH GIÁ 0:
///    Đơn "nhận miễn phí" (giá = 0) VẪN cho đánh giá — đó là quyền của người
///    dùng và giúp phát hiện hàng giả được tặng. NHƯNG đánh giá đó KHÔNG
///    tính vào điểm uy tín, vì tặng miễn phí không phải dịch vụ, ai cũng làm
///    được -> mọi người đều 5 sao -> tín hiệu vô nghĩa.
/// </summary>
public static class ReviewPolicy
{
    /// <summary>
    /// Số ngày được phép sửa đánh giá kể từ lúc viết.
    /// </summary>
    public const int EditWindowDays = 14;

    /// <summary>
    /// Đơn này có được đánh giá 2 chiều không?
    /// Chỉ đơn Exchange mới 2 chiều (xem mô tả class).
    /// </summary>
    public static bool IsTwoWay(int transactionType)
        => transactionType == (int)Domain.Enums.TransactionType.Exchange;

    /// <summary>
    /// Đánh giá này có được tính vào điểm uy tín không?
    ///
    /// Điều kiện: đơn có giá trị &gt; 0. Đánh giá bị vô hiệu (IsVoided) đã
    /// bị loại ở tầng repository rồi, không cần kiểm tra lại ở đây.
    /// </summary>
    public static bool CountsTowardsTrust(decimal transactionAmount)
        => transactionAmount > 0;

    /// <summary>
    /// Còn trong thời hạn sửa đánh giá không?
    /// </summary>
    public static bool IsWithinEditWindow(DateTime createdAt, DateTime now)
        => (now - createdAt).TotalDays <= EditWindowDays;
}
