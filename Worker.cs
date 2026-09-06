using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;
using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker;

/// <summary>
/// 启动就绪探测：验证原生渲染运行时可用（ABI 版本匹配、上下文可创建），
/// 并对关键配置项的缺失发出明确警告。实际渲染由 <see cref="Messaging.RenderTaskConsumer"/> 驱动。
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    PhiRenderer renderer,
    IOptions<S3Options> s3Options,
    IOptions<RabbitMqOptions> rabbitMqOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var probe = renderer.Probe();
            logger.LogInformation(
                "Phi recorder native runtime ready: ABI v{Loaded} (wrapper expects v{Expected})",
                probe.LoadedAbiVersion,
                probe.ExpectedAbiVersion);
            if (probe.LastError is not null)
            {
                logger.LogWarning("Native context last error: {Error}", probe.LastError);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Phi recorder native runtime is unavailable");
        }

        WarnMissingConfiguration(s3Options.Value, rabbitMqOptions.Value);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void WarnMissingConfiguration(S3Options s3, RabbitMqOptions rabbitMq)
    {
        if (string.IsNullOrWhiteSpace(s3.ServiceUrl))
        {
            logger.LogWarning(
                "S3:ServiceUrl 未配置（环境变量 S3__ServiceUrl / 配置节 \"S3\" 的 ServiceUrl）；渲染产物将无法上传");
        }

        if (string.IsNullOrWhiteSpace(s3.BucketName))
        {
            logger.LogWarning(
                "S3:BucketName 未配置（环境变量 S3__BucketName / 配置节 \"S3\" 的 BucketName）；渲染产物将无法上传");
        }

        if (string.IsNullOrWhiteSpace(s3.AccessKey))
        {
            logger.LogWarning(
                "S3:AccessKey 未配置（环境变量 S3__AccessKey / 配置节 \"S3\" 的 AccessKey）");
        }

        if (string.IsNullOrWhiteSpace(s3.SecretKey))
        {
            logger.LogWarning(
                "S3:SecretKey 未配置（环境变量 S3__SecretKey / 配置节 \"S3\" 的 SecretKey）");
        }

        if (string.IsNullOrWhiteSpace(rabbitMq.HostName))
        {
            logger.LogWarning(
                "RabbitMq:HostName 未配置（环境变量 RabbitMq__HostName / 配置节 \"RabbitMq\" 的 HostName）");
        }
    }
}
