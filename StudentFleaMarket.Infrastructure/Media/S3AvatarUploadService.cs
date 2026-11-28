using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.DTOs.Auth;
using StudentFleaMarket.Application.Media;
using StudentFleaMarket.Application.Services;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Media;

/// <summary>
/// Ảnh đại diện đi cùng pipeline với ảnh tin đăng:
/// browser → presigned POST → <c>raw/users/{userId}/avatars/{uploadId}.ext</c> (private)
/// → Lambda ImageProcessor cắt vuông → <c>processed/avatars/{userId}/{uploadId}/{large|thumbnail}.webp</c> → CloudFront.
/// </summary>
internal sealed class S3AvatarUploadService(
    AppDbContext dbContext,
    IAmazonS3 s3,
    MediaStorageOptions options,
    MediaUploadLimits limits) : IAvatarUploadService
{
    public async Task<PresignedImageUploadDto> CreatePresignedUploadAsync(
        Guid userId,
        PresignAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveUserAsync(userId, cancellationToken);

        var uploadId = Guid.NewGuid();
        var objectKey = MediaUploadRules.CreateAvatarObjectKey(userId, uploadId, request.ContentType);
        var expiresAt = MediaUploadRules.CalculateExpiry(DateTime.UtcNow, options.PresignedUrlExpiryMinutes);

        var presignedRequest = new CreatePresignedPostRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            Expires = expiresAt
        };
        presignedRequest.Fields["Content-Type"] = request.ContentType;
        presignedRequest.Conditions.Add(S3PostCondition.ContentLengthRange(request.SizeBytes, request.SizeBytes));
        var presignedPost = await s3.CreatePresignedPostAsync(presignedRequest);

        return new PresignedImageUploadDto(
            uploadId,
            objectKey,
            presignedPost.Url,
            "POST",
            presignedPost.Fields,
            request.SizeBytes,
            expiresAt,
            $"{MediaUrls.Root(options)}/{MediaUploadRules.AvatarProcessedPrefix(userId, uploadId)}/");
    }

    public async Task<UserDto> ConfirmUploadAsync(
        Guid userId,
        ConfirmAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        // Key phải do chính presign của user này sinh ra — không nhận URL tuỳ ý từ client.
        if (!MediaUploadRules.TryParseAvatarObjectKey(request.ObjectKey, userId, out var uploadId))
            throw new ValidationException("Object key does not belong to this user's avatar upload.", "INVALID_OBJECT_KEY");

        var user = await EnsureActiveUserAsync(userId, cancellationToken);
        await EnsureRawObjectUploadedAsync(request.ObjectKey!, cancellationToken);

        user.AvatarUrl = $"{MediaUrls.Root(options)}/{MediaUploadRules.AvatarProcessedPrefix(userId, uploadId)}/large.webp";
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthService.MapUser(user);
    }

    public async Task<UserDto> RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await EnsureActiveUserAsync(userId, cancellationToken);
        if (user.AvatarUrl is not null)
        {
            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return AuthService.MapUser(user);
    }

    private async Task<ApplicationUser> EnsureActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.", "USER_NOT_FOUND");
        if (user.Status != 1)
            throw new ForbiddenException("User account is not active.");
        return user;
    }

    private async Task EnsureRawObjectUploadedAsync(string objectKey, CancellationToken cancellationToken)
    {
        GetObjectMetadataResponse metadata;
        try
        {
            metadata = await s3.GetObjectMetadataAsync(options.BucketName, objectKey, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ValidationException("Image has not been uploaded yet.", "UPLOAD_NOT_FOUND");
        }

        if (metadata.ContentLength <= 0 || metadata.ContentLength > limits.MaxAvatarSizeBytes)
            throw new ValidationException("Uploaded image size is invalid.", "UPLOAD_INVALID_SIZE");
    }
}

/// <summary>
/// Gốc URL công khai của ảnh đã xử lý. Ưu tiên CloudFront; local (LocalStack) dùng
/// path-style ServiceUrl; còn lại là S3 regional endpoint.
/// </summary>
internal static class MediaUrls
{
    public static string Root(MediaStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CloudFrontBaseUrl))
            return options.CloudFrontBaseUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            return $"{options.ServiceUrl.TrimEnd('/')}/{options.BucketName}";
        return $"https://{options.BucketName}.s3.{options.Region}.amazonaws.com";
    }
}
