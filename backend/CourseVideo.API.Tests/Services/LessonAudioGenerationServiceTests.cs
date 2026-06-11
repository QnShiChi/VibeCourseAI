using CourseVideo.API.DTOs.AudioWorker;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Audio;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonAudioGenerationServiceTests
{
    [Fact]
    public async Task GenerateAudioForLessonInternalAsync_UsesLocalPipelineAndUpdatesLesson()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var queue = new Mock<ILessonAudioJobQueue>();
        var narrationService = new Mock<INarrationService>();
        var audioPipelineService = new Mock<IAudioPipelineService>();

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Lesson 1",
            TeachingScript = "Script",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Slide 1\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"n\"}]",
            VoiceoverPlanJson = "{\"estimatedDurationMinutes\":1,\"tone\":\"clear\",\"pacing\":\"steady\",\"targetAudience\":\"student\",\"pronunciationNotes\":\"none\"}",
            VideoGenerationStatus = "Completed",
            VideoUrl = "/storage/video/old.mp4"
        };

        narrationService
            .Setup(x => x.BuildNarrationSegments(lesson.TeachingScript!, lesson.SlideOutlineJson!, lesson.VoiceoverPlanJson!))
            .Returns(
            [
                new NarrationSegment
                {
                    SlideNumber = 1,
                    Title = "Slide 1",
                    NarrationText = "Xin chao"
                }
            ]);

        audioPipelineService
            .Setup(x => x.GenerateLessonAudioAsync(lesson.Id, It.IsAny<List<NarrationSegment>>(), CancellationToken.None))
            .ReturnsAsync(new AudioWorkerLessonResponse
            {
                AudioUrl = "/storage/audio/lesson-1.mp3",
                DurationSeconds = 12.5,
                Segments =
                [
                    new AudioWorkerSegmentResponse
                    {
                        SlideNumber = 1,
                        Title = "Slide 1",
                        NarrationText = "Xin chao",
                        AudioUrl = "/storage/audio/lesson-1-slide-1.mp3",
                        DurationSeconds = 12.5
                    }
                ]
            });

        var service = new LessonAudioGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            queue.Object,
            narrationService.Object,
            audioPipelineService.Object);

        await service.GenerateAudioForLessonInternalAsync(lesson, CancellationToken.None);

        lesson.AudioGenerationStatus.Should().Be("Completed");
        lesson.AudioUrl.Should().Be("/storage/audio/lesson-1.mp3");
        lesson.VideoGenerationStatus.Should().Be("NotGenerated");
        lesson.VideoUrl.Should().BeNull();
        narrationService.Verify(x => x.BuildNarrationSegments(lesson.TeachingScript!, lesson.SlideOutlineJson!, lesson.VoiceoverPlanJson!), Times.Once);
        audioPipelineService.Verify(x => x.GenerateLessonAudioAsync(lesson.Id, It.IsAny<List<NarrationSegment>>(), CancellationToken.None), Times.Once);
    }
}
