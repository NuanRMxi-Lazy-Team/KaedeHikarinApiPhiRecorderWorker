namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed class RenderWorkerOptions
{
    public const string SectionName = "Rendering";

    public TimeSpan QueueWaitTimeout { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan JobExecutionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan ProgressPublishInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan OutputUrlLifetime { get; set; } = TimeSpan.FromDays(7);
}

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "auto";

    public bool ForcePathStyle { get; set; }
}
