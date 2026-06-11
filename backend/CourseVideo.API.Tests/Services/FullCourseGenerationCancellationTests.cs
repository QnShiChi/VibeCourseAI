using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class FullCourseGenerationCancellationTests
{
    [Fact]
    public async Task ProcessJobAsync_WhenVideoStepIsCancelled_DoesNotLeaveLessonStuckInGeneratingVideo()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var queue = new Mock<IFullCourseJobQueue>();
        var lessonContentService = new Mock<ILessonContentGenerationService>();
        var lessonAudioService = new Mock<ILessonAudioGenerationService>();
        var lessonVideoService = new Mock<ILessonVideoGenerationService>();
        var quizGenerationService = new Mock<IQuizGenerationService>();

        var course = CreateCourse();
        var lesson = course.Modules.Single().Lessons.Single();
        var job = new GenerationJob
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            SyllabusId = course.SyllabusId!.Value,
            JobType = "GenerateFullCourse",
            Status = "Pending"
        };

        generationJobRepository.Setup(repository => repository.GetByIdAsync(job.Id)).ReturnsAsync(job);
        generationJobRepository.Setup(repository => repository.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(repository => repository.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);
        lessonRepository.Setup(repository => repository.SaveChangesAsync()).Returns(Task.CompletedTask);

        lessonVideoService
            .Setup(service => service.GenerateVideoForLessonInternalAsync(lesson, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("cancelled"));

        var service = new FullCourseGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            queue.Object,
            lessonContentService.Object,
            lessonAudioService.Object,
            lessonVideoService.Object,
            quizGenerationService.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ProcessJobAsync(job.Id, new CancellationToken(canceled: true)));

        lesson.VideoGenerationStatus.Should().Be("NotGenerated");
        lesson.VideoGenerationError.Should().NotBeNullOrWhiteSpace();
        job.Status.Should().NotBe("Failed");
    }

    private static Course CreateCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Course Cancelled Video",
            SyllabusId = Guid.NewGuid()
        };

        var module = new Module
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Module 1",
            OrderIndex = 1,
            Course = course
        };

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            Title = "Lesson 1",
            OrderIndex = 1,
            ContentSeed = "Seed 1",
            ContentGenerationStatus = "Completed",
            AudioGenerationStatus = "Completed",
            VideoGenerationStatus = "NotGenerated",
            TeachingScript = "Script",
            SlideOutlineJson = "[{\"SlideNumber\":1,\"Title\":\"Slide 1\",\"BulletPoints\":[\"A\"],\"SpeakerNotes\":\"n\"}]",
            VoiceoverPlanJson = "{\"estimatedDurationMinutes\":1,\"tone\":\"clear\",\"pacing\":\"steady\",\"targetAudience\":\"student\",\"pronunciationNotes\":\"none\"}",
            AudioUrl = "/storage/audio/lesson-1.mp3",
            AudioSegmentsJson = "[{\"slideNumber\":1,\"title\":\"Slide 1\",\"durationSeconds\":5}]"
        };

        module.Lessons = [lesson];
        course.Modules = [module];
        return course;
    }
}
