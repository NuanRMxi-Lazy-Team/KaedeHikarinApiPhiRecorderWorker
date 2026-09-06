using System.Text.Json;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Rendering;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public sealed class RenderTaskConsumer : BackgroundService
{
    private readonly RabbitMqClientFactory _factory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly RenderWorkerOptions _workerOptions;
    private readonly RenderEventPublisher _publisher;
    private readonly RenderJobState _jobState;
    private readonly RenderJobProcessor _processor;
    private readonly ILogger<RenderTaskConsumer> _logger;

    public RenderTaskConsumer(
        RabbitMqClientFactory factory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<RenderWorkerOptions> workerOptions,
        RenderEventPublisher publisher,
        RenderJobState jobState,
        RenderJobProcessor processor,
        ILogger<RenderTaskConsumer> logger)
    {
        _factory = factory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _workerOptions = workerOptions.Value;
        _publisher = publisher;
        _jobState = jobState;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilDisconnectedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "render task consumer crashed, reconnecting");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_rabbitMqOptions.ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(options: null, cancellationToken: stoppingToken);
        await RabbitMqTopology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => HandleAsync(args, channel, stoppingToken);
        await channel.BasicConsumeAsync(
            RabbitMqTopology.TaskQueue,
            autoAck: false,
            consumer,
            stoppingToken);
        _logger.LogInformation("consuming render tasks from {Queue}", RabbitMqTopology.TaskQueue);

        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            shutdown.TrySetResult();
            return Task.CompletedTask;
        };
        await Task.WhenAny(shutdown.Task, Task.Delay(Timeout.Infinite, stoppingToken));
        _logger.LogWarning("render task consumer connection closed");
    }

    private async Task HandleAsync(
        BasicDeliverEventArgs args,
        IChannel channel,
        CancellationToken stoppingToken)
    {
        RenderTaskMessage? task;
        try
        {
            task = JsonSerializer.Deserialize<RenderTaskMessage>(args.Body.Span, ContractJson.Options);
            if (task is null)
            {
                throw new JsonException("empty message body");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "malformed render task message rejected");
            await _publisher.PublishAsync(
                RenderEventMessage.Failed(Guid.Empty, RenderFailReason.MalformedMessage, exception.Message),
                stoppingToken);
            await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            return;
        }

        var queuedFor = DateTimeOffset.UtcNow - task.SubmittedAtUtc;
        if (queuedFor > _workerOptions.QueueWaitTimeout)
        {
            _logger.LogWarning("job {JobId} timed out after {QueuedFor} in queue", task.JobId, queuedFor);
            await _publisher.PublishAsync(
                RenderEventMessage.Failed(
                    task.JobId,
                    RenderFailReason.QueueWaitTimeout,
                    $"queued for {queuedFor}, exceeds {_workerOptions.QueueWaitTimeout}"),
                stoppingToken);
            await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            return;
        }

        if (_jobState.IsCanceled(task.JobId))
        {
            _logger.LogInformation("job {JobId} was canceled while queued", task.JobId);
            await _publisher.PublishAsync(RenderEventMessage.Canceled(task.JobId), stoppingToken);
            await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            return;
        }

        _logger.LogInformation("job {JobId} dequeued after {QueuedFor}", task.JobId, queuedFor);
        try
        {
            await _processor.ProcessAsync(task, stoppingToken);
            await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "job {JobId} processing crashed", task.JobId);
            await _publisher.PublishAsync(
                RenderEventMessage.Failed(task.JobId, RenderFailReason.RenderFailed, exception.Message),
                stoppingToken);
            await channel.BasicRejectAsync(args.DeliveryTag, requeue: false, stoppingToken);
        }
    }
}
