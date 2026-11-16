using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentFleaMarket.Application.Media;

namespace StudentFleaMarket.Infrastructure.Media;

public static class MediaServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký upload ảnh tin đăng qua S3 presigned POST (LocalStack khi có ServiceUrl).
    /// </summary>
    public static IServiceCollection AddMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var mediaStorage = configuration.GetSection(MediaStorageOptions.SectionName).Get<MediaStorageOptions>()
            ?? throw new InvalidOperationException("MediaStorage configuration is required.");
        ValidateMediaStorage(mediaStorage);

        services.AddSingleton(mediaStorage);
        services.AddSingleton(new MediaUploadLimits(mediaStorage.MaxFileSizeBytes, mediaStorage.MaxImagesPerItem));
        services.AddSingleton<IAmazonS3>(_ => CreateS3Client(mediaStorage));
        services.AddScoped<IMediaUploadService, S3MediaUploadService>();
        services.AddScoped<IAvatarUploadService, S3AvatarUploadService>();
        return services;
    }

    private static void ValidateMediaStorage(MediaStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BucketName))
            throw new InvalidOperationException("MediaStorage:BucketName is required.");
        if (string.IsNullOrWhiteSpace(options.Region))
            throw new InvalidOperationException("MediaStorage:Region is required.");
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) &&
            (!Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var serviceUri) ||
             (serviceUri.Scheme != Uri.UriSchemeHttp && serviceUri.Scheme != Uri.UriSchemeHttps)))
            throw new InvalidOperationException("MediaStorage:ServiceUrl must be an absolute HTTP or HTTPS URL.");
        if (options.PresignedUrlExpiryMinutes is < 1 or > 60)
            throw new InvalidOperationException("MediaStorage:PresignedUrlExpiryMinutes must be between 1 and 60.");
        if (options.MaxFileSizeBytes <= 0)
            throw new InvalidOperationException("MediaStorage:MaxFileSizeBytes must be greater than zero.");
        if (options.MaxImagesPerItem is < 1 or > 20)
            throw new InvalidOperationException("MediaStorage:MaxImagesPerItem must be between 1 and 20.");
        if (!string.IsNullOrWhiteSpace(options.CloudFrontBaseUrl) &&
            (!Uri.TryCreate(options.CloudFrontBaseUrl, UriKind.Absolute, out var cdnUri) || cdnUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("MediaStorage:CloudFrontBaseUrl must be an HTTPS URL.");
    }

    private static IAmazonS3 CreateS3Client(MediaStorageOptions options)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
            ForcePathStyle = options.ForcePathStyle
        };

        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
            return new AmazonS3Client(config);

        config.ServiceURL = options.ServiceUrl;
        config.AuthenticationRegion = options.Region;

        // LocalStack accepts non-production credentials. This prevents local development
        // from reading or sending credentials from the developer's real AWS profile.
        return new AmazonS3Client(new BasicAWSCredentials("test", "test"), config);
    }
}
