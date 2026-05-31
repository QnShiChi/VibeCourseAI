using System.Security.Claims;
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class QuizzesControllerTests
{
    [Fact]
    public async Task GetLessonQuiz_ReturnsOk_WhenQuizExists()
    {
        var service = new Mock<IQuizService>();
        service.Setup(x => x.GetLessonQuizAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuizResponse
            {
                QuizId = Guid.NewGuid(),
                Title = "Quiz",
                Status = "Ready",
                QuestionCount = 1
            });

        var controller = CreateController(service, isAdmin: false);

        var result = await controller.GetLessonQuiz(Guid.NewGuid());

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private static QuizzesController CreateController(Mock<IQuizService> service, bool isAdmin)
    {
        return new QuizzesController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
                    ], "Test"))
                }
            }
        };
    }
}
