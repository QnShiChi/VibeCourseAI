using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonVoiceTutorSessionServiceTests
{
    [Fact]
    public async Task CreateOrResumeSessionAsync_ReusesActiveSession_WhenOneExists()
    {
        var lessonId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activeSession = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            CourseId = Guid.NewGuid(),
            UserId = userId,
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural"
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetActiveSessionAsync(lessonId, userId, CancellationToken.None))
            .ReturnsAsync(activeSession);

        var lessonRepository = new Mock<ILessonRepository>();
        var service = new LessonVoiceTutorSessionService(sessions.Object, lessonRepository.Object);

        var result = await service.CreateOrResumeSessionAsync(lessonId, userId, false, CancellationToken.None);

        result.SessionId.Should().Be(activeSession.Id);
        sessions.Verify(x => x.AddAsync(It.IsAny<LessonVoiceSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseSessionAsync_MarksSessionClosed_WhenUserOwnsSession()
    {
        var session = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural"
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetByIdAsync(session.Id, CancellationToken.None)).ReturnsAsync(session);

        var service = new LessonVoiceTutorSessionService(sessions.Object, Mock.Of<ILessonRepository>());

        await service.CloseSessionAsync(session.Id, session.UserId, CancellationToken.None);

        session.Status.Should().Be("Closed");
        sessions.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
