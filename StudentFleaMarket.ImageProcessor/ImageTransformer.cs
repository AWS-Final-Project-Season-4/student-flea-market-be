using ImageMagick;

namespace StudentFleaMarket.ImageProcessor;

public sealed record ImageVariant(string Name, byte[] Content);

/// <summary>Listing photos keep their aspect ratio; avatars are center-cropped to a square.</summary>
public enum ImageShape
{
    Listing,
    Avatar
}

public sealed class ImageTransformer(ImageProcessorOptions options)
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public Task<IReadOnlyList<ImageVariant>> TransformAsync(
        Stream source,
        string? declaredContentType,
        CancellationToken cancellationToken = default) =>
        TransformAsync(source, declaredContentType, ImageShape.Listing, cancellationToken);

    public const int AvatarLargeSize = 512;
    public const int AvatarThumbnailSize = 128;

    public async Task<IReadOnlyList<ImageVariant>> TransformAsync(
        Stream source,
        string? declaredContentType,
        ImageShape shape,
        CancellationToken cancellationToken = default)
    {
        await using var buffered = new MemoryStream();
        await CopyWithLimitAsync(source, buffered, options.MaxSourceBytes, cancellationToken);
        buffered.Position = 0;

        var bytes = buffered.ToArray();
        var info = new MagickImageInfo(bytes);
        var detectedContentType = ContentTypeFor(info.Format);
        if (detectedContentType is null || !AllowedMimeTypes.Contains(detectedContentType))
            throw new InvalidDataException($"Unsupported image format: {info.Format}.");
        if (!string.IsNullOrWhiteSpace(declaredContentType) &&
            !detectedContentType.Equals(declaredContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The declared Content-Type does not match the image bytes.");
        if ((long)info.Width * info.Height > options.MaxPixels)
            throw new InvalidDataException($"The image exceeds the {options.MaxPixels} pixel limit.");

        cancellationToken.ThrowIfCancellationRequested();
        using var image = new MagickImage(bytes);
        image.AutoOrient();
        image.Strip();

        if (shape == ImageShape.Avatar)
        {
            // Center-crop to a square so every avatar renders as a clean circle.
            var side = Math.Min(image.Width, image.Height);
            image.Crop(new MagickGeometry(side, side) { IgnoreAspectRatio = true }, Gravity.Center);
            image.ResetPage();
            return
            [
                new ImageVariant("large.webp", Render(image, AvatarLargeSize, cancellationToken)),
                new ImageVariant("thumbnail.webp", Render(image, AvatarThumbnailSize, cancellationToken))
            ];
        }

        return
        [
            new ImageVariant("large.webp", Render(image, options.LargeMaxWidth, cancellationToken)),
            new ImageVariant("thumbnail.webp", Render(image, options.ThumbnailMaxWidth, cancellationToken))
        ];
    }

    private byte[] Render(MagickImage source, int maxWidth, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var variant = source.Clone();
        if (variant.Width > maxWidth)
        {
            variant.Resize(new MagickGeometry((uint)maxWidth, (uint)maxWidth)
            {
                Greater = true
            });
        }
        variant.Format = MagickFormat.WebP;
        variant.Quality = (uint)options.WebpQuality;
        variant.Strip();
        return variant.ToByteArray();
    }

    private static string? ContentTypeFor(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg or MagickFormat.Jpg => "image/jpeg",
        MagickFormat.Png => "image/png",
        MagickFormat.WebP => "image/webp",
        _ => null
    };

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long limit,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            total += read;
            if (total > limit)
                throw new InvalidDataException($"The source object exceeds the {limit} byte limit.");
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }
}
