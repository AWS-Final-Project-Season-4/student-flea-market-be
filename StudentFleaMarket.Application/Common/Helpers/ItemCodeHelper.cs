using System.Security.Cryptography;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// Sinh mã hàng (<c>ItemCode</c>) — mã ngắn để người dùng giao tiếp với hệ thống
/// (mua / đổi đồ) thay cho GUID dài 36 ký tự.
///
/// ĐẶC ĐIỂM
/// ---------
/// - Đúng 8 ký tự, chỉ gồm CHỮ CÁI (A-Z) và CHỮ SỐ (0-9).
/// - Không chứa ký tự đặc biệt (không `-`, không `_`, không ` `) để user
///   gõ / đọc / trao đổi không bị nhầm lẫn.
/// - Không chứa thông tin tiêu đề — vì mã phải BẤT BIẾN suốt vòng đời tin đăng,
///   còn tiêu đề thì có thể sửa.
/// - Sinh bằng <see cref="RandomNumberGenerator"/> (CSPRNG) — không dùng
///   <c>Random</c> để tránh mã trùng lặp giữa các tiến trình.
/// </summary>
public static class ItemCodeHelper
{
    /// <summary>Số ký tự của mã hàng.</summary>
    public const int Length = 8;

    /// <summary>
    /// Bảng ký tự: 26 chữ cái (A-Z) + 10 chữ số (0-9) = 36 ký tự.
    /// </summary>
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Sinh một mã hàng ngẫu nhiên gồm đúng <see cref="Length"/> ký tự.
    /// </summary>
    public static string Generate()
    {
        // 8 ký tự x 5 bit = 40 bit -> cần 5 byte. Dùng dư 1 byte cho an toàn.
        var bytes = RandomNumberGenerator.GetBytes(Length);
        var chars = new char[Length];

        for (var i = 0; i < Length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }

    /// <summary>
    /// Chuẩn hóa mã do người dùng nhập để tra cứu: bỏ khoảng trắng thừa,
    /// chuyển sang chữ hoa.
    ///
    /// KHÔNG bỏ ký tự đặc biệt ở đây — mã sinh ra không có ký tự đặc biệt,
    /// nên nếu user nhập thêm thì đó là mã sai và phải báo "không tìm thấy".
    /// </summary>
    public static string Normalize(string? code)
        => string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

    /// <summary>
    /// Kiểm tra chuỗi có đúng hình dạng của một mã hàng hợp lệ hay không
    /// (8 ký tự, chữ cái + chữ số).
    /// </summary>
    public static bool IsValidFormat(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var trimmed = code.Trim();
        if (trimmed.Length != Length)
            return false;

        return trimmed.All(c => Alphabet.Contains(c));
    }
}
