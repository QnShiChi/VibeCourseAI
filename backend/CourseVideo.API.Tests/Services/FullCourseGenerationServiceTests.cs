using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class FullCourseGenerationServiceTests
{
    [Fact]
    public async Task ProcessJobAsync_TracksProgressByStep_AndContinuesAfterLessonFailure()
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
        var module = course.Modules.Single();
        var firstLesson = module.Lessons.OrderBy(lesson => lesson.OrderIndex).First();
        var secondLesson = module.Lessons.OrderBy(lesson => lesson.OrderIndex).Skip(1).First();
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

        lessonContentService
            .Setup(service => service.GenerateContentForLessonInternalAsync(course, module, firstLesson, CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback(() => firstLesson.ContentGenerationStatus = "Completed");
        lessonContentService
            .Setup(service => service.GenerateContentForLessonInternalAsync(course, module, secondLesson, CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback(() => secondLesson.ContentGenerationStatus = "Completed");

        lessonAudioService
            .Setup(service => service.GenerateAudioForLessonInternalAsync(firstLesson, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("audio fail"));
        lessonAudioService
            .Setup(service => service.GenerateAudioForLessonInternalAsync(secondLesson, CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback(() => secondLesson.AudioGenerationStatus = "Completed");

        lessonVideoService
            .Setup(service => service.GenerateVideoForLessonInternalAsync(secondLesson, CancellationToken.None))
            .Returns(Task.CompletedTask)
            .Callback(() => secondLesson.VideoGenerationStatus = "Completed");

        var service = new FullCourseGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            queue.Object,
            lessonContentService.Object,
            lessonAudioService.Object,
            lessonVideoService.Object,
            quizGenerationService.Object);

        await service.ProcessJobAsync(job.Id, CancellationToken.None);

        job.TotalItems.Should().Be(5);
        job.ProcessedItems.Should().Be(3);
        job.FailedItems.Should().Be(1);
        job.Status.Should().Be("CompletedWithWarnings");
        job.ErrorMessage.Should().Be("Có 1 lesson gặp lỗi trong quá trình generate.");
        firstLesson.ContentGenerationStatus.Should().Be("Completed");
        firstLesson.AudioGenerationStatus.Should().Be("Failed");
        secondLesson.ContentGenerationStatus.Should().Be("Completed");
        secondLesson.AudioGenerationStatus.Should().Be("Completed");
        secondLesson.VideoGenerationStatus.Should().Be("Completed");
        quizGenerationService.Verify(service => service.GenerateLessonQuizAsync(course.Id, firstLesson.Id, CancellationToken.None), Times.Once);
        quizGenerationService.Verify(service => service.GenerateFinalQuizAsync(course.Id, CancellationToken.None), Times.Once);
    }

    private static Course CreateCourse()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Course A-Z",
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

        var firstLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            Title = "Lesson 1",
            OrderIndex = 1,
            ContentSeed = "Seed 1",
            ContentGenerationStatus = "NotGenerated",
            AudioGenerationStatus = "NotGenerated",
            VideoGenerationStatus = "NotGenerated"
        };

        var secondLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            Title = "Lesson 2",
            OrderIndex = 2,
            ContentSeed = "Seed 2",
            ContentGenerationStatus = "Completed",
            AudioGenerationStatus = "NotGenerated",
            VideoGenerationStatus = "NotGenerated"
        };

        module.Lessons = [firstLesson, secondLesson];
        course.Modules = [module];
        return course;
    }
}
