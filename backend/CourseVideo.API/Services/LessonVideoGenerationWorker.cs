using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonVideoGenerationWorker : BackgroundService
{
    private readonly ILessonVideoJobQueue _lessonVideoJobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IJobCancellationTracker _cancellationTracker;
    private readonly ILogger<LessonVideoGenerationWorker> _logger;

    public LessonVideoGenerationWorker(
        ILessonVideoJobQueue lessonVideoJobQueue,
        IServiceScopeFactory serviceScopeFactory,
        IJobCancellationTracker cancellationTracker,
        ILogger<LessonVideoGenerationWorker> logger)
    {
        _lessonVideoJobQueue = lessonVideoJobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _cancellationTracker = cancellationTracker;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var generationJobRepository = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
        var recoverableJobs = await generationJobRepository.GetRecoverableLessonVideoJobsAsync();

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
                supersededJob.ErrorMessage = "Job video cũ đã bị dừng khi hệ thống khởi động lại để tránh chạy trùng.";
                supersededJob.CompletedAt = DateTime.UtcNow;
                supersededJob.UpdatedAt = DateTime.UtcNow;
            }

            _lessonVideoJobQueue.Enqueue(jobToResume.Id);
        }

        await generationJobRepository.SaveChangesAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _lessonVideoJobQueue.DequeueAsync(stoppingToken);
            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _cancellationTracker.RegisterJob(jobId, jobCts);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ILessonVideoGenerationService>();
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
                _logger.LogError(exception, "Lesson video background job {JobId} failed unexpectedly.", jobId);
            }
            finally
            {
                _cancellationTracker.UnregisterJob(jobId);
            }
        }
    }
}
