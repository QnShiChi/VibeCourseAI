using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class FullCourseGenerationWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_MarksJobFailed_WhenBackgroundProcessingThrowsUnexpectedly()
    {
        var jobId = Guid.NewGuid();
        var job = new GenerationJob
        {
            Id = jobId,
            Status = "GeneratingFullCourse"
        };

        var queue = new FullCourseJobQueue();
        var cancellationTracker = new JobCancellationTracker();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var generationService = new Mock<IFullCourseGenerationService>();
        var failedJobPersisted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        generationJobRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync([]);
        generationJobRepository.Setup(repository => repository.GetByIdAsync(jobId)).ReturnsAsync(job);
        generationJobRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                if (job.Status == "Failed")
                {
                    failedJobPersisted.TrySetResult();
                }
            });

        generationService.Setup(service => service.ProcessJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate quiz"));

        var services = new ServiceCollection();
        services.AddScoped(_ => generationJobRepository.Object);
        services.AddScoped(_ => generationService.Object);

        await using var provider = services.BuildServiceProvider();

        var worker = new FullCourseGenerationWorker(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            cancellationTracker,
            NullLogger<FullCourseGenerationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        queue.Enqueue(jobId);

        await failedJobPersisted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        job.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Be("duplicate quiz");
        job.ProgressMessage.Should().Be("Job generate full course kết thúc với lỗi hệ thống.");
        job.CompletedAt.Should().NotBeNull();
        generationJobRepository.Verify(repository => repository.SaveChangesAsync(), Times.AtLeastOnce);
    }
}
