using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Media;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class MediaUploadTests
{
    private static readonly MediaUploadLimits Limits = new(5 * 1024 * 1024, 3);

    [Fact]
    public void Presign_validation_accepts_supported_image()
    {
        var request = new PresignItemImagesRequest([
            new PresignImageFileRequest("photo.jpg", "image/jpeg", 1024)
        ]);

        Assert.True(new PresignItemImagesRequestValidator(Limits).Validate(request).IsValid);
    }

    [Theory]
    [InlineData("photo.gif", "image/gif", 1024)]
    [InlineData("photo.png", "image/jpeg", 1024)]
    [InlineData("../photo.jpg", "image/jpeg", 1024)]
    [InlineData("photo.jpg", "image/jpeg", 5242881)]
    public void Presign_validation_rejects_invalid_file_metadata(string fileName, string contentType, long sizeBytes)
    {
        var request = new PresignItemImagesRequest([
            new PresignImageFileRequest(fileName, contentType, sizeBytes)
        ]);

        Assert.False(new PresignItemImagesRequestValidator(Limits).Validate(request).IsValid);
    }

    [Fact]
    public void Presign_validation_rejects_too_many_images()
    {
        var files = Enumerable.Range(0, 4)
            .Select(index => new PresignImageFileRequest($"photo-{index}.webp", "image/webp", 1024))
            .ToList();

        Assert.False(new PresignItemImagesRequestValidator(Limits)
            .Validate(new PresignItemImagesRequest(files)).IsValid);
    }

    [Fact]
    public void Object_key_is_generated_from_server_owned_identifiers_and_mime()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var uploadId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var key = MediaUploadRules.CreateObjectKey(userId, itemId, uploadId, "image/png");

        Assert.Equal(
            "raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.png",
            key);
    }

    [Fact]
    public void Ownership_check_rejects_another_seller()
    {
        Assert.Throws<ForbiddenException>(() => MediaUploadRules.EnsureAuthorized(
            true,
            1,
            true,
            Guid.NewGuid(),
            Guid.NewGuid()));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Ownership_check_rejects_inactive_account(int status)
    {
        var userId = Guid.NewGuid();
        Assert.Throws<ForbiddenException>(() => MediaUploadRules.EnsureAuthorized(
            true,
            status,
            true,
            userId,
            userId));
    }

    [Fact]
    public void Ownership_check_rejects_missing_item()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<NotFoundException>(() => MediaUploadRules.EnsureAuthorized(
            true,
            1,
            false,
            Guid.Empty,
            userId));
    }

    [Fact]
    public void Expiry_uses_configured_short_lifetime()
    {
        var now = new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(now.AddMinutes(10), MediaUploadRules.CalculateExpiry(now, 10));
    }

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UploadId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Confirm_parses_key_generated_by_presign()
    {
        var key = MediaUploadRules.CreateObjectKey(UserId, ItemId, UploadId, "image/webp");

        Assert.True(MediaUploadRules.TryParseRawObjectKey(key, UserId, ItemId, out var uploadId));
        Assert.Equal(UploadId, uploadId);
    }

    [Theory]
    [InlineData("raw/users/99999999-9999-9999-9999-999999999999/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.jpg")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/items/99999999-9999-9999-9999-999999999999/33333333-3333-3333-3333-333333333333.jpg")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333.gif")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/not-a-guid.jpg")]
    [InlineData("raw/users/11111111-1111-1111-1111-111111111111/items/22222222-2222-2222-2222-222222222222/x/33333333-3333-3333-3333-333333333333.jpg")]
    [InlineData("processed/items/22222222-2222-2222-2222-222222222222/33333333-3333-3333-3333-333333333333/large.webp")]
    [InlineData("")]
    public void Confirm_rejects_keys_of_other_users_items_or_invalid_shape(string key)
    {
        Assert.False(MediaUploadRules.TryParseRawObjectKey(key, UserId, ItemId, out _));
    }

    [Fact]
    public void Confirm_validation_rejects_empty_duplicate_and_too_many_keys()
    {
        var validator = new ConfirmItemImagesRequestValidator(Limits);

        Assert.False(validator.Validate(new ConfirmItemImagesRequest([])).IsValid);
        Assert.False(validator.Validate(new ConfirmItemImagesRequest(["a", "a"])).IsValid);
        Assert.False(validator.Validate(new ConfirmItemImagesRequest(["a", "b", "c", "d"])).IsValid);
        Assert.True(validator.Validate(new ConfirmItemImagesRequest(["a", "b"])).IsValid);
    }
}
