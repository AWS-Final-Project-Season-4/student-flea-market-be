using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace StudentFleaMarket.ImageProcessor;

public sealed class Function
{
    private readonly S3ImageProcessor processor;

    public Function()
    {
        var options = ImageProcessorOptions.FromEnvironment();
        processor = new S3ImageProcessor(CreateS3Client(), options, new ImageTransformer(options));
    }

    internal Function(S3ImageProcessor processor)
    {
        this.processor = processor;
    }

    public async Task FunctionHandler(S3Event s3Event, ILambdaContext context)
    {
        foreach (var record in s3Event.Records ?? [])
        {
            var bucketName = record.S3.Bucket.Name;
            var encodedObjectKey = record.S3.Object.Key;
            context.Logger.LogInformation(
                "Processing S3 record {EventName} for bucket {Bucket}; request {RequestId}.",
                record.EventName,
                bucketName,
                context.AwsRequestId);

            await processor.ProcessAsync(
                bucketName,
                encodedObjectKey,
                record.S3.Object.Size,
                CancellationToken.None);
        }
    }

    private static IAmazonS3 CreateS3Client()
    {
        var endpoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");
        if (string.IsNullOrWhiteSpace(endpoint))
            return new AmazonS3Client();
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("AWS_ENDPOINT_URL must be an absolute HTTP or HTTPS URL.");

        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-1";
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = region,
            RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            ForcePathStyle = true
        };

        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        return !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config)
            : new AmazonS3Client(config);
    }
}
