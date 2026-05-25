using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class FullCourseGenerationWorker : BackgroundService
{
    private readonly IFullCourseJobQueue _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FullCourseGenerationWorker> _logger;

    public FullCourseGenerationWorker(
        IFullCourseJobQueue jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FullCourseGenerationWorker> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
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

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFullCourseGenerationService>();
                await service.ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Full course generation background job {JobId} failed unexpectedly.", jobId);
            }
        }
    }
}
