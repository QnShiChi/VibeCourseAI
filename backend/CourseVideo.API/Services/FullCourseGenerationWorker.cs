using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class FullCourseGenerationWorker : BackgroundService
{
    private readonly IFullCourseJobQueue _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IJobCancellationTracker _cancellationTracker;
    private readonly ILogger<FullCourseGenerationWorker> _logger;

    public FullCourseGenerationWorker(
        IFullCourseJobQueue jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        IJobCancellationTracker cancellationTracker,
        ILogger<FullCourseGenerationWorker> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _cancellationTracker = cancellationTracker;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var generationJobRepository = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
        
        var allJobs = await generationJobRepository.GetAllAsync();
        var recoverableJobs = allJobs.Where(j => j.JobType == "GenerateFullCourse" && (j.Status == "Pending" || j.Status == "GeneratingFullCourse")).ToList();

        foreach (var courseJobs in recoverableJobs
            .Where(job => job.CourseId.HasValue)
            .GroupBy(job => job.CourseId!.Value))
        {
            var jobsByPriority = courseJobs
                .OrderByDescending(job => job.UpdatedAt ?? job.StartedAt ?? job.CreatedAt)
                .ToList();

            var jobToResume = jobsByPriority[0];
            foreach (var supersededJob in jobsByPriority.Skip(1))
            {
                supersededJob.Status = "Failed";
                supersededJob.ErrorMessage = "Job cũ đã bị dừng khi hệ thống khởi động lại để tránh chạy trùng.";
                supersededJob.CompletedAt = DateTime.UtcNow;
                supersededJob.UpdatedAt = DateTime.UtcNow;
            }

            _jobQueue.Enqueue(jobToResume.Id);
        }

        await generationJobRepository.SaveChangesAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _jobQueue.DequeueAsync(stoppingToken);
            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _cancellationTracker.RegisterJob(jobId, jobCts);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFullCourseGenerationService>();
                await service.ProcessJobAsync(jobId, jobCts.Token);
            }
            catch (OperationCanceledException) when (jobCts.Token.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Job {JobId} was explicitly cancelled.", jobId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Full course generation background job {JobId} failed unexpectedly.", jobId);
                await TryMarkJobAsFailedAsync(jobId, exception);
            }
            finally
            {
                _cancellationTracker.UnregisterJob(jobId);
            }
        }
    }

    private async Task TryMarkJobAsFailedAsync(Guid jobId, Exception exception)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var generationJobRepository = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
            var job = await generationJobRepository.GetByIdAsync(jobId);
            if (job is null)
            {
                return;
            }

            job.Status = "Failed";
            job.ErrorMessage = exception.Message;
            job.ProgressMessage = "Job generate full course kết thúc với lỗi hệ thống.";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await generationJobRepository.SaveChangesAsync();
        }
        catch (Exception saveException)
        {
            _logger.LogError(saveException, "Failed to persist failed status for full-course generation job {JobId}.", jobId);
        }
    }
}
