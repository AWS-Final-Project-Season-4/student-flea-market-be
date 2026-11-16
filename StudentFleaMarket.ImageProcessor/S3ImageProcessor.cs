using Amazon.S3;
using Amazon.S3.Model;

namespace StudentFleaMarket.ImageProcessor;

public sealed class S3ImageProcessor(
    IAmazonS3 s3,
    ImageProcessorOptions options,
    ImageTransformer transformer)
{
    public async Task ProcessAsync(
        string bucketName,
        string encodedObjectKey,
        long eventObjectSize,
        CancellationToken cancellationToken = default)
    {
        var objectKey = Uri.UnescapeDataString(encodedObjectKey.Replace('+', ' '));
        if (!MediaObjectKey.TryParseRaw(objectKey, out var mediaKey) || mediaKey is null)
            throw new InvalidDataException("The S3 object key does not match the expected raw media path.");
        if (eventObjectSize > options.MaxSourceBytes)
            throw new InvalidDataException($"The S3 event reports an object larger than {options.MaxSourceBytes} bytes.");

        using var source = await s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        }, cancellationToken);
        if (source.ContentLength > options.MaxSourceBytes)
            throw new InvalidDataException($"The source object exceeds the {options.MaxSourceBytes} byte limit.");

        var variants = await transformer.TransformAsync(
            source.ResponseStream,
            source.Headers.ContentType,
            mediaKey.IsAvatar ? ImageShape.Avatar : ImageShape.Listing,
            cancellationToken);

        foreach (var variant in variants)
        {
            await using var content = new MemoryStream(variant.Content, writable: false);
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = $"{mediaKey.ProcessedPrefix}/{variant.Name}",
                InputStream = content,
                ContentType = "image/webp",
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };
            request.Headers.CacheControl = "public,max-age=31536000,immutable";
            await s3.PutObjectAsync(request, cancellationToken);
        }
    }
}
