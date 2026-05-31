using CourseVideo.API.Models;
using CourseVideo.API.Models.OpenRouter;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonContentGenerationServiceTests
{
    [Fact]
    public async Task GenerateCourseContentAsync_QueuesBackgroundJob_InsteadOfGeneratingInline()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var generator = new Mock<IOpenRouterLessonContentService>();
        var queue = new Mock<IGenerationJobQueue>();
        var quizGenerationService = new Mock<IQuizGenerationService>();
        var courseId = Guid.NewGuid();
        var course = CreateCourseWithLessons(courseId, 2, lessonStatus: "NotGenerated");

        courseRepository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(course);
        generationJobRepository.Setup(x => x.HasRunningLessonContentJobForCourseAsync(courseId)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>())).Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new LessonContentGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            generator.Object,
            queue.Object,
            quizGenerationService.Object);

        var result = await service.GenerateCourseContentAsync(courseId, Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be("Pending");
        result.TotalLessons.Should().Be(2);
        queue.Verify(x => x.Enqueue(result.JobId), Times.Once);
        generator.Verify(x => x.GenerateAsync(It.IsAny<Course>(), It.IsAny<Module>(), It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegenerateLessonContentAsync_QueuesSingleFailedLesson()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var generator = new Mock<IOpenRouterLessonContentService>();
        var queue = new Mock<IGenerationJobQueue>();
        var quizGenerationService = new Mock<IQuizGenerationService>();
        var courseId = Guid.NewGuid();
        var course = CreateCourseWithLessons(courseId, 1, lessonStatus: "Failed");
        var module = course.Modules.First();
        var lesson = module.Lessons.First();
        lesson.Module = module;
        module.Course = course;

        lessonRepository.Setup(x => x.GetByIdWithModuleAndCourseAsync(lesson.Id)).ReturnsAsync(lesson);
        generationJobRepository.Setup(x => x.HasRunningLessonContentJobForCourseAsync(courseId)).ReturnsAsync(false);
        generationJobRepository.Setup(x => x.AddAsync(It.IsAny<GenerationJob>())).Returns(Task.CompletedTask);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new LessonContentGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            generator.Object,
            queue.Object,
            quizGenerationService.Object);

        var result = await service.RegenerateLessonContentAsync(courseId, lesson.Id, Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be("Pending");
        result.TotalLessons.Should().Be(1);
        queue.Verify(x => x.Enqueue(result.JobId), Times.Once);
    }

    [Fact]
    public async Task ProcessJobAsync_CompletesSingleLessonRetry_WhenGenerationSucceeds()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var generator = new Mock<IOpenRouterLessonContentService>();
        var queue = new Mock<IGenerationJobQueue>();
        var quizGenerationService = new Mock<IQuizGenerationService>();
        var courseId = Guid.NewGuid();
        var course = CreateCourseWithLessons(courseId, 1, lessonStatus: "Failed");
        var module = course.Modules.First();
        var lesson = module.Lessons.First();
        module.Course = course;
        lesson.Module = module;
        var job = new GenerationJob
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            LessonId = lesson.Id,
            SyllabusId = course.SyllabusId!.Value,
            JobType = "RegenerateLessonContent",
            Status = "Pending"
        };

        generationJobRepository.Setup(x => x.GetByIdAsync(job.Id)).ReturnsAsync(job);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        lessonRepository.Setup(x => x.GetByIdWithModuleAndCourseAsync(lesson.Id)).ReturnsAsync(lesson);
        lessonRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        generator.Setup(x => x.GenerateAsync(course, module, lesson, CancellationToken.None))
            .ReturnsAsync(CreateContentResult(lesson));

        var service = new LessonContentGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            generator.Object,
            queue.Object,
            quizGenerationService.Object);

        await service.ProcessJobAsync(job.Id, CancellationToken.None);

        lesson.ContentGenerationStatus.Should().Be("Completed");
        job.Status.Should().Be("Completed");
        job.ProcessedItems.Should().Be(1);
        job.FailedItems.Should().Be(0);
        quizGenerationService.Verify(x => x.GenerateLessonQuizAsync(courseId, lesson.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ProcessJobAsync_GeneratesLessonQuiz_ForEachSuccessfullyGeneratedLesson()
    {
        var courseRepository = new Mock<ICourseRepository>();
        var lessonRepository = new Mock<ILessonRepository>();
        var generationJobRepository = new Mock<IGenerationJobRepository>();
        var generator = new Mock<IOpenRouterLessonContentService>();
        var queue = new Mock<IGenerationJobQueue>();
        var quizGenerationService = new Mock<IQuizGenerationService>();
        var courseId = Guid.NewGuid();
        var course = CreateCourseWithLessons(courseId, 2, lessonStatus: "NotGenerated");
        var job = new GenerationJob
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            SyllabusId = course.SyllabusId!.Value,
            JobType = "GenerateLessonContent",
            Status = "Pending"
        };

        generationJobRepository.Setup(x => x.GetByIdAsync(job.Id)).ReturnsAsync(job);
        generationJobRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        courseRepository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(course);
        lessonRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        foreach (var lesson in course.Modules.SelectMany(module => module.Lessons))
        {
            generator.Setup(x => x.GenerateAsync(course, lesson.Module!, lesson, CancellationToken.None))
                .ReturnsAsync(CreateContentResult(lesson));
        }

        var service = new LessonContentGenerationService(
            courseRepository.Object,
            lessonRepository.Object,
            generationJobRepository.Object,
            generator.Object,
            queue.Object,
            quizGenerationService.Object);

        await service.ProcessJobAsync(job.Id, CancellationToken.None);

        foreach (var lesson in course.Modules.SelectMany(module => module.Lessons))
        {
            quizGenerationService.Verify(x => x.GenerateLessonQuizAsync(courseId, lesson.Id, CancellationToken.None), Times.Once);
        }
    }

    private static Course CreateCourseWithLessons(Guid courseId, int lessonCount, string lessonStatus)
    {
        var course = new Course
        {
            Id = courseId,
            Title = "Course",
            SyllabusId = Guid.NewGuid()
        };

        var module = new Module
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = "Module 1",
            OrderIndex = 1,
            Course = course,
            Lessons = Enumerable.Range(1, lessonCount).Select(index => new Lesson
            {
                Id = Guid.NewGuid(),
                Title = $"Bai {index}",
                Description = $"Mo ta {index}",
                ContentSeed = $"Seed {index}",
                OrderIndex = index,
                ContentGenerationStatus = lessonStatus
            }).ToList()
        };

        foreach (var lesson in module.Lessons)
        {
            lesson.Module = module;
        }

        course.Modules = [module];
        return course;
    }

    private static OpenRouterLessonContentResult CreateContentResult(Lesson lesson)
    {
        return new OpenRouterLessonContentResult
        {
            LessonId = lesson.Id,
            LessonTitle = lesson.Title,
            TeachingScript = "Script",
            SlideOutline =
            [
                new OpenRouterSlideOutlineResult
                {
                    SlideNumber = 1,
                    Title = "Slide 1",
                    BulletPoints = ["A"],
                    SpeakerNotes = "Notes"
                }
            ],
            VoiceoverPlan = new OpenRouterVoiceoverPlanResult
            {
                EstimatedDurationMinutes = 5,
                Tone = "clear",
                Pacing = "steady",
                TargetAudience = "student",
                PronunciationNotes = "none"
            }
        };
    }
}
