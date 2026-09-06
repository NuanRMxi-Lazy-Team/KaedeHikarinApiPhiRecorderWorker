using KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;
using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;

public sealed class RenderJobProcessor
{
    private readonly PhiRenderer _renderer;
    private readonly PhiRendererOptions _rendererOptions;
    private readonly RenderWorkerOptions _workerOptions;
    private readonly RenderEventPublisher _publisher;
    private readonly RenderJobState _jobState;
    private readonly ChartDownloader _chartDownloader;
    private readonly ResultUploader _resultUploader;
    private readonly ILogger<RenderJobProcessor> _logger;

    public RenderJobProcessor(
        PhiRenderer renderer,
        IOptions<PhiRendererOptions> rendererOptions,
        IOptions<RenderWorkerOptions> workerOptions,
        RenderEventPublisher publisher,
        RenderJobState jobState,
        ChartDownloader chartDownloader,
        ResultUploader resultUploader,
        ILogger<RenderJobProcessor> logger)
    {
        _renderer = renderer;
        _rendererOptions = rendererOptions.Value;
        _workerOptions = workerOptions.Value;
        _publisher = publisher;
        _jobState = jobState;
        _chartDownloader = chartDownloader;
        _resultUploader = resultUploader;
        _logger = logger;
    }

    public async Task ProcessAsync(RenderTaskMessage task, CancellationToken stoppingToken)
    {
        using var timeoutCts = new CancellationTokenSource(_workerOptions.JobExecutionTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
        _jobState.BeginJob(task.JobId, linkedCts);

        string? workDirectory = null;
        try
        {
            await PublishFinalAsync(RenderEventMessage.Started(task.JobId), stoppingToken);

            workDirectory = Path.Combine(
                _rendererOptions.ResolveTempDirectory(),
                "jobs",
                task.JobId.ToString("N"));
            Directory.CreateDirectory(workDirectory);
            var chartPath = await _chartDownloader.DownloadAsync(
                task.ChartPresignedUrl,
                workDirectory,
                linkedCts.Token);
            var outputPath = Path.Combine(workDirectory, "output.mp4");

            var config = task.Config.ToPhiRenderConfig();
            config.Validate();

            using var job = _renderer.SubmitRender(
                new PhiRenderRequest
                {
                    ChartPath = chartPath,
                    OutputPath = outputPath,
                    Config = config,
                },
                linkedCts.Token);

            var lastProgressPublish = DateTime.MinValue;
            PhiJobEventData? terminalEvent = null;
            await foreach (var evt in job.Events.ReadAllAsync(stoppingToken))
            {
                if (!evt.IsTerminal)
                {
                    var now = DateTime.UtcNow;
                    if (now - lastProgressPublish >= _workerOptions.ProgressPublishInterval)
                    {
                        lastProgressPublish = now;
                        var progressSnapshot = job.Snapshot;
                        await PublishProgressSafeAsync(
                            RenderEventMessage.ProgressUpdate(
                                task.JobId,
                                progressSnapshot.Progress,
                                progressSnapshot.Fps,
                                progressSnapshot.Frame,
                                progressSnapshot.TotalFrames),
                            stoppingToken);
                    }

                    continue;
                }

                terminalEvent = evt;
                break;
            }

            if (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("job {JobId} exceeded execution timeout", task.JobId);
                await PublishFinalAsync(
                    RenderEventMessage.Failed(
                        task.JobId,
                        RenderFailReason.ExecutionTimeout,
                        $"job exceeded execution timeout of {_workerOptions.JobExecutionTimeout}"),
                    stoppingToken);
                return;
            }

            if (stoppingToken.IsCancellationRequested || _jobState.IsCanceled(task.JobId))
            {
                await PublishFinalAsync(RenderEventMessage.Canceled(task.JobId), stoppingToken);
                return;
            }

            var snapshot = job.Snapshot;
            switch (snapshot.State)
            {
                case PhiJobState.Done:
                {
                    var uploaded = await _resultUploader.UploadAsync(
                        outputPath,
                        task.OutputObjectKey,
                        stoppingToken);
                    await PublishFinalAsync(
                        RenderEventMessage.Done(task.JobId, uploaded.PresignedUrl, uploaded.ExpiresAtUtc),
                        stoppingToken);
                    break;
                }

                case PhiJobState.Failed:
                {
                    var error = terminalEvent?.Message;
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        error = _renderer.GetLastError() ?? "native render failed";
                    }
                    await PublishFinalAsync(
                        RenderEventMessage.Failed(task.JobId, RenderFailReason.RenderFailed, error),
                        stoppingToken);
                    break;
                }

                default:
                    await PublishFinalAsync(RenderEventMessage.Canceled(task.JobId), stoppingToken);
                    break;
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("job {JobId} timed out during pipeline stage", task.JobId);
            await PublishFinalAsync(
                RenderEventMessage.Failed(
                    task.JobId,
                    RenderFailReason.ExecutionTimeout,
                    $"job exceeded execution timeout of {_workerOptions.JobExecutionTimeout}"),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            await PublishFinalAsync(RenderEventMessage.Canceled(task.JobId), stoppingToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "job {JobId} chart download failed", task.JobId);
            await PublishFinalAsync(
                RenderEventMessage.Failed(task.JobId, RenderFailReason.ChartDownloadFailed, exception.Message),
                stoppingToken);
        }
        catch (Exception exception)
        {
            var nativeError = _renderer.GetLastError();
            var message = nativeError is null
                ? exception.Message
                : $"{exception.Message} | native: {nativeError}";
            _logger.LogError(exception, "job {JobId} failed", task.JobId);
            await PublishFinalAsync(
                RenderEventMessage.Failed(task.JobId, RenderFailReason.RenderFailed, message),
                stoppingToken);
            throw;
        }
        finally
        {
            _jobState.EndJob(task.JobId);
            if (workDirectory is not null)
            {
                try
                {
                    Directory.Delete(workDirectory, recursive: true);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "failed to clean up work directory {Path}", workDirectory);
                }
            }
        }
    }

    private async Task PublishProgressSafeAsync(RenderEventMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "failed to publish progress event for job {JobId}", message.JobId);
        }
    }

    private async Task PublishFinalAsync(RenderEventMessage message, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        await _publisher.PublishAsync(message, linked.Token);
    }
}
