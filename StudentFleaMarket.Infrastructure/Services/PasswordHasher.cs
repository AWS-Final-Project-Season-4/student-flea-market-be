using Microsoft.AspNetCore.Identity;
using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.Infrastructure.Services;

/// <summary>
/// Băm mật khẩu bằng PBKDF2 (mặc định của ASP.NET Core Identity).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;

        try
        {
            var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);
            return result != PasswordVerificationResult.Failed;
        }
        catch
        {
            return false;
        }
    }
}
