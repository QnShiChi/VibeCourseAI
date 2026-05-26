using System.Collections.Concurrent;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class JobCancellationTracker : IJobCancellationTracker
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeJobs = new();

    public void RegisterJob(Guid jobId, CancellationTokenSource cts)
    {
        _activeJobs.AddOrUpdate(jobId, cts, (_, existing) => cts);
    }

    public void UnregisterJob(Guid jobId)
    {
        _activeJobs.TryRemove(jobId, out _);
    }

    public bool CancelJob(Guid jobId)
    {
        if (_activeJobs.TryRemove(jobId, out var cts))
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            return true;
        }
        return false;
    }
}
