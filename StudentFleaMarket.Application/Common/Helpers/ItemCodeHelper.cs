using System.Security.Cryptography;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// Sinh mã hàng (<c>ItemCode</c>) — mã ngắn 8 ký tự để người dùng giao tiếp với hệ thống
/// (mua bán / trao đổi đồ) thay cho GUID 36 ký tự.
///
/// THIẾT KẾ CHUẨN: Tiền tố Danh mục (2 ký tự) + Crockford Base32 (6 ký tự) = Đúng 8 ký tự
/// --------------------------------------------------------------------------------------
/// 1. Đúng 8 ký tự để khớp với schema Database hiện tại (HasMaxLength(8)).
/// 2. Tiền tố phân loại danh mục (Smart Category Prefix):
///    - BK: Books & Textbooks (Sách & Giáo trình)
///    - EL: Electronics (Đồ điện tử)
///    - DL: Dorm & Living (Đồ KTX & Đời sống)
///    - FA: Fashion (Thời trang & Phụ kiện)
///    - ST: Stationery & Study (Dụng cụ học tập)
///    - BH: Beauty & Health (Sức khỏe & Sắc đẹp)
///    - FD: Food & Drinks (Đồ ăn & Thức uống)
///    - VA: Vehicle Accessories (Phụ kiện xe)
///    - IT: Generic Item (Mặc định / Danh mục khác)
/// 3. Bảng ký tự Crockford Base32 (32 ký tự) loại bỏ triệt để các ký tự gây nhầm lẫn:
///    - Không chứa 'O', 'I', 'L' (dễ nhầm với 0 và 1).
///    - Không chứa 'U' (tránh vô tình ghép thành từ ngữ nhạy cảm).
/// 4. Không gian tổ hợp: 32^6 = 1.073.741.824 (hơn 1 TỶ sản phẩm MỖI DANH MỤC).
/// 5. Auto-correct thông minh: tự động sửa lỗi nhập (O -> 0, I/L -> 1, tự bỏ dấu gạch ngang '-').
/// 6. Tương thích ngược: Vẫn chấp nhận mã 8 ký tự cũ (từ dữ liệu demo/seed).
/// </summary>
public static class ItemCodeHelper
{
    /// <summary>Số ký tự của mã hàng trong Database.</summary>
    public const int Length = 8;

    /// <summary>Tiền tố mặc định nếu không xác định được danh mục.</summary>
    public const string DefaultPrefix = "IT";

    /// <summary>Độ dài phần ngẫu nhiên (8 - 2 = 6).</summary>
    public const int SuffixLength = 6;

    /// <summary>
    /// Bảng ký tự Crockford Base32 (32 ký tự: 0-9 và A-Z loại trừ I, L, O, U).
    /// </summary>
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Bảng ký tự mở rộng để kiểm tra tính hợp lệ và tương thích ngược với mã demo cũ.
    /// </summary>
    private const string LegacyAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Bản đồ quy đổi tên / slug danh mục sang tiền tố 2 ký tự.
    /// </summary>
    public static string GetCategoryPrefix(string? categorySlugOrName)
    {
        if (string.IsNullOrWhiteSpace(categorySlugOrName))
            return DefaultPrefix;

        var normalized = categorySlugOrName.Trim().ToLowerInvariant();

        if (normalized.Contains("book") || normalized.Contains("sách") || normalized.Contains("textbooks") || normalized.Contains("giáo trình"))
            return "BK";
        if (normalized.Contains("electr") || normalized.Contains("điện tử") || normalized.Contains("tech"))
            return "EL";
        if (normalized.Contains("dorm") || normalized.Contains("ktx") || normalized.Contains("đời sống") || normalized.Contains("living"))
            return "DL";
        if (normalized.Contains("fashion") || normalized.Contains("thời trang") || normalized.Contains("quần áo"))
            return "FA";
        if (normalized.Contains("station") || normalized.Contains("học tập") || normalized.Contains("dụng cụ") || normalized.Contains("study"))
            return "ST";
        if (normalized.Contains("beauty") || normalized.Contains("sức khỏe") || normalized.Contains("sắc đẹp") || normalized.Contains("health"))
            return "BH";
        if (normalized.Contains("food") || normalized.Contains("ăn") || normalized.Contains("uống") || normalized.Contains("drink"))
            return "FD";
        if (normalized.Contains("phu-kien-xe") || normalized.Contains("phụ kiện xe") || normalized.Contains("vehicle"))
            return "VA";

        return DefaultPrefix;
    }

    /// <summary>
    /// Sinh một mã hàng ngẫu nhiên gồm đúng 8 ký tự: Tiền tố (2 ký tự) + 6 ký tự Base32.
    /// </summary>
    public static string Generate(string? categoryPrefix = null)
    {
        var prefix = string.IsNullOrWhiteSpace(categoryPrefix) || categoryPrefix.Trim().Length < 2
            ? DefaultPrefix
            : categoryPrefix.Trim()[..2].ToUpperInvariant();

        var bytes = RandomNumberGenerator.GetBytes(SuffixLength);
        var chars = new char[Length];
        chars[0] = prefix[0];
        chars[1] = prefix[1];

        for (var i = 0; i < SuffixLength; i++)
        {
            chars[2 + i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }

    /// <summary>
    /// Định dạng mã hiển thị thân thiện trên UI (ví dụ "ST-7K9A2P", "BK-8X2M4N").
    /// </summary>
    public static string FormatDisplay(string? code)
    {
        var normalized = Normalize(code);
        if (normalized.Length == Length)
        {
            return $"{normalized[..2]}-{normalized[2..]}";
        }
        return normalized;
    }

    /// <summary>
    /// Chuẩn hóa mã do người dùng nhập để tra cứu:
    /// - Bỏ khoảng trắng thừa và dấu gạch nối '-'.
    /// - Chuyển sang chữ hoa.
    /// - Tự động sửa lỗi nhầm lẫn người dùng: 'O' -> '0', 'I'/'L' -> '1'.
    /// - Nếu người dùng chỉ nhập 6 ký tự hậu tố, tự động thêm tiền tố "IT".
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var cleaned = code.Trim().ToUpperInvariant().Replace("-", string.Empty);

        // Nếu người dùng chỉ gõ 6 ký tự hậu tố, tự bổ sung tiền tố mặc định
        if (cleaned.Length == SuffixLength)
        {
            cleaned = DefaultPrefix + cleaned;
        }

        var chars = new char[cleaned.Length];
        for (var i = 0; i < cleaned.Length; i++)
        {
            // 2 ký tự tiền tố đầu tiên giữ nguyên chữ cái
            if (i < 2 && char.IsLetter(cleaned[i]))
            {
                chars[i] = cleaned[i];
                continue;
            }

            chars[i] = cleaned[i] switch
            {
                'O' => '0',
                'I' => '1',
                'L' => '1',
                _ => cleaned[i]
            };
        }

        return new string(chars);
    }

    /// <summary>
    /// Kiểm tra chuỗi có đúng hình dạng của một mã hàng hợp lệ hay không.
    /// Chấp nhận cả dạng "XX-YYYYYY", "XXYYYYYY" và mã cũ 8 ký tự alphanumeric.
    /// </summary>
    public static bool IsValidFormat(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalized = Normalize(code);
        if (normalized.Length != Length)
            return false;

        return normalized.All(c => LegacyAlphabet.Contains(c));
    }
}
