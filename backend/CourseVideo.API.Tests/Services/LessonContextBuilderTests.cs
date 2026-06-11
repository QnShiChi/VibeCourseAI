using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_UsesLessonAndCourseMetadata_WhenLessonExists()
    {
        var lessonId = Guid.NewGuid();
        var repository = new Mock<ILessonRepository>();
        repository.Setup(x => x.GetByIdWithModuleAndCourseAsync(lessonId))
            .ReturnsAsync(new Lesson
            {
                Id = lessonId,
                Title = "Dinh nghia AI",
                Description = "Mo ta",
                TeachingScript = "Script",
                SlideOutlineJson = "[{}]",
                VoiceoverPlanJson = "{}",
                TranscriptText = "Transcript",
                Module = new Module
                {
                    Title = "Module 1",
                    Course = new Course { Title = "Khoa hoc AI", Description = "Tong quan" }
                }
            });

        var builder = new LessonContextBuilder(repository.Object);
        var context = await builder.BuildAsync(lessonId, 42.5, CancellationToken.None);

        context.LessonTitle.Should().Be("Dinh nghia AI");
        context.CourseTitle.Should().Be("Khoa hoc AI");
        context.PlaybackTimeSeconds.Should().Be(42.5);
    }
}
