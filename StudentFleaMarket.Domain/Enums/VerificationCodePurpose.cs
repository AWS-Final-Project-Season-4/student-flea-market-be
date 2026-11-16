namespace StudentFleaMarket.Domain.Enums;

/// <summary>
/// Mục đích sử dụng của mã OTP 6 chữ số.
/// Tách biệt để mã xác thực email không thể dùng lại cho đặt lại mật khẩu và ngược lại.
/// </summary>
public enum VerificationCodePurpose
{
    EmailVerification = 1,
    PasswordReset = 2
}
