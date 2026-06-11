using System.Security.Claims;
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.LessonVoiceTutor;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class LessonVoiceSessionsControllerTests
{
    [Fact]
    public async Task CreateSession_ReturnsOk_WhenServiceCreatesSession()
    {
        var lessonId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new Mock<ILessonVoiceTutorSessionService>();
        service.Setup(x => x.CreateOrResumeSessionAsync(lessonId, userId, false, CancellationToken.None))
            .ReturnsAsync(new LessonVoiceSessionResponse
            {
                SessionId = Guid.NewGuid(),
                LessonId = lessonId,
                CourseId = Guid.NewGuid(),
                Status = "Active",
                VoiceProfileKey = "vi-VN-HoaiMyNeural"
            });

        var controller = BuildController(service.Object, userId, "User");

        var result = await controller.CreateSession(lessonId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMessages_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var service = new Mock<ILessonVoiceTutorSessionService>();
        service.Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), CancellationToken.None))
            .ThrowsAsync(new KeyNotFoundException("Session not found."));

        var controller = BuildController(service.Object, Guid.NewGuid(), "User");

        var result = await controller.GetMessages(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static LessonVoiceSessionsController BuildController(
        ILessonVoiceTutorSessionService service,
        Guid userId,
        string role)
    {
        var controller = new LessonVoiceSessionsController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "TestAuth"))
            }
        };

        return controller;
    }
}
