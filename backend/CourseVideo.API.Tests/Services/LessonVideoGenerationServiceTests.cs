using CourseVideo.API.DTOs.VideoWorker;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Video;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonVideoGenerationServiceTests
{
    [Fact]
    public async Task GenerateVideoForLessonInternalAsync_UsesLocalRenderingPipelineAndUpdatesLesson()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var queue = new Mock<ILessonVideoJobQueue>();
        var timelineService = new Mock<ITimelineService>();
        var storageService = new Mock<IStorageService>();
        var renderService = new Mock<IRenderService>();
        var ffmpegService = new Mock<IFFmpegService>();

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Lesson 1",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Slide 1\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"n\"}]",
            AudioUrl = "/storage/audio/lesson-1.mp3",
            AudioSegmentsJson = "[{\"slideNumber\":1,\"title\":\"Slide 1\",\"durationSeconds\":5}]",
            AudioGenerationStatus = "Completed"
        };

        timelineService.Setup(x => x.ParseSlideOutlineJson(lesson.SlideOutlineJson!))
            .Returns([new SlideItem { SlideNumber = 1, Title = "Slide 1", BulletPoints = ["A"], SpeakerNotes = "n" }]);
        timelineService.Setup(x => x.ParseAudioSegmentsJson(lesson.AudioSegmentsJson!))
            .Returns([new AudioSegment { SlideNumber = 1, Title = "Slide 1", DurationSeconds = 5 }]);
        timelineService.Setup(x => x.BuildSlideTimeline(It.IsAny<List<AudioSegment>>()))
            .Returns([new SlideTimingResponse { SlideNumber = 1, StartSeconds = 0, DurationSeconds = 5, EndSeconds = 5 }]);

        storageService.Setup(x => x.BuildVideoFramesDir(lesson.Id.ToString())).Returns("/tmp/video-frames");
        storageService.Setup(x => x.ResolveStoragePathFromUrl(lesson.AudioUrl!)).Returns("/tmp/audio/lesson-1.mp3");
        storageService.Setup(x => x.BuildVideoOutputPath(lesson.Id.ToString())).Returns("/tmp/video/lesson-1.mp4");

        ffmpegService.Setup(x => x.AssembleVideoAsync(
                It.IsAny<List<string>>(),
                It.IsAny<List<double>>(),
                "/tmp/audio/lesson-1.mp3",
                "/tmp/video/lesson-1.mp4",
                CancellationToken.None))
            .ReturnsAsync(5);

        var service = new LessonVideoGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            queue.Object,
            timelineService.Object,
            storageService.Object,
            renderService.Object,
            ffmpegService.Object);

        await service.GenerateVideoForLessonInternalAsync(lesson, CancellationToken.None);

        lesson.VideoGenerationStatus.Should().Be("Completed");
        lesson.VideoUrl.Should().Be("/storage/video/lesson-1.mp4");
        renderService.Verify(x => x.RenderSlidePngAsync("/tmp/video-frames/slide-001.png", It.IsAny<SlideItem>(), CancellationToken.None), Times.Once);
        ffmpegService.Verify(x => x.AssembleVideoAsync(
            It.IsAny<List<string>>(),
            It.IsAny<List<double>>(),
            "/tmp/audio/lesson-1.mp3",
            "/tmp/video/lesson-1.mp4",
            CancellationToken.None), Times.Once);
    }
}
