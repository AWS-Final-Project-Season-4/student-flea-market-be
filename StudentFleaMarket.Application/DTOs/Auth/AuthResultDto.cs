namespace StudentFleaMarket.Application.DTOs.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Username { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SchoolName { get; set; }
    public string? StudentCode { get; set; }
    public string? CommonArea { get; set; }
    public string? Dormitory { get; set; }
    public int Role { get; set; }

    /// <summary>
    /// Tên vai trò: "User", "Admin" hoặc "AiService".
    /// </summary>
    public string RoleName { get; set; } = "User";

    public int Status { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Kết quả đăng ký. Chưa sinh token: người dùng phải xác thực email trước khi đăng nhập.
/// </summary>
public class RegisterResultDto
{
    public string Message { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    public string VerificationEmail { get; set; } = null!;
}
