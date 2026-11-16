using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Exceptions;
using StudentFleaMarket.Application.Media;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Media;

internal sealed class S3MediaUploadService(
    AppDbContext dbContext,
    IAmazonS3 s3,
    MediaStorageOptions options) : IMediaUploadService
{
    public async Task<PresignItemImagesResponse> CreatePresignedUploadsAsync(
        Guid userId,
        Guid itemId,
        PresignItemImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var userStatus = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => (int?)x.Status)
            .SingleOrDefaultAsync(cancellationToken);
        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == itemId && !x.IsDeleted)
            .Select(x => new { x.SellerId, ImageCount = x.Images.Count })
            .SingleOrDefaultAsync(cancellationToken);

        MediaUploadRules.EnsureAuthorized(
            userStatus.HasValue,
            userStatus.GetValueOrDefault(),
            item is not null,
            item?.SellerId ?? Guid.Empty,
            userId);

        var files = request.Files!;
        if (item!.ImageCount + files.Count > options.MaxImagesPerItem)
            throw new ConflictException($"An item can contain at most {options.MaxImagesPerItem} images.");

        var expiresAt = MediaUploadRules.CalculateExpiry(DateTime.UtcNow, options.PresignedUrlExpiryMinutes);
        var uploads = new List<PresignedImageUploadDto>(files.Count);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var uploadId = Guid.NewGuid();
            var objectKey = MediaUploadRules.CreateObjectKey(userId, itemId, uploadId, file.ContentType);
            var presignedRequest = new CreatePresignedPostRequest
            {
                BucketName = options.BucketName,
                Key = objectKey,
                Expires = expiresAt
            };
            presignedRequest.Fields["Content-Type"] = file.ContentType;
            presignedRequest.Conditions.Add(S3PostCondition.ContentLengthRange(file.SizeBytes, file.SizeBytes));
            var presignedPost = await s3.CreatePresignedPostAsync(presignedRequest);
            var processedImageBaseUrl = $"{ProcessedBaseUrl(itemId, uploadId)}/";

            uploads.Add(new PresignedImageUploadDto(
                uploadId,
                objectKey,
                presignedPost.Url,
                "POST",
                presignedPost.Fields,
                file.SizeBytes,
                expiresAt,
                processedImageBaseUrl));
        }

        return new PresignItemImagesResponse(uploads);
    }

    public async Task<ConfirmItemImagesResponse> ConfirmUploadsAsync(
        Guid userId,
        Guid itemId,
        ConfirmItemImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var userStatus = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => (int?)x.Status)
            .SingleOrDefaultAsync(cancellationToken);
        var item = await dbContext.Items
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted, cancellationToken);

        MediaUploadRules.EnsureAuthorized(
            userStatus.HasValue,
            userStatus.GetValueOrDefault(),
            item is not null,
            item?.SellerId ?? Guid.Empty,
            userId);

        // Parse toàn bộ key trước khi gọi S3: key phải do chính presign của user + item này sinh ra.
        var uploads = new List<(string ObjectKey, Guid UploadId)>();
        foreach (var objectKey in request.ObjectKeys!)
        {
            if (!MediaUploadRules.TryParseRawObjectKey(objectKey, userId, itemId, out var uploadId))
                throw new ValidationException("Object key does not belong to this item upload.", "INVALID_OBJECT_KEY");
            uploads.Add((objectKey, uploadId));
        }

        var existingByUrl = new Dictionary<string, ItemImage>(StringComparer.Ordinal);
        foreach (var existing in item!.Images)
            existingByUrl.TryAdd(existing.ImageUrl, existing);
        var newUploads = uploads
            .Where(u => !existingByUrl.ContainsKey(LargeUrl(itemId, u.UploadId)))
            .ToList();

        if (item.Images.Count + newUploads.Count > options.MaxImagesPerItem)
            throw new ConflictException($"An item can contain at most {options.MaxImagesPerItem} images.");

        foreach (var upload in newUploads)
            await EnsureRawObjectUploadedAsync(upload.ObjectKey, cancellationToken);

        var hasPrimary = item.Images.Any(x => x.IsPrimary);
        var nextOrder = item.Images.Count == 0 ? 0 : item.Images.Max(x => x.Order) + 1;
        var now = DateTime.UtcNow;

        foreach (var upload in newUploads)
        {
            var image = new ItemImage
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                ImageUrl = LargeUrl(itemId, upload.UploadId),
                IsPrimary = !hasPrimary,
                Order = nextOrder++,
                CreatedAt = now
            };
            hasPrimary = true;
            dbContext.ItemImages.Add(image);
            existingByUrl[image.ImageUrl] = image;
        }

        if (newUploads.Count > 0)
        {
            item.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var images = uploads
            .Select(u =>
            {
                var image = existingByUrl[LargeUrl(itemId, u.UploadId)];
                return new ConfirmedItemImageDto(
                    image.Id,
                    u.UploadId,
                    image.ImageUrl,
                    $"{ProcessedBaseUrl(itemId, u.UploadId)}/thumbnail.webp",
                    image.IsPrimary,
                    image.Order);
            })
            .ToList();

        return new ConfirmItemImagesResponse(images);
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

        if (metadata.ContentLength <= 0 || metadata.ContentLength > options.MaxFileSizeBytes)
            throw new ValidationException("Uploaded image size is invalid.", "UPLOAD_INVALID_SIZE");
    }

    private string LargeUrl(Guid itemId, Guid uploadId) => $"{ProcessedBaseUrl(itemId, uploadId)}/large.webp";

    /// <summary>
    /// URL công khai của thư mục ảnh đã xử lý. Ưu tiên CloudFront; local (LocalStack) dùng
    /// path-style ServiceUrl; còn lại là S3 regional endpoint.
    /// </summary>
    private string ProcessedBaseUrl(Guid itemId, Guid uploadId)
    {
        return $"{MediaUrls.Root(options)}/{MediaUploadRules.ProcessedPrefix(itemId, uploadId)}";
    }
}
