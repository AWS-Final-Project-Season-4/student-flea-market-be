using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.Infrastructure.Services;

/// <summary>
/// Xác thực Google OpenID Connect ID Token bằng JWKS công khai của Google.
/// </summary>
public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private const string GoogleCertsUrl = "https://www.googleapis.com/oauth2/v3/certs";
    private static readonly string[] ValidIssuers = { "accounts.google.com", "https://accounts.google.com" };

    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static JsonWebKeySet? _cachedJwks;
    private static DateTime _cacheExpiresAt = DateTime.MinValue;

    private readonly GoogleAuthOptions _options;
    private readonly ILogger<GoogleTokenVerifier> _logger;

    public GoogleTokenVerifier(
        IOptions<GoogleAuthOptions> options,
        ILogger<GoogleTokenVerifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var clientIds = _options.GetConfiguredClientIds().ToList();
        if (clientIds.Count == 0)
        {
            throw new ValidationException(
                "Google OAuth is not configured on the server (missing Google:ClientId).",
                "GOOGLE_AUTH_NOT_CONFIGURED");
        }

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        JwtSecurityToken parsedToken;
        try
        {
            parsedToken = handler.ReadJwtToken(idToken);
        }
        catch (Exception)
        {
            throw new UnauthorizedException("Invalid Google ID token format.", "INVALID_GOOGLE_TOKEN");
        }

        var jwks = await GetGoogleJwksAsync(parsedToken.Header.Kid, ct);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers,
            ValidateAudience = true,
            ValidAudiences = clientIds,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = jwks.GetSigningKeys()
        };

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(idToken, validationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Google ID token validation failed.");
            throw new UnauthorizedException("Google ID token is invalid or expired.", "INVALID_GOOGLE_TOKEN");
        }

        var subject = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var emailVerifiedRaw = principal.FindFirst("email_verified")?.Value;
        var fullName = principal.FindFirst("name")?.Value;
        var picture = principal.FindFirst("picture")?.Value;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedException("Google ID token is missing required claims.", "INVALID_GOOGLE_TOKEN");

        var emailVerified = string.Equals(emailVerifiedRaw, "true", StringComparison.OrdinalIgnoreCase);

        return new GoogleUserInfo
        {
            Subject = subject,
            Email = email,
            EmailVerified = emailVerified,
            FullName = fullName,
            Picture = picture
        };
    }

    private async Task<JsonWebKeySet> GetGoogleJwksAsync(string? kid, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var current = _cachedJwks;

        if (current is not null && now < _cacheExpiresAt && KeyExists(current, kid))
            return current;

        await CacheLock.WaitAsync(ct);
        try
        {
            now = DateTime.UtcNow;
            if (_cachedJwks is not null && now < _cacheExpiresAt && KeyExists(_cachedJwks, kid))
                return _cachedJwks;

            using var response = await HttpClient.GetAsync(GoogleCertsUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var jwks = new JsonWebKeySet(json);

            var maxAge = response.Headers.CacheControl?.MaxAge ?? TimeSpan.FromHours(1);
            if (maxAge < TimeSpan.FromMinutes(5))
                maxAge = TimeSpan.FromMinutes(5);

            _cachedJwks = jwks;
            _cacheExpiresAt = DateTime.UtcNow.Add(maxAge);
            return jwks;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Unable to fetch Google JWKS certificates.");
            throw new UnauthorizedException("Unable to verify Google token at this time.", "GOOGLE_VERIFICATION_UNAVAILABLE");
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static bool KeyExists(JsonWebKeySet jwks, string? kid)
    {
        if (string.IsNullOrEmpty(kid))
            return true;

        return jwks.Keys.Any(k => string.Equals(k.Kid, kid, StringComparison.Ordinal));
    }
}
