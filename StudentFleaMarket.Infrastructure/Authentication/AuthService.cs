using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentFleaMarket.Application.Auth;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Authentication;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("Email is already registered.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            SchoolName = request.SchoolName?.Trim(),
            StudentCode = request.StudentCode?.Trim(),
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ConflictException(string.Join(" ", result.Errors.Select(x => x.Description)));

        return CreateToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Email or password is incorrect.");
        if (user.Status != 1)
            throw new ForbiddenException("This account is inactive.");

        return CreateToken(user);
    }

    private AuthResponse CreateToken(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, user.Role == 1 ? "Admin" : "Student")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiresAt, signingCredentials: credentials);
        return new AuthResponse(user.Id, user.FullName, user.Email!, new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
