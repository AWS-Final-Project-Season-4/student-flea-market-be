namespace StudentFleaMarket.Infrastructure.Media;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; }
    public bool ForcePathStyle { get; set; }
    public int PresignedUrlExpiryMinutes { get; set; } = 10;
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxImagesPerItem { get; set; } = 8;
    public string? CloudFrontBaseUrl { get; set; }
}
