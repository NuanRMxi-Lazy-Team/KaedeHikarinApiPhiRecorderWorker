namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging.Contracts;

public enum RenderEventType
{
    Started = 0,
    Progress = 1,
    Done = 2,
    Failed = 3,
    Canceled = 4,
}

public enum RenderFailReason
{
    None = 0,
    QueueWaitTimeout = 1,
    ExecutionTimeout = 2,
    ChartDownloadFailed = 3,
    UploadFailed = 4,
    RenderFailed = 5,
    MalformedMessage = 6,
}

public sealed class RenderEventMessage
{
    public Guid JobId { get; set; }

    public RenderEventType EventType { get; set; }

    public RenderFailReason Reason { get; set; }

    public double Progress { get; set; }

    public double Fps { get; set; }

    public ulong Frame { get; set; }

    public ulong TotalFrames { get; set; }

    public string? OutputUrl { get; set; }

    public DateTimeOffset? OutputUrlExpiresAtUtc { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public static RenderEventMessage Started(Guid jobId) =>
        new() { JobId = jobId, EventType = RenderEventType.Started };

    public static RenderEventMessage ProgressUpdate(
        Guid jobId,
        double progress,
        double fps,
        ulong frame,
        ulong totalFrames) =>
        new()
        {
            JobId = jobId,
            EventType = RenderEventType.Progress,
            Progress = progress,
            Fps = fps,
            Frame = frame,
            TotalFrames = totalFrames,
        };

    public static RenderEventMessage Done(
        Guid jobId,
        string outputUrl,
        DateTimeOffset? outputUrlExpiresAtUtc) =>
        new()
        {
            JobId = jobId,
            EventType = RenderEventType.Done,
            OutputUrl = outputUrl,
            OutputUrlExpiresAtUtc = outputUrlExpiresAtUtc,
        };

    public static RenderEventMessage Failed(Guid jobId, RenderFailReason reason, string error) =>
        new() { JobId = jobId, EventType = RenderEventType.Failed, Reason = reason, Error = error };

    public static RenderEventMessage Canceled(Guid jobId) =>
        new() { JobId = jobId, EventType = RenderEventType.Canceled };
}
