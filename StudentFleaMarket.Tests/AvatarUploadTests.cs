using ImageMagick;
using StudentFleaMarket.Application.Media;
using StudentFleaMarket.ImageProcessor;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class AvatarUploadTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UploadId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly MediaUploadLimits Limits = new(10 * 1024 * 1024, 8);

    [Fact]
    public void Avatar_key_round_trips_between_api_and_lambda()
    {
        var key = MediaUploadRules.CreateAvatarObjectKey(UserId, UploadId, "image/jpeg");
        Assert.Equal("raw/users/11111111-1111-1111-1111-111111111111/avatars/33333333-3333-3333-3333-333333333333.jpg", key);

        Assert.True(MediaUploadRules.TryParseAvatarObjectKey(key, UserId, out var uploadId));
        Assert.Equal(UploadId, uploadId);

        Assert.True(MediaObjectKey.TryParseRaw(key, out var parsed));
        Assert.NotNull(parsed);
        Assert.True(parsed.IsAvatar);
        Assert.Equal(MediaUploadRules.AvatarProcessedPrefix(UserId, UploadId), parsed.ProcessedPrefix);
    }

    [Theory]
    [InlineData("raw/users/22222222-2222-2222-2222-222222222222/avatars/33333333-3333-3333-3333-333333333333.jpg")] // another user
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/avatars/nested/33333333-3333-3333-3333-333333333333.jpg")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/avatars/33333333-3333-3333-3333-333333333333.gif")]
    [InlineData("processed/avatars/11111111-1111-1111-1111-111111111111/33333333-3333-3333-3333-333333333333/large.webp")]
    [InlineData(null)]
    public void Avatar_key_from_client_is_rejected_when_not_owned_or_malformed(string? key)
    {
        Assert.False(MediaUploadRules.TryParseAvatarObjectKey(key, UserId, out _));
    }

    [Theory]
    [InlineData("me.png", "image/png", 1024, true)]
    [InlineData("me.jpg", "image/png", 1024, false)]
    [InlineData("../me.png", "image/png", 1024, false)]
    [InlineData("me.png", "image/png", 5 * 1024 * 1024 + 1, false)]
    public void Avatar_presign_validation(string fileName, string contentType, long size, bool valid)
    {
        var result = new PresignAvatarRequestValidator(Limits).Validate(new PresignAvatarRequest(fileName, contentType, size));
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public async Task Avatar_transform_center_crops_to_square_variants()
    {
        await using var input = new MemoryStream();
        using (var image = new MagickImage(MagickColors.CornflowerBlue, 1200, 800))
        {
            image.Format = MagickFormat.Png;
            await image.WriteAsync(input);
        }
        input.Position = 0;

        var options = new ImageProcessorOptions(5 * 1024 * 1024, 10_000_000, 1600, 480, 80);
        var variants = await new ImageTransformer(options).TransformAsync(input, "image/png", ImageShape.Avatar);

        Assert.Equal(["large.webp", "thumbnail.webp"], variants.Select(x => x.Name));
        using var large = new MagickImage(variants[0].Content);
        using var thumb = new MagickImage(variants[1].Content);
        Assert.Equal(((uint)512, (uint)512), (large.Width, large.Height));
        Assert.Equal(((uint)128, (uint)128), (thumb.Width, thumb.Height));
    }
}
