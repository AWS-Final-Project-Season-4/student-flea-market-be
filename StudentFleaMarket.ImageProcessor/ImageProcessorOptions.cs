namespace StudentFleaMarket.ImageProcessor;

public sealed record ImageProcessorOptions(
    long MaxSourceBytes,
    long MaxPixels,
    int LargeMaxWidth,
    int ThumbnailMaxWidth,
    int WebpQuality)
{
    public static ImageProcessorOptions FromEnvironment() => new(
        ReadLong("MAX_SOURCE_BYTES", 10 * 1024 * 1024, 1, 100 * 1024 * 1024),
        ReadLong("MAX_PIXELS", 40_000_000, 1, 200_000_000),
        ReadInt("LARGE_MAX_WIDTH", 1600, 1, 10000),
        ReadInt("THUMBNAIL_MAX_WIDTH", 480, 1, 4000),
        ReadInt("WEBP_QUALITY", 82, 1, 100));

    private static int ReadInt(string name, int defaultValue, int minimum, int maximum)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (!int.TryParse(raw, out var value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"{name} must be an integer between {minimum} and {maximum}.");
        return value;
    }

    private static long ReadLong(string name, long defaultValue, long minimum, long maximum)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (!long.TryParse(raw, out var value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"{name} must be an integer between {minimum} and {maximum}.");
        return value;
    }
}
