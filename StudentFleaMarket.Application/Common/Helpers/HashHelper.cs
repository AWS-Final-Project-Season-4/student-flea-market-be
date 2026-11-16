using System.Security.Cryptography;
using System.Text;

namespace StudentFleaMarket.Application.Common.Helpers;

/// <summary>
/// Tiện ích băm SHA-256 dùng cho OTP và refresh token.
/// </summary>
public static class HashHelper
{
    public static string Sha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
