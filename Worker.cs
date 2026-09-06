using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker;

public class Worker(
    ILogger<Worker> logger,
    PhiRenderer renderer,
    IOptions<PhiRendererOptions> options) : BackgroundService
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

            var probeChartPath = options.Value.ProbeChartPath;
            if (!string.IsNullOrWhiteSpace(probeChartPath))
            {
                using var chartInfo = renderer.LoadChartInfo(probeChartPath);
                var info = chartInfo.GetView();
                logger.LogInformation(
                    "Probe chart loaded: {Name} [{Level}] by {Charter} ({Format})",
                    info.Name,
                    info.Level,
                    info.Charter,
                    info.Format);
            }

            var smokeChartPath = options.Value.SmokeRenderChartPath;
            var smokeOutputPath = options.Value.SmokeRenderOutputPath;
            if (!string.IsNullOrWhiteSpace(smokeChartPath) && !string.IsNullOrWhiteSpace(smokeOutputPath))
            {
                await RunSmokeRenderAsync(renderer, smokeChartPath, smokeOutputPath, options.Value, logger, stoppingToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Phi recorder native runtime is unavailable");
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task RunSmokeRenderAsync(
        PhiRenderer renderer,
        string chartPath,
        string outputPath,
        PhiRendererOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var config = PhiRenderConfig.CreateDefault();
        config.Width = options.SmokeWidth;
        config.Height = options.SmokeHeight;
        config.Fps = options.SmokeFps;
        config.PlayStartTime = options.SmokeStartTime;
        config.PlayEndTime = options.SmokeEndTime;
        config.HasPlayEndTime = true;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var job = renderer.SubmitRender(
            new PhiRenderRequest
            {
                ChartPath = chartPath,
                OutputPath = outputPath,
                Config = config,
            },
            cancellationToken);

        await foreach (var evt in job.Events.ReadAllAsync(cancellationToken))
        {
            logger.LogInformation(
                "smoke job {JobId} state={State} progress={Progress:P1} fps={Fps:F1} {Message}",
                evt.JobId,
                evt.State,
                evt.Progress,
                evt.Fps,
                evt.Message);
            if (evt.IsTerminal)
            {
                break;
            }
        }

        var snapshot = job.Snapshot;
        logger.LogInformation(
            "smoke render finished: state={State} frame={Frame}/{TotalFrames} duration={DurationSeconds:F2}s output={Output}",
            snapshot.State,
            snapshot.Frame,
            snapshot.TotalFrames,
            snapshot.DurationSeconds,
            outputPath);
    }
}
