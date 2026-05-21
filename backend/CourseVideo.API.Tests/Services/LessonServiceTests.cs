using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonServiceTests
{
    [Fact]
    public async Task UpdateAsync_UpdatesLessonMetadata_WhenLessonExists()
    {
        var repository = new Mock<ILessonRepository>();
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            Description = "Old description",
            ContentSeed = "Old seed"
        };

        repository.Setup(x => x.GetByIdAsync(lesson.Id)).ReturnsAsync(lesson);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new LessonService(repository.Object);

        var result = await service.UpdateAsync(lesson.Id, new UpdateLessonRequest
        {
            Title = "New title",
            Description = "New description",
            ContentSeed = "New seed"
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("New title");
        result.Description.Should().Be("New description");
        result.ContentSeed.Should().Be("New seed");
    }

    [Fact]
    public async Task UpdateGeneratedContentAsync_StoresTrimmedGeneratedFields_AndClearsPreviousError()
    {
        var repository = new Mock<ILessonRepository>();
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Lesson 1",
            ContentGenerationError = "schema invalid"
        };

        repository.Setup(x => x.GetByIdAsync(lesson.Id)).ReturnsAsync(lesson);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new LessonService(repository.Object);

        var result = await service.UpdateGeneratedContentAsync(lesson.Id, new UpdateLessonGeneratedContentRequest
        {
            TeachingScript = " Script moi ",
            SlideOutlineJson = " {\"slides\":[{\"title\":\"S1\"}]} ",
            VoiceoverPlanJson = " {\"tone\":\"clear\"} "
        });

        result.Should().NotBeNull();
        result!.TeachingScript.Should().Be("Script moi");
        result.ContentGenerationError.Should().BeNullOrEmpty();
        result.ContentGenerationStatus.Should().Be("ManuallyEdited");
    }
}
