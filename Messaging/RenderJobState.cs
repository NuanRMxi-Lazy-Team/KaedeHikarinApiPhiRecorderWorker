using Microsoft.Extensions.Options;

namespace KaedeHikarinCialloTeam.PhiRecorder.Worker.Messaging;

/// <summary>
/// 跨后台服务的任务状态：当前在跑任务（供取消）、以及"已取消但可能尚未出队"的任务缓存。
/// </summary>
public sealed class RenderJobState
{
    private readonly object _gate = new();
    private readonly TimeSpan _canceledTtl;
    private Guid? _currentJobId;
    private CancellationTokenSource? _currentJobCts;
    private readonly Dictionary<Guid, DateTime> _canceledJobs = new();

    public RenderJobState(IOptions<RenderWorkerOptions> options)
    {
        _canceledTtl = options.Value.QueueWaitTimeout;
    }

    public void BeginJob(Guid jobId, CancellationTokenSource cancellationTokenSource)
    {
        lock (_gate)
        {
            _currentJobId = jobId;
            _currentJobCts = cancellationTokenSource;
        }
    }

    public void EndJob(Guid jobId)
    {
        lock (_gate)
        {
            if (_currentJobId == jobId)
            {
                _currentJobId = null;
                _currentJobCts = null;
            }
        }
    }

    /// <summary>来自控制队列的取消：在跑则立即取消，同时记录以便出队时识别。</summary>
    public void RequestCancel(Guid jobId)
    {
        lock (_gate)
        {
            _canceledJobs[jobId] = DateTime.UtcNow.Add(_canceledTtl);
            if (_currentJobId == jobId)
            {
                _currentJobCts?.Cancel();
            }
        }
    }

    /// <summary>任务出队时检查：该任务是否已被取消（顺带清理过期记录）。</summary>
    public bool IsCanceled(Guid jobId)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var expired = _canceledJobs.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToList();
            foreach (var key in expired)
            {
                _canceledJobs.Remove(key);
            }

            return _canceledJobs.ContainsKey(jobId);
        }
    }
}
