using System.Globalization;
using System.Text;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// Sinh slug URL-friendly từ tiêu đề tiếng Việt.
/// Ví dụ: "Sách Lập trình C#" -> "sach-lap-trinh-c"
/// </summary>
public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "item";

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);

            // Bỏ dấu tiếng Việt: giữ lại chữ cái cơ bản (L) và số (N).
            if (category != UnicodeCategory.NonSpacingMark)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    lastWasDash = false;
                }
                else if (!lastWasDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
        }

        var slug = sb.ToString().Trim('-');

        return string.IsNullOrEmpty(slug) ? "item" : slug;
    }

    /// <summary>
    /// Sinh slug kèm hậu tố ngẫu nhiên để đảm bảo duy nhất (Slug có unique index).
    /// </summary>
    public static string GenerateUnique(string input)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{Generate(input)}-{suffix}";
    }

    /// <summary>
    /// Chuẩn hóa chuỗi để TÌM KIẾM không phân biệt dấu tiếng Việt.
    /// Ví dụ: "Sách Lập trình C#" -> "sach lap trinh c"
    /// Dùng cho cột TitleNormalized (đã index) để tìm kiếm nhanh & chuẩn.
    /// </summary>
    public static string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasSpace = false;

        foreach (var ch in normalized)
        {
            // Bỏ dấu (NonSpacingMark) — giữ lại chữ cái gốc.
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasSpace = false;
            }
            else if (!lastWasSpace && sb.Length > 0)
            {
                // Mọi ký tự phân cách (space, -, _, /, ...) đều gộp thành 1 space.
                sb.Append(' ');
                lastWasSpace = true;
            }
        }

        return sb.ToString().Trim();
    }
}
