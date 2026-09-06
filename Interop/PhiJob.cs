using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Interop;

public readonly record struct PhiJobSnapshot(
    ulong JobId,
    PhiJobState State,
    double Progress,
    double Fps,
    double EstimatedSeconds,
    double DurationSeconds,
    ulong Frame,
    ulong TotalFrames)
{
    public bool IsTerminal => State is PhiJobState.Done or PhiJobState.Canceled or PhiJobState.Failed;
}

public sealed record PhiJobEventData
{
    public required ulong JobId { get; init; }
    public required PhiJobState State { get; init; }
    public required double Progress { get; init; }
    public required double Fps { get; init; }
    public required double EstimatedSeconds { get; init; }
    public required double DurationSeconds { get; init; }
    public required string Message { get; init; }

    public bool IsTerminal => State is PhiJobState.Done or PhiJobState.Canceled or PhiJobState.Failed;
}

public sealed class PhiRenderRequest
{
    public required string ChartPath { get; init; }
    public required string OutputPath { get; init; }
    public required PhiRenderConfig Config { get; init; }
    public PhiChartInfo? ChartInfo { get; init; }
}

/// <summary>
/// Owns one native <c>phi_job_t</c>. Events arrive through <see cref="Events"/>
/// from the native dispatcher thread and must never re-enter this library.
/// Disposing cancels the job, force-terminates the private host when needed and
/// then releases the handle.
/// </summary>
public sealed class PhiJob : IDisposable
{
    private readonly PhiRenderer _renderer;
    private readonly Channel<PhiJobEventData> _events = Channel.CreateUnbounded<PhiJobEventData>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
    private readonly TaskCompletionSource<PhiJobEventData> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PhiJobCallbackFn _callbackDelegate;
    private readonly GCHandle _selfHandle;
    private CancellationTokenRegistration _cancellationRegistration;
    private nint _job;
    private int _disposed;

    internal PhiJobCallbackFn CallbackDelegate => _callbackDelegate;
    internal nint UserData => GCHandle.ToIntPtr(_selfHandle);

    internal unsafe PhiJob(PhiRenderer renderer)
    {
        _renderer = renderer;
        _callbackDelegate = NativeCallback;
        _selfHandle = GCHandle.Alloc(this);
    }

    /// <summary>Reads all job events, including the terminal event.</summary>
    public ChannelReader<PhiJobEventData> Events => _events.Reader;

    public PhiJobSnapshot Snapshot
    {
        get
        {
            EnsureNotDisposed();
            unsafe
            {
                PhiJobSnapshotStruct native = default;
                PhiNative.phi_job_get_snapshot(_job, &native).ThrowIfError("phi_job_get_snapshot");
                return new PhiJobSnapshot(
                    native.JobId,
                    native.State,
                    native.Progress,
                    native.Fps,
                    native.EstimatedSeconds,
                    native.DurationSeconds,
                    native.Frame,
                    native.TotalFrames);
            }
        }
    }

    /// <summary>Waits until the job reaches a terminal state and returns the terminal event.</summary>
    public Task<PhiJobEventData> WaitForCompletionAsync(CancellationToken cancellationToken = default) =>
        _completion.Task.WaitAsync(cancellationToken);

    public void Cancel()
    {
        EnsureNotDisposed();
        PhiNative.phi_job_cancel(_job).ThrowIfError("phi_job_cancel");
    }

    public void Pause()
    {
        EnsureNotDisposed();
        PhiNative.phi_job_pause(_job).ThrowIfError("phi_job_pause");
    }

    public void Resume()
    {
        EnsureNotDisposed();
        PhiNative.phi_job_resume(_job).ThrowIfError("phi_job_resume");
    }

    internal void Attach(nint job) => _job = job;

    internal void RegisterCancellation(CancellationToken cancellationToken) =>
        _cancellationRegistration = cancellationToken.Register(static state =>
        {
            var job = (PhiJob)state!;
            try
            {
                job.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (PhiException)
            {
            }
        }, this);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cancellationRegistration.Dispose();

        if (_job != 0)
        {
            PhiNative.phi_job_destroy(_job);
            _job = 0;
        }

        // The dispatcher thread is joined by phi_job_destroy, so no further
        // callbacks can run and the user-data handle can be released safely.
        _selfHandle.Free();
        _events.Writer.TryComplete();
        _completion.TrySetResult(new PhiJobEventData
        {
            JobId = 0,
            State = PhiJobState.Canceled,
            Progress = 0,
            Fps = 0,
            EstimatedSeconds = 0,
            DurationSeconds = 0,
            Message = "job disposed",
        });
    }

    private static unsafe void NativeCallback(PhiJobEventStruct* evt, void* userData)
    {
        if (evt == null || userData == null)
        {
            return;
        }

        var self = (PhiJob?)GCHandle.FromIntPtr((nint)userData).Target;
        if (self is null || Volatile.Read(ref self._disposed) != 0)
        {
            return;
        }

        var data = new PhiJobEventData
        {
            JobId = evt->JobId,
            State = evt->State,
            Progress = evt->Progress,
            Fps = evt->Fps,
            EstimatedSeconds = evt->EstimatedSeconds,
            DurationSeconds = evt->DurationSeconds,
            Message = Utf8.Decode(evt->Message.Data, evt->Message.Length),
        };

        try
        {
            self._events.Writer.TryWrite(data);
        }
        catch (Exception)
        {
            // Never let an exception escape the native callback.
        }

        if (data.IsTerminal)
        {
            self._completion.TrySetResult(data);
        }
    }

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(PhiJob));
        }
    }
}
