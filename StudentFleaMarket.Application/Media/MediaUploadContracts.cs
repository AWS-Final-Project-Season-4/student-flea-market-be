using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.DTOs.Auth;

namespace StudentFleaMarket.Application.Media;

public sealed record PresignImageFileRequest(string FileName, string ContentType, long SizeBytes);

public sealed record PresignItemImagesRequest(IReadOnlyList<PresignImageFileRequest>? Files);

public sealed record PresignedImageUploadDto(
    Guid UploadId,
    string ObjectKey,
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> FormFields,
    long ExpectedSizeBytes,
    DateTime ExpiresAt,
    string? ProcessedImageBaseUrl);

public sealed record PresignItemImagesResponse(IReadOnlyList<PresignedImageUploadDto> Uploads);

/// <summary>Object key nhận từ response presign (trường <c>objectKey</c>), sau khi client đã POST ảnh lên S3.</summary>
public sealed record ConfirmItemImagesRequest(IReadOnlyList<string>? ObjectKeys);

public sealed record ConfirmedItemImageDto(
    Guid Id,
    Guid UploadId,
    string ImageUrl,
    string ThumbnailUrl,
    bool IsPrimary,
    int Order);

public sealed record ConfirmItemImagesResponse(IReadOnlyList<ConfirmedItemImageDto> Images);

public sealed record MediaUploadLimits(long MaxFileSizeBytes, int MaxImagesPerItem)
{
    /// <summary>Ảnh đại diện nhỏ hơn ảnh tin đăng: tối đa 5 MB (hoặc giới hạn chung nếu thấp hơn).</summary>
    public long MaxAvatarSizeBytes => Math.Min(MaxFileSizeBytes, 5 * 1024 * 1024);
}

/// <summary>Body của <c>POST /api/Profile/avatar/presign</c> — một file ảnh.</summary>
public sealed record PresignAvatarRequest(string FileName, string ContentType, long SizeBytes);

/// <summary>Body của <c>POST /api/Profile/avatar/confirm</c> — <c>objectKey</c> lấy từ response presign.</summary>
public sealed record ConfirmAvatarRequest(string? ObjectKey);

public interface IAvatarUploadService
{
    /// <summary>Presigned POST để upload ảnh đại diện vào <c>raw/users/{userId}/avatars/</c>.</summary>
    Task<PresignedImageUploadDto> CreatePresignedUploadAsync(
        Guid userId,
        PresignAvatarRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra ảnh đã lên S3 rồi ghi URL bản đã xử lý (CloudFront) vào <c>User.AvatarUrl</c>.</summary>
    Task<UserDto> ConfirmUploadAsync(Guid userId, ConfirmAvatarRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gỡ ảnh đại diện (đặt <c>AvatarUrl</c> về null).</summary>
    Task<UserDto> RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IMediaUploadService
{
    Task<PresignItemImagesResponse> CreatePresignedUploadsAsync(
        Guid userId,
        Guid itemId,
        PresignItemImagesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi các ảnh đã upload xong (raw object tồn tại trên S3) vào bảng ItemImage.
    /// Idempotent: object key đã xác nhận trước đó sẽ được trả lại, không thêm trùng.
    /// </summary>
    Task<ConfirmItemImagesResponse> ConfirmUploadsAsync(
        Guid userId,
        Guid itemId,
        ConfirmItemImagesRequest request,
        CancellationToken cancellationToken = default);
}

public static class MediaUploadRules
{
    private static readonly IReadOnlyDictionary<string, string> ExtensionsByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp"
        };

    public static string ExtensionFor(string contentType) =>
        ExtensionsByContentType.TryGetValue(contentType, out var extension)
            ? extension
            : throw new ArgumentException("Unsupported image content type.", nameof(contentType));

    public static string CreateObjectKey(Guid userId, Guid itemId, Guid uploadId, string contentType) =>
        $"raw/users/{userId:D}/items/{itemId:D}/{uploadId:D}.{ExtensionFor(contentType)}";

    /// <summary>
    /// Kiểm tra object key đúng dạng <c>raw/users/{userId}/items/{itemId}/{uploadId}.{jpg|png|webp}</c>
    /// và thuộc đúng user + item; trả về uploadId.
    /// </summary>
    public static bool TryParseRawObjectKey(string? objectKey, Guid userId, Guid itemId, out Guid uploadId)
    {
        uploadId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(objectKey))
            return false;

        var prefix = $"raw/users/{userId:D}/items/{itemId:D}/";
        if (!objectKey.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var fileName = objectKey[prefix.Length..];
        var dot = fileName.LastIndexOf('.');
        if (dot <= 0)
            return false;

        var extension = fileName[(dot + 1)..];
        if (!ExtensionsByContentType.Values.Contains(extension, StringComparer.Ordinal))
            return false;

        return Guid.TryParseExact(fileName[..dot], "D", out uploadId);
    }

    public static string CreateAvatarObjectKey(Guid userId, Guid uploadId, string contentType) =>
        $"raw/users/{userId:D}/avatars/{uploadId:D}.{ExtensionFor(contentType)}";

    /// <summary>
    /// Kiểm tra object key đúng dạng <c>raw/users/{userId}/avatars/{uploadId}.{jpg|png|webp}</c>
    /// và thuộc đúng user; trả về uploadId.
    /// </summary>
    public static bool TryParseAvatarObjectKey(string? objectKey, Guid userId, out Guid uploadId)
    {
        uploadId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(objectKey))
            return false;

        var prefix = $"raw/users/{userId:D}/avatars/";
        if (!objectKey.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var fileName = objectKey[prefix.Length..];
        var dot = fileName.LastIndexOf('.');
        if (dot <= 0 || fileName.Contains('/'))
            return false;

        var extension = fileName[(dot + 1)..];
        if (!ExtensionsByContentType.Values.Contains(extension, StringComparer.Ordinal))
            return false;

        return Guid.TryParseExact(fileName[..dot], "D", out uploadId);
    }

    /// <summary>Phải khớp với <c>MediaObjectKey.ProcessedPrefix</c> của Lambda ImageProcessor.</summary>
    public static string AvatarProcessedPrefix(Guid userId, Guid uploadId) =>
        $"processed/avatars/{userId:D}/{uploadId:D}";

    public static string ProcessedPrefix(Guid itemId, Guid uploadId) =>
        $"processed/items/{itemId:D}/{uploadId:D}";

    public static DateTime CalculateExpiry(DateTime utcNow, int expiryMinutes) =>
        utcNow.AddMinutes(expiryMinutes);

    public static void EnsureAuthorized(
        bool userExists,
        int userStatus,
        bool itemExists,
        Guid itemSellerId,
        Guid userId)
    {
        if (!userExists || userStatus != 1)
            throw new ForbiddenException("User account is not active.");
        if (!itemExists)
            throw new NotFoundException("Item was not found.");
        if (itemSellerId != userId)
            throw new ForbiddenException("Only the seller can upload images for this item.");
    }
}
