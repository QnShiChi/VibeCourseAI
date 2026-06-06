using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using CourseVideo.API.DTOs.AudioWorker;
using CourseVideo.API.Services.Audio;
using CourseVideo.API.Services.Video;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class AudioPipelineServiceTests
{
    [Fact]
    public async Task GenerateLessonAudioAsync_DoesNotPauseBetweenSlides()
    {
        var edgeTtsService = new Mock<IEdgeTtsService>();
        var storageService = new Mock<IStorageService>();
        var storageDir = Path.Combine(Path.GetTempPath(), $"audio-pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageDir);
        storageService.Setup(x => x.GetStorageDirectory()).Returns(storageDir);

        edgeTtsService
            .Setup(x => x.SynthesizeToBytesAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });

        var service = new AudioPipelineService(
            edgeTtsService.Object,
            storageService.Object,
            NullLogger<AudioPipelineService>.Instance);

        var segments = new List<NarrationSegment>
        {
            new() { SlideNumber = 1, Title = "S1", NarrationText = "Xin chao 1" },
            new() { SlideNumber = 2, Title = "S2", NarrationText = "Xin chao 2" },
            new() { SlideNumber = 3, Title = "S3", NarrationText = "Xin chao 3" }
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await service.GenerateLessonAudioAsync(Guid.NewGuid(), segments, null, CancellationToken.None);
            stopwatch.Stop();

            result.Segments.Should().HaveCount(3);
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }
        finally
        {
            if (Directory.Exists(storageDir))
            {
                Directory.Delete(storageDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateLessonAudioAsync_ProcessesSynthRequestsSequentially()
    {
        var edgeTtsService = new Mock<IEdgeTtsService>();
        var storageService = new Mock<IStorageService>();
        var storageDir = Path.Combine(Path.GetTempPath(), $"audio-pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageDir);
        storageService.Setup(x => x.GetStorageDirectory()).Returns(storageDir);

        var releaseSynth = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeRequests = new StrongBox<int>(0);
        var maxConcurrentRequests = new StrongBox<int>(0);

        edgeTtsService
            .Setup(x => x.SynthesizeToBytesAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns((string _, string? _, CancellationToken cancellationToken) => WaitForReleaseAsync(
                releaseSynth,
                cancellationToken,
                activeRequests,
                maxConcurrentRequests));

        var service = new AudioPipelineService(
            edgeTtsService.Object,
            storageService.Object,
            NullLogger<AudioPipelineService>.Instance);

        var segments = new List<NarrationSegment>
        {
            new() { SlideNumber = 1, Title = "S1", NarrationText = "Xin chao 1" },
            new() { SlideNumber = 2, Title = "S2", NarrationText = "Xin chao 2" },
            new() { SlideNumber = 3, Title = "S3", NarrationText = "Xin chao 3" }
        };

        try
        {
            var generationTask = service.GenerateLessonAudioAsync(Guid.NewGuid(), segments, null, CancellationToken.None);
            await Task.Delay(250, CancellationToken.None);

            maxConcurrentRequests.Value.Should().Be(1);

            releaseSynth.TrySetResult();
            await generationTask;
        }
        finally
        {
            if (Directory.Exists(storageDir))
            {
                Directory.Delete(storageDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateLessonAudioAsync_ReportsSegmentProgress_AsSegmentsComplete()
    {
        var edgeTtsService = new Mock<IEdgeTtsService>();
        var storageService = new Mock<IStorageService>();
        var storageDir = Path.Combine(Path.GetTempPath(), $"audio-pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageDir);
        storageService.Setup(x => x.GetStorageDirectory()).Returns(storageDir);

        edgeTtsService
            .Setup(x => x.SynthesizeToBytesAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3, 4]);

        var service = new AudioPipelineService(
            edgeTtsService.Object,
            storageService.Object,
            NullLogger<AudioPipelineService>.Instance);

        var segments = new List<NarrationSegment>
        {
            new() { SlideNumber = 1, Title = "S1", NarrationText = "Xin chao 1" },
            new() { SlideNumber = 2, Title = "S2", NarrationText = "Xin chao 2" },
            new() { SlideNumber = 3, Title = "S3", NarrationText = "Xin chao 3" }
        };
        var progressEvents = new ConcurrentBag<(int completed, int total)>();

        try
        {
            await service.GenerateLessonAudioAsync(
                Guid.NewGuid(),
                segments,
                (completed, total) =>
                {
                    progressEvents.Add((completed, total));
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            progressEvents.Should().HaveCount(3);
            progressEvents.Should().Contain((1, 3));
            progressEvents.Should().Contain((2, 3));
            progressEvents.Should().Contain((3, 3));
        }
        finally
        {
            if (Directory.Exists(storageDir))
            {
                Directory.Delete(storageDir, recursive: true);
            }
        }
    }

    private static async Task<byte[]> WaitForReleaseAsync(
        TaskCompletionSource releaseSynth,
        CancellationToken cancellationToken,
        StrongBox<int> activeRequests,
        StrongBox<int> maxConcurrentRequests)
    {
        var active = Interlocked.Increment(ref activeRequests.Value);
        UpdateMaxConcurrent(active, maxConcurrentRequests);

        try
        {
            await releaseSynth.Task.WaitAsync(cancellationToken);
            return [1, 2, 3, 4];
        }
        finally
        {
            Interlocked.Decrement(ref activeRequests.Value);
        }
    }

    private static void UpdateMaxConcurrent(int active, StrongBox<int> maxConcurrentRequests)
    {
        int currentMax;
        do
        {
            currentMax = maxConcurrentRequests.Value;
            if (active <= currentMax)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref maxConcurrentRequests.Value, active, currentMax) != currentMax);
    }
}
