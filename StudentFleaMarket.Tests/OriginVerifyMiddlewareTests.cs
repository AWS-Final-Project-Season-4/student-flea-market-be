using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentFleaMarket.API.Middleware;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class OriginVerifyMiddlewareTests
{
    private const string Secret = "0123456789abcdef0123456789abcdef-test-secret";

    private static OriginVerifyMiddleware Create(bool enforce, string? secret, Action onNext) =>
        new(_ => { onNext(); return Task.CompletedTask; },
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OriginVerify:Enforce"] = enforce ? "true" : "false",
                ["OriginVerify:Secret"] = secret,
            }).Build(),
            NullLogger<OriginVerifyMiddleware>.Instance);

    private static DefaultHttpContext Request(string path, string? header = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.Body = new MemoryStream();
        if (header is not null) ctx.Request.Headers[OriginVerifyMiddleware.HeaderName] = header;
        return ctx;
    }

    [Fact]
    public async Task Not_enforced_lets_everything_through()
    {
        var called = false;
        await Create(false, null, () => called = true).InvokeAsync(Request("/api/Items/search"));
        Assert.True(called);
    }

    [Fact]
    public async Task Enforced_blocks_api_without_header()
    {
        var called = false;
        var ctx = Request("/api/Items/search");
        await Create(true, Secret, () => called = true).InvokeAsync(ctx);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Enforced_blocks_api_with_wrong_header()
    {
        var called = false;
        var ctx = Request("/api/Auth/login", Secret + "x");
        await Create(true, Secret, () => called = true).InvokeAsync(ctx);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Enforced_allows_api_with_correct_header()
    {
        var called = false;
        await Create(true, Secret, () => called = true).InvokeAsync(Request("/api/Auth/login", Secret));
        Assert.True(called);
    }

    [Fact]
    public async Task Enforced_still_allows_health_check()
    {
        var called = false;
        await Create(true, Secret, () => called = true).InvokeAsync(Request("/health"));
        Assert.True(called);
    }

    [Fact]
    public void Enforced_without_secret_fails_fast()
    {
        Assert.Throws<InvalidOperationException>(() => Create(true, "short", () => { }));
    }
}
