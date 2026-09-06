using KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;
using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Tests.Messaging;

public class RenderJobStateTests
{
    private static RenderJobState CreateState(TimeSpan queueWaitTimeout) =>
        new(Options.Create(new RenderWorkerOptions { QueueWaitTimeout = queueWaitTimeout }));

    [Fact]
    public void RequestCancel_OnCurrentJob_CancelsToken()
    {
        var state = CreateState(TimeSpan.FromMinutes(30));
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        state.BeginJob(jobId, cts);

        state.RequestCancel(jobId);

        Assert.True(cts.IsCancellationRequested);
        Assert.True(state.IsCanceled(jobId));
    }

    [Fact]
    public void RequestCancel_OnQueuedJob_MarksCanceled()
    {
        var state = CreateState(TimeSpan.FromMinutes(30));
        var jobId = Guid.NewGuid();

        state.RequestCancel(jobId);

        Assert.True(state.IsCanceled(jobId));
    }

    [Fact]
    public void IsCanceled_Expires_AfterQueueWaitTimeout()
    {
        var state = CreateState(TimeSpan.FromMilliseconds(50));
        var jobId = Guid.NewGuid();
        state.RequestCancel(jobId);
        Assert.True(state.IsCanceled(jobId));

        Thread.Sleep(80);

        Assert.False(state.IsCanceled(jobId));
    }

    [Fact]
    public void EndJob_ClearsCurrentJob()
    {
        var state = CreateState(TimeSpan.FromMinutes(30));
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        state.BeginJob(jobId, cts);

        state.EndJob(jobId);

        using var other = new CancellationTokenSource();
        state.BeginJob(Guid.NewGuid(), other);
        state.RequestCancel(jobId);
        Assert.False(other.IsCancellationRequested);
    }
}
