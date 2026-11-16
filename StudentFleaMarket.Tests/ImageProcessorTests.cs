using ImageMagick;
using StudentFleaMarket.ImageProcessor;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class ImageProcessorTests
{
    private static readonly ImageProcessorOptions Options = new(
        5 * 1024 * 1024,
        10_000_000,
        1600,
        480,
        80);

    [Fact]
    public void Raw_key_maps_to_stable_processed_prefix()
    {
        const string key = "raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.jpg";

        Assert.True(MediaObjectKey.TryParseRaw(key, out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal(
            "processed/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333",
            parsed.ProcessedPrefix);
    }

    [Theory]
    [InlineData("processed/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.jpg")]
    [InlineData("raw/users/not-a-guid/items/22222222-2222-2222-2222-222222222222/image.jpg")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.gif")]
    public void Invalid_or_processed_key_is_rejected(string key)
    {
        Assert.False(MediaObjectKey.TryParseRaw(key, out _));
    }

    [Fact]
    public async Task Transformer_creates_bounded_webp_variants()
    {
        await using var input = new MemoryStream();
        using (var image = new MagickImage(MagickColors.CornflowerBlue, 2000, 1000))
        {
            image.Format = MagickFormat.Png;
            await image.WriteAsync(input);
        }
        input.Position = 0;

        var variants = await new ImageTransformer(Options).TransformAsync(input, "image/png");

        Assert.Equal(["large.webp", "thumbnail.webp"], variants.Select(x => x.Name));
        using var large = new MagickImage(variants.Single(x => x.Name == "large.webp").Content);
        using var thumbnail = new MagickImage(variants.Single(x => x.Name == "thumbnail.webp").Content);
        Assert.Equal(((uint)1600, (uint)800), (large.Width, large.Height));
        Assert.Equal(((uint)480, (uint)240), (thumbnail.Width, thumbnail.Height));
    }

    [Fact]
    public async Task Transformer_rejects_declared_mime_mismatch()
    {
        await using var input = new MemoryStream();
        using (var image = new MagickImage(MagickColors.White, 32, 32))
        {
            image.Format = MagickFormat.Png;
            await image.WriteAsync(input);
        }
        input.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new ImageTransformer(Options).TransformAsync(input, "image/jpeg"));
    }

    [Fact]
    public async Task Transformer_rejects_excessive_pixel_count_before_decode()
    {
        var strict = Options with { MaxPixels = 100 };
        await using var input = new MemoryStream();
        using (var image = new MagickImage(MagickColors.White, 20, 20))
        {
            image.Format = MagickFormat.Png;
            await image.WriteAsync(input);
        }
        input.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new ImageTransformer(strict).TransformAsync(input, "image/png"));
    }
}
