using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class CourseServiceTests
{
    [Fact]
    public async Task PublishAsync_SetsCoursePublished_WhenCourseExists()
    {
        var repository = new Mock<ICourseRepository>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            IsPublished = false
        };

        repository.Setup(x => x.GetByIdAsync(course.Id)).ReturnsAsync(course);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = CreateCourseService(repository);

        var result = await service.PublishAsync(course.Id);

        result.Should().NotBeNull();
        result!.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UnpublishAsync_SetsCourseDraft_WhenCourseExists()
    {
        var repository = new Mock<ICourseRepository>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "OOP",
            IsPublished = true
        };

        repository.Setup(x => x.GetByIdAsync(course.Id)).ReturnsAsync(course);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = CreateCourseService(repository);

        var result = await service.UnpublishAsync(course.Id);

        result.Should().NotBeNull();
        result!.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublishedCoursesAsync_ReturnsOnlyPublishedCourses()
    {
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetPublishedAsync()).ReturnsAsync(new List<Course>
        {
            new() { Id = Guid.NewGuid(), Title = "Published 1", Description = "Desc", IsPublished = true },
            new() { Id = Guid.NewGuid(), Title = "Published 2", Description = "Desc", IsPublished = true }
        });

        var service = CreateCourseService(repository);

        var result = await service.GetPublishedCoursesAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(course => course.IsPublished);
    }

    [Fact]
    public async Task GetPublishedCoursesAsync_MapsCategoryAndThumbnailUrl()
    {
        var repository = new Mock<ICourseRepository>();
        var category = new Category { Id = Guid.NewGuid(), Name = "AiAndData", Status = CategoryStatus.Visible };
        repository.Setup(x => x.GetPublishedAsync()).ReturnsAsync(new List<Course>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "AI Prompting",
                Description = "Desc",
                IsPublished = true,
                CategoryId = category.Id,
                Category = category,
                ThumbnailUrl = "/storage/course-thumbnails/ai.png"
            }
        });

        var service = CreateCourseService(repository);

        var result = await service.GetPublishedCoursesAsync();

        result.Should().ContainSingle();
        result[0].Category.Should().Be("AiAndData");
        result[0].ThumbnailUrl.Should().Be("/storage/course-thumbnails/ai.png");
    }

    [Fact]
    public async Task GetAdminCoursesAsync_ReturnsDraftAndPublishedCourses()
    {
        var repository = new Mock<ICourseRepository>();
        repository.Setup(x => x.GetAdminCoursesAsync()).ReturnsAsync(new List<Course>
        {
            new() { Id = Guid.NewGuid(), Title = "Draft", Description = "Desc", IsPublished = false, Modules = [new Module { Lessons = [new Lesson()] }] },
            new() { Id = Guid.NewGuid(), Title = "Published", Description = "Desc", IsPublished = true, Modules = [new Module { Lessons = [new Lesson(), new Lesson()] }] }
        });

        var service = CreateCourseService(repository);

        var result = await service.GetAdminCoursesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(course => !course.IsPublished);
        result.Should().Contain(course => course.IsPublished);
    }

    [Fact]
    public async Task GetLearnPayloadAsync_ReturnsNull_WhenDraftPreviewNotAllowed()
    {
        var repository = new Mock<ICourseRepository>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Draft Course",
            Description = "Desc",
            IsPublished = false,
            Modules =
            [
                new Module
                {
                    Id = Guid.NewGuid(),
                    Title = "Module 1",
                    Description = "M1",
                    OrderIndex = 1,
                    Lessons = [new Lesson { Id = Guid.NewGuid(), Title = "Lesson 1", Description = "L1", ContentSeed = "Seed", OrderIndex = 1 }]
                }
            ]
        };

        repository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);

        var service = CreateCourseService(repository);

        var result = await service.GetLearnPayloadAsync(course.Id, currentUserId: null, canPreviewDraft: false);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLearnPayloadAsync_ReturnsLearnPayloadForAdminDraftPreview()
    {
        var repository = new Mock<ICourseRepository>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Draft Course",
            Description = "Desc",
            IsPublished = false,
            Modules =
            [
                new Module
                {
                    Id = Guid.NewGuid(),
                    Title = "Module 1",
                    Description = "M1",
                    OrderIndex = 1,
                    Lessons =
                    [
                        new Lesson { Id = Guid.NewGuid(), Title = "Lesson 1", Description = "L1", ContentSeed = "Seed 1", OrderIndex = 1 },
                        new Lesson { Id = Guid.NewGuid(), Title = "Lesson 2", Description = "L2", ContentSeed = "Seed 2", OrderIndex = 2 }
                    ]
                }
            ]
        };

        repository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);

        var service = CreateCourseService(repository);

        var result = await service.GetLearnPayloadAsync(course.Id, currentUserId: null, canPreviewDraft: true);

        result.Should().NotBeNull();
        result!.SelectedLessonId.Should().Be(course.Modules.First().Lessons.First().Id);
        result.Modules.Should().ContainSingle();
        result.Modules[0].Lessons.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStructureAsync_MapsCategoryAndThumbnailUrl()
    {
        var repository = new Mock<ICourseRepository>();
        var courseId = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "UiUxDesign", Status = CategoryStatus.Visible };
        repository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId,
            Title = "UI Systems",
            Description = "Desc",
            CategoryId = category.Id,
            Category = category,
            ThumbnailUrl = "/storage/course-thumbnails/ui.png"
        });

        var service = CreateCourseService(repository);

        var result = await service.GetStructureAsync(courseId);

        result.Should().NotBeNull();
        result!.Category.Should().Be("UiUxDesign");
        result.ThumbnailUrl.Should().Be("/storage/course-thumbnails/ui.png");
    }

    [Fact]
    public async Task UpdateCategoryAsync_PersistsParsedCategory()
    {
        var repository = new Mock<ICourseRepository>();
        var categoryRepository = new Mock<ICategoryRepository>();
        var currentCategory = new Category { Id = Guid.NewGuid(), Name = "UiUxDesign", Status = CategoryStatus.Visible };
        var updatedCategory = new Category { Id = Guid.NewGuid(), Name = "Development", Status = CategoryStatus.Visible };
        var course = new Course { Id = Guid.NewGuid(), CategoryId = currentCategory.Id, Category = currentCategory };
        repository.Setup(x => x.GetByIdAsync(course.Id)).ReturnsAsync(course);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        repository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);
        categoryRepository.Setup(x => x.GetByIdAsync(updatedCategory.Id)).ReturnsAsync(updatedCategory);

        var service = CreateCourseService(repository, categoryRepository: categoryRepository);

        var result = await service.UpdateCategoryAsync(course.Id, updatedCategory.Id);

        result.Should().NotBeNull();
        course.CategoryId.Should().Be(updatedCategory.Id);
        course.Category.Should().BeSameAs(updatedCategory);
        result!.Category.Should().Be("Development");
    }

    [Fact]
    public async Task GenerateLessonQuizAsync_ForwardsRequestToQuizGenerationService()
    {
        var repository = new Mock<ICourseRepository>();
        var quizGenerationService = new Mock<IQuizGenerationService>();
        quizGenerationService.Setup(x => x.GenerateLessonQuizAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateCourseService(repository, quizGenerationService);
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        await service.GenerateLessonQuizAsync(courseId, lessonId);

        quizGenerationService.Verify(x => x.GenerateLessonQuizAsync(courseId, lessonId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLearnPayloadAsync_MapsQuizFlags_ForLessonAndFinalQuiz()
    {
        var repository = new Mock<ICourseRepository>();
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var lessonQuizId = Guid.NewGuid();
        var finalQuizId = Guid.NewGuid();

        repository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId,
            Title = "AI",
            Description = "Desc",
            IsPublished = true,
            Modules =
            [
                new Module
                {
                    Id = Guid.NewGuid(),
                    Title = "Module",
                    Description = "Desc",
                    OrderIndex = 1,
                    Lessons =
                    [
                        new Lesson
                        {
                            Id = lessonId,
                            Title = "Lesson",
                            Description = "Desc",
                            ContentSeed = "Noi dung",
                            OrderIndex = 1
                        }
                    ]
                }
            ],
            Quizzes =
            [
                new Quiz { Id = lessonQuizId, LessonId = lessonId, CourseId = courseId, Type = "Lesson", Status = "Ready", QuestionCount = 5 },
                new Quiz { Id = finalQuizId, CourseId = courseId, Type = "Final", Status = "Ready", QuestionCount = 10 }
            ]
        });

        var service = CreateCourseService(repository);

        var result = await service.GetLearnPayloadAsync(courseId, currentUserId: null, canPreviewDraft: true);

        result.Should().NotBeNull();
        result!.HasFinalQuiz.Should().BeTrue();
        result.FinalQuizId.Should().Be(finalQuizId);
        result.Modules[0].Lessons[0].QuizId.Should().Be(lessonQuizId);
        result.Modules[0].Lessons[0].QuizStatus.Should().Be("Ready");
    }

    private static CourseService CreateCourseService(
        Mock<ICourseRepository> repository,
        Mock<IQuizGenerationService>? quizGenerationService = null,
        Mock<ICategoryRepository>? categoryRepository = null,
        Mock<IPaymentService>? paymentService = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns("/tmp/vibecourseai-course-service-tests");

        return new CourseService(
            repository.Object,
            categoryRepository?.Object ?? Mock.Of<ICategoryRepository>(),
            Mock.Of<ILessonContentGenerationService>(),
            Mock.Of<ILessonAudioGenerationService>(),
            Mock.Of<ILessonVideoGenerationService>(),
            Mock.Of<IFullCourseGenerationService>(),
            quizGenerationService?.Object ?? Mock.Of<IQuizGenerationService>(),
            environment.Object,
            paymentService?.Object ?? Mock.Of<IPaymentService>());
    }
}
