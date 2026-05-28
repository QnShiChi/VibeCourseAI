using CourseVideo.API.Models;
using CourseVideo.API.DTOs.Quizzes;
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

    [Fact]
    public async Task SubmitAttemptAsync_ComputesScore_AndReturnsCorrectAnswers()
    {
        var quizId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var correctOptionId = Guid.NewGuid();
        var wrongOptionId = Guid.NewGuid();
        var repository = new Mock<IQuizRepository>();
        repository.Setup(x => x.GetByIdAsync(quizId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Quiz
            {
                Id = quizId,
                Status = "Ready",
                Questions =
                [
                    new QuizQuestion
                    {
                        Id = questionId,
                        QuestionText = "AI giup gi?",
                        Explanation = "AI ho tro giai quyet bai toan tri tue.",
                        Options =
                        [
                            new QuizOption { Id = correctOptionId, OptionText = "Ho tro bai toan tri tue", IsCorrect = true, OrderIndex = 1 },
                            new QuizOption { Id = wrongOptionId, OptionText = "Chi de nghe nhac", IsCorrect = false, OrderIndex = 2 },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "Chi de luu file", IsCorrect = false, OrderIndex = 3 },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "Chi de in giay", IsCorrect = false, OrderIndex = 4 }
                        ]
                    }
                ],
                Attempts =
                [
                    new QuizAttempt
                    {
                        Id = attemptId,
                        QuizId = quizId,
                        UserId = userId,
                        StartedAt = DateTime.UtcNow
                    }
                ]
            });
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = new QuizService(repository.Object);

        var submitted = await service.SubmitAttemptAsync(
            quizId,
            attemptId,
            userId,
            new SubmitQuizAttemptRequest
            {
                Answers =
                [
                    new SubmitQuizAttemptAnswerRequest
                    {
                        QuestionId = questionId,
                        SelectedOptionId = correctOptionId
                    }
                ]
            });

        submitted.Score.Should().Be(100);
        submitted.CorrectCount.Should().Be(1);
        submitted.Answers.Should().ContainSingle(x => x.IsCorrect);
    }
}
