namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Hình thức giao nhận sản phẩm trong đơn giao dịch.
/// Sàn không thu hộ hay hạch toán phí ship bên thứ 3 vào giá trị đơn hàng.
/// </summary>
public enum DeliveryMethod
{
    /// <summary>Gặp trực tiếp / Người bán tự đi giao tại điểm hẹn.</summary>
    DirectMeetup = 1,

    /// <summary>Đặt ship bên ngoài (Grab, Lalamove, Ahamove, GHN...). Hai bên tự thoả thuận cước vận chuyển.</summary>
    ExternalShipper = 2
}
