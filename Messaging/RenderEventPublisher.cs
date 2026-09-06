using System.Text.Json;
using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;
using RabbitMQ.Client;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public sealed class RenderEventPublisher : IAsyncDisposable
{
    private readonly RabbitMqClientFactory _factory;
    private readonly ILogger<RenderEventPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RenderEventPublisher(RabbitMqClientFactory factory, ILogger<RenderEventPublisher> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async ValueTask PublishAsync(RenderEventMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);
        var body = JsonSerializer.SerializeToUtf8Bytes(message, ContractJson.Options);
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.JobId.ToString("N"),
            Headers = new Dictionary<string, object?>
            {
                [RabbitMqTopology.ContractVersionHeader] = RabbitMqTopology.ContractVersion,
            },
        };
        await channel.BasicPublishAsync(
            RabbitMqTopology.DefaultExchangeName,
            RabbitMqTopology.EventQueue,
            mandatory: true,
            properties,
            body,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: cancellationToken);
            await RabbitMqTopology.DeclareAsync(_channel, cancellationToken);
            _logger.LogInformation("RabbitMQ event publisher connected");
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }
}
