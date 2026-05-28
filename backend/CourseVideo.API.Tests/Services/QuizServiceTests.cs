using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class QuizServiceTests
{
    [Fact]
    public async Task GetLessonQuizAsync_ReturnsNull_WhenQuizDoesNotExist()
    {
        var repository = new Mock<IQuizRepository>();
        repository.Setup(x => x.GetLessonQuizAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quiz?)null);

        var service = new QuizService(repository.Object);

        var result = await service.GetLessonQuizAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLessonQuizAsync_ReturnsReadyQuizSummary_WhenQuizExists()
    {
        var lessonId = Guid.NewGuid();
        var repository = new Mock<IQuizRepository>();
        repository.Setup(x => x.GetLessonQuizAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Quiz
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                Type = "Lesson",
                Status = "Ready",
                Title = "Kiem tra nhanh",
                Questions =
                [
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "AI la gi?",
                        OrderIndex = 1,
                        Explanation = "Giai thich",
                        Options =
                        [
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "A", OrderIndex = 1, IsCorrect = true },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "B", OrderIndex = 2, IsCorrect = false },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "C", OrderIndex = 3, IsCorrect = false },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "D", OrderIndex = 4, IsCorrect = false }
                        ]
                    }
                ]
            });

        var service = new QuizService(repository.Object);

        var result = await service.GetLessonQuizAsync(lessonId, Guid.NewGuid(), false);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Ready");
        result.QuestionCount.Should().Be(1);
    }
}
