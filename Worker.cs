using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker;

/// <summary>
/// 启动就绪探测：验证原生渲染运行时可用（ABI 版本匹配、上下文可创建）。
/// 仅做探测与日志，实际渲染由 <see cref="Messaging.RenderTaskConsumer"/> 驱动。
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    PhiRenderer renderer) : BackgroundService
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

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
