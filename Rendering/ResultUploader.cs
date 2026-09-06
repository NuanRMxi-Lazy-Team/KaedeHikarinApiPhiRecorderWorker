using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;
using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;

public sealed record UploadedResult(string PresignedUrl, DateTimeOffset ExpiresAtUtc);

public sealed class ResultUploader
{
    private readonly S3Options _s3Options;
    private readonly RenderWorkerOptions _workerOptions;
    private readonly ILogger<ResultUploader> _logger;

    public ResultUploader(
        IOptions<S3Options> s3Options,
        IOptions<RenderWorkerOptions> workerOptions,
        ILogger<ResultUploader> logger)
    {
        _s3Options = s3Options.Value;
        _workerOptions = workerOptions.Value;
        _logger = logger;
    }

    public async Task<UploadedResult> UploadAsync(
        string localFilePath,
        string objectKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_s3Options.ServiceUrl)
            || string.IsNullOrWhiteSpace(_s3Options.BucketName))
        {
            throw new InvalidOperationException("S3 bucket configuration is missing");
        }

        var credentials = new BasicAWSCredentials(_s3Options.AccessKey, _s3Options.SecretKey);
        var sdkConfig = new AmazonS3Config
        {
            ServiceURL = _s3Options.ServiceUrl,
            ForcePathStyle = _s3Options.ForcePathStyle,
            AuthenticationRegion = _s3Options.Region,
        };

        using var client = new AmazonS3Client(credentials, sdkConfig);
        await client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _s3Options.BucketName,
                Key = objectKey,
                FilePath = localFilePath,
            },
            cancellationToken);
        _logger.LogInformation("render output uploaded to {Bucket}/{Key}", _s3Options.BucketName, objectKey);

        var expiresAt = DateTime.UtcNow.Add(_workerOptions.OutputUrlLifetime);
        var presignedUrl = client.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _s3Options.BucketName,
                Key = objectKey,
                Expires = expiresAt,
                Protocol = Protocol.HTTPS,
            });
        return new UploadedResult(presignedUrl, new DateTimeOffset(expiresAt));
    }
}
