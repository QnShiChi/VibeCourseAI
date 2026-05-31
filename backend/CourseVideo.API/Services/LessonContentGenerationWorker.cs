using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonContentGenerationWorker : BackgroundService
{
    private readonly IGenerationJobQueue _generationJobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IJobCancellationTracker _cancellationTracker;
    private readonly ILogger<LessonContentGenerationWorker> _logger;

    public LessonContentGenerationWorker(
        IGenerationJobQueue generationJobQueue,
        IServiceScopeFactory serviceScopeFactory,
        IJobCancellationTracker cancellationTracker,
        ILogger<LessonContentGenerationWorker> logger)
    {
        _generationJobQueue = generationJobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _cancellationTracker = cancellationTracker;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var generationJobRepository = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
        var recoverableJobs = await generationJobRepository.GetRecoverableLessonContentJobsAsync();

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

            _generationJobQueue.Enqueue(jobToResume.Id);
        }

        await generationJobRepository.SaveChangesAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _generationJobQueue.DequeueAsync(stoppingToken);
            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _cancellationTracker.RegisterJob(jobId, jobCts);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ILessonContentGenerationService>();
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
                _logger.LogError(exception, "Lesson content background job {JobId} failed unexpectedly.", jobId);
            }
            finally
            {
                _cancellationTracker.UnregisterJob(jobId);
            }
        }
    }
}
