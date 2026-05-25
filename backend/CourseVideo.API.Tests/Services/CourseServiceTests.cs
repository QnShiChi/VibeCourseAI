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
        repository.Setup(x => x.GetPublishedAsync()).ReturnsAsync(new List<Course>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "AI Prompting",
                Description = "Desc",
                IsPublished = true,
                Category = CourseCategory.AiAndData,
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

        var result = await service.GetLearnPayloadAsync(course.Id, canPreviewDraft: false);

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

        var result = await service.GetLearnPayloadAsync(course.Id, canPreviewDraft: true);

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
        repository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId,
            Title = "UI Systems",
            Description = "Desc",
            Category = CourseCategory.UiUxDesign,
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
        var course = new Course { Id = Guid.NewGuid(), Category = CourseCategory.UiUxDesign };
        repository.Setup(x => x.GetByIdAsync(course.Id)).ReturnsAsync(course);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        repository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);

        var service = CreateCourseService(repository);

        var result = await service.UpdateCategoryAsync(course.Id, "Development");

        result.Should().NotBeNull();
        course.Category.Should().Be(CourseCategory.Development);
        result!.Category.Should().Be("Development");
    }

    private static CourseService CreateCourseService(Mock<ICourseRepository> repository)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns("/tmp/vibecourseai-course-service-tests");

        return new CourseService(
            repository.Object,
            Mock.Of<ILessonContentGenerationService>(),
            Mock.Of<ILessonAudioGenerationService>(),
            Mock.Of<ILessonVideoGenerationService>(),
            Mock.Of<IFullCourseGenerationService>(),
            environment.Object);
    }
}
