namespace CourseVideo.API.Services.Interfaces;

public interface IJobCancellationTracker
{
    void RegisterJob(Guid jobId, CancellationTokenSource cts);
    void UnregisterJob(Guid jobId);
    bool CancelJob(Guid jobId);
}
