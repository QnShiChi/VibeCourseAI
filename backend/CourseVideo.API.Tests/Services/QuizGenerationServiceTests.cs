using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class QuizGenerationServiceTests
{
    [Fact]
    public async Task GenerateLessonQuizAsync_CreatesReadyQuiz_WhenOpenRouterReturnsValidPayload()
    {
        var course = new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" };
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", Description = "Desc", Course = course, CourseId = course.Id };
        var lesson = new Lesson { Id = Guid.NewGuid(), Title = "Lesson", Description = "Desc", ContentSeed = "Noi dung ve tri tue nhan tao", Module = module, ModuleId = module.Id };
        module.Lessons = [lesson];
        course.Modules = [module];

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);

        var quizRepository = new Mock<IQuizRepository>();
        quizRepository.Setup(x => x.GetLessonQuizAsync(lesson.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Quiz?)null);

        var openRouter = new Mock<IOpenRouterQuizGenerationService>();
        openRouter.Setup(x => x.GenerateLessonQuizAsync(course, module, lesson, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenRouterQuizGenerationResult
            {
                Title = "Quiz bai hoc",
                Questions =
                [
                    new OpenRouterQuizQuestionResult
                    {
                        QuestionText = "AI mo phong dieu gi?",
                        Explanation = "AI mo phong tri tue con nguoi.",
                        Options =
                        [
                            new OpenRouterQuizOptionResult { OptionText = "Tri tue con nguoi", IsCorrect = true },
                            new OpenRouterQuizOptionResult { OptionText = "May in", IsCorrect = false },
                            new OpenRouterQuizOptionResult { OptionText = "Loa", IsCorrect = false },
                            new OpenRouterQuizOptionResult { OptionText = "Ban phim", IsCorrect = false }
                        ]
                    }
                ]
            });

        Quiz? savedQuiz = null;
        quizRepository.Setup(x => x.AddAsync(It.IsAny<Quiz>(), It.IsAny<CancellationToken>()))
            .Callback<Quiz, CancellationToken>((quiz, _) => savedQuiz = quiz)
            .Returns(Task.CompletedTask);
        quizRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = new QuizGenerationService(courseRepository.Object, quizRepository.Object, openRouter.Object);

        await service.GenerateLessonQuizAsync(course.Id, lesson.Id);

        savedQuiz.Should().NotBeNull();
        savedQuiz!.Status.Should().Be("Ready");
        savedQuiz.Type.Should().Be("Lesson");
        savedQuiz.Questions.Should().HaveCount(1);
    }
}
