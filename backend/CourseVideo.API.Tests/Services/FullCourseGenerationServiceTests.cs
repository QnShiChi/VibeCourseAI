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
            .Setup(service => service.GenerateAudioForLessonInternalAsync(firstLesson, CancellationToken.None, It.IsAny<Func<int, int, Task>?>()))
            .ThrowsAsync(new InvalidOperationException("audio fail"));
        lessonAudioService
            .Setup(service => service.GenerateAudioForLessonInternalAsync(secondLesson, CancellationToken.None, It.IsAny<Func<int, int, Task>?>()))
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

    [Fact]
    public async Task ProcessJobAsync_StopsEarlyAfterRepeatedSystemicAudioFailures()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var queue = new Mock<IFullCourseJobQueue>();
        var lessonContentService = new Mock<ILessonContentGenerationService>();
        var lessonAudioService = new Mock<ILessonAudioGenerationService>();
        var lessonVideoService = new Mock<ILessonVideoGenerationService>();
        var quizGenerationService = new Mock<IQuizGenerationService>();

        var course = CreateCourseWithLessonCount(4);
        var module = course.Modules.Single();
        var lessons = module.Lessons.OrderBy(lesson => lesson.OrderIndex).ToList();
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

        foreach (var lesson in lessons)
        {
            lessonContentService
                .Setup(service => service.GenerateContentForLessonInternalAsync(course, module, lesson, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Callback(() => lesson.ContentGenerationStatus = "Completed");
        }

        lessonAudioService
            .Setup(service => service.GenerateAudioForLessonInternalAsync(It.IsAny<Lesson>(), CancellationToken.None, It.IsAny<Func<int, int, Task>?>()))
            .ThrowsAsync(new InvalidOperationException("edge-tts failed with exit code 1: edge_tts.exceptions.NoAudioReceived"));

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

        job.Status.Should().Be("Failed");
        job.FailedItems.Should().Be(3);
        job.ErrorMessage.Should().Contain("edge-tts");
        lessonAudioService.Verify(
            svc => svc.GenerateAudioForLessonInternalAsync(It.IsAny<Lesson>(), CancellationToken.None, It.IsAny<Func<int, int, Task>?>()),
            Times.Exactly(3));
        lessons[3].ContentGenerationStatus.Should().Be("NotGenerated");
    }

    private static Course CreateCourse()
    {
        return CreateCourseWithLessonCount(2);
    }

    private static Course CreateCourseWithLessonCount(int lessonCount)
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

        var lessons = new List<Lesson>();
        for (var index = 1; index <= lessonCount; index++)
        {
            lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Module = module,
                Title = $"Lesson {index}",
                OrderIndex = index,
                ContentSeed = $"Seed {index}",
                ContentGenerationStatus = index == 2 && lessonCount == 2 ? "Completed" : "NotGenerated",
                AudioGenerationStatus = "NotGenerated",
                VideoGenerationStatus = "NotGenerated"
            });
        }

        module.Lessons = lessons;
        course.Modules = [module];
        return course;
    }
}
