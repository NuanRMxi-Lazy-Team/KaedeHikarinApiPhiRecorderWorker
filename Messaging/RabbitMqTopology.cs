using RabbitMQ.Client;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public static class RabbitMqTopology
{
    public const string DefaultExchangeName = "";

    public const string TaskQueue = "phi.render.tasks";

    public const string EventQueue = "phi.render.events";

    public const string ControlExchange = "phi.render.control";

    public const string DeadLetterExchange = "phi.render.dlx";

    public const string DeadLetterQueue = "phi.render.dlx.queue";

    public const string DeadLetterRoutingKey = "dead";

    public const string ContractVersionHeader = "contract-version";

    public const string ContractVersion = "1";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken);
        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            DeadLetterRoutingKey,
            arguments: null,
            noWait: false,
            cancellationToken);

        await channel.QueueDeclareAsync(
            TaskQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = DeadLetterRoutingKey,
            },
            passive: false,
            noWait: false,
            cancellationToken);
        await channel.QueueDeclareAsync(
            EventQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken);
        await channel.ExchangeDeclareAsync(
            ControlExchange,
            ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken);
    }
}
