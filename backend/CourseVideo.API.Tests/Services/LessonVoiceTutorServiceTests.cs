using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonVoiceTutorServiceTests
{
    [Fact]
    public async Task CompleteTurnAsync_PersistsTranscriptionAnswerAndMessages()
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

        var contextBuilder = new Mock<ILessonContextBuilder>();
        contextBuilder.Setup(x => x.BuildAsync(session.LessonId, 10, CancellationToken.None))
            .ReturnsAsync(new LessonTutorContext("Khoa hoc", "Module", "Lesson", "Mo ta", "Script", "[]", "{}", "Transcript", 10));

        var transcription = new Mock<ITranscriptionService>();
        transcription.Setup(x => x.TranscribeAsync(It.IsAny<byte[]>(), CancellationToken.None))
            .ReturnsAsync(new TranscriptionResult("Tri tue nhan tao la gi?", 0.98m));

        var answer = new Mock<ILessonTutorAnswerService>();
        answer.Setup(x => x.GenerateAnswerAsync(It.IsAny<LessonTutorAnswerRequest>(), CancellationToken.None))
            .ReturnsAsync(new LessonTutorAnswerResult("AI la he thong mo phong tri tue cua con nguoi.", "Mixed"));

        var speech = new Mock<ILessonTutorSpeechService>();
        speech.Setup(x => x.SynthesizeAsync("vi-VN-HoaiMyNeural", "AI la he thong mo phong tri tue cua con nguoi.", CancellationToken.None))
            .ReturnsAsync([
                new LessonTutorAudioSegment(0, "AI la he thong mo phong tri tue cua con nguoi.", "/storage/voice-tutor/assistant-answers/a1.wav", 6.5)
            ]);

        var service = new LessonVoiceTutorService(
            sessions.Object,
            contextBuilder.Object,
            transcription.Object,
            answer.Object,
            speech.Object);

        var result = await service.CompleteTurnAsync(session.Id, session.UserId, 10, [1, 2, 3], CancellationToken.None);

        result.Status.Should().Be("AwaitingFollowUpDecision");
        sessions.Verify(x => x.AddMessageAsync(It.IsAny<LessonVoiceMessage>(), CancellationToken.None), Times.AtLeast(2));
    }
}
