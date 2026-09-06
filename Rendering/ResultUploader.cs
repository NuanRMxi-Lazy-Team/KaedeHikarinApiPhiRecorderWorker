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
    private readonly bool _useHttps;

    public ResultUploader(
        IOptions<S3Options> s3Options,
        IOptions<RenderWorkerOptions> workerOptions,
        ILogger<ResultUploader> logger)
    {
        _s3Options = s3Options.Value;
        _workerOptions = workerOptions.Value;
        _logger = logger;
        _useHttps = _s3Options.ServiceUrl.StartsWith(
            "https://",
            StringComparison.OrdinalIgnoreCase
        );
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
        var request = new PutObjectRequest
        {
            BucketName = _s3Options.BucketName,
            Key = objectKey,
            InputStream = new FileStream(
                localFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                65536,
                FileOptions.Asynchronous | FileOptions.SequentialScan),
            ContentType = "video/mp4",
            AutoCloseStream = true,
            // R2/MinIO 等兼容存储不支持 STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER
            // 签名（AWSSDK v4 默认行为），禁用分块编码；HTTPS 端点（R2）另关闭负载签名。
            UseChunkEncoding = false,
        };
        if (_useHttps)
        {
            // R2 不支持 Streaming SigV4，必须禁用负载签名（仅 HTTPS 端点允许）
            request.DisablePayloadSigning = true;
        }
        request.DisableDefaultChecksumValidation = true;

        await client.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("render output uploaded to {Bucket}/{Key}", _s3Options.BucketName, objectKey);

        var expiresAt = DateTime.UtcNow.Add(_workerOptions.OutputUrlLifetime);
        var presignedUrl = client.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _s3Options.BucketName,
                Key = objectKey,
                Expires = expiresAt,
                Protocol = _useHttps ? Protocol.HTTPS : Protocol.HTTP,
            });
        return new UploadedResult(presignedUrl, new DateTimeOffset(expiresAt));
    }
}
