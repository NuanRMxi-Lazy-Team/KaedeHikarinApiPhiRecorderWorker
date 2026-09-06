using System.Text.Json;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public sealed class ControlConsumer : BackgroundService
{
    private readonly RabbitMqClientFactory _factory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly RenderJobState _jobState;
    private readonly ILogger<ControlConsumer> _logger;

    public ControlConsumer(
        RabbitMqClientFactory factory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        RenderJobState jobState,
        ILogger<ControlConsumer> logger)
    {
        _factory = factory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _jobState = jobState;
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
                _logger.LogError(exception, "control consumer crashed, reconnecting");
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

        var queue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            passive: false,
            noWait: false,
            stoppingToken);
        await channel.QueueBindAsync(
            queue.QueueName,
            RabbitMqTopology.ControlExchange,
            routingKey: string.Empty,
            arguments: null,
            noWait: false,
            stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => HandleAsync(args, stoppingToken);
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer, stoppingToken);
        _logger.LogInformation(
            "consuming cancel requests from {Exchange} via {Queue}",
            RabbitMqTopology.ControlExchange,
            queue.QueueName);

        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            shutdown.TrySetResult();
            return Task.CompletedTask;
        };
        await Task.WhenAny(shutdown.Task, Task.Delay(Timeout.Infinite, stoppingToken));
    }

    private Task HandleAsync(BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RenderControlMessage>(
                args.Body.Span,
                ContractJson.Options);
            if (message is null || !string.Equals(message.Action, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("cancel request received for job {JobId}", message.JobId);
            _jobState.RequestCancel(message.JobId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "malformed control message ignored");
        }

        return Task.CompletedTask;
    }
}
