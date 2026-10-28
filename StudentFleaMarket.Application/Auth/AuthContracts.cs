namespace StudentFleaMarket.Application.Auth;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? SchoolName,
    string? StudentCode);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    string AccessToken,
    DateTime ExpiresAtUtc);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
