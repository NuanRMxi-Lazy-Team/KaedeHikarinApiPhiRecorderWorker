using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public sealed class RabbitMqClientFactory
{
    private readonly RabbitMqOptions _options;

    public RabbitMqClientFactory(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = false,
        };
        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
