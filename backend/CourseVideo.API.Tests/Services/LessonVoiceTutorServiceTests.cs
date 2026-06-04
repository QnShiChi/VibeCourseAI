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
    public async Task CompleteTurnAsync_StreamsSegmentsAndPersistsMessages()
    {
        var session = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural",
            Turns = []
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetByIdAsync(session.Id, CancellationToken.None)).ReturnsAsync(session);

        var contextBuilder = new Mock<ILessonContextBuilder>();
        contextBuilder.Setup(x => x.BuildAsync(session.LessonId, 10, CancellationToken.None))
            .ReturnsAsync(new LessonTutorContext("Khoa hoc", "Module", "Lesson", "Mo ta", "Script", "[]", "{}", "Transcript", 10));

        var transcription = new Mock<ITranscriptionService>();
        transcription.Setup(x => x.TranscribeAsync(It.IsAny<byte[]>(), CancellationToken.None))
            .ReturnsAsync(new TranscriptionResult("Tri tue nhan tao la gi?", 0.98m));

        var responseStream = new Mock<ILessonTutorResponseStreamService>();
        responseStream.Setup(x => x.StreamAnswerAsync(It.IsAny<LessonTutorAnswerRequest>(), CancellationToken.None))
            .Returns(ToAsyncEnumerable(["AI la ", "he thong mo phong.", " Rat huu ich"]));

        var segmenter = new Mock<ILessonTutorSegmenter>();
        segmenter.SetupSequence(x => x.PushText(It.IsAny<string>()))
            .Returns(["AI la he thong mo phong."])
            .Returns([])
            .Returns([]);
        segmenter.Setup(x => x.FlushRemaining()).Returns(["Rat huu ich."]);

        var speech = new Mock<ILessonTutorSpeechService>();
        speech.Setup(x => x.SynthesizeSegmentAsync("vi-VN-HoaiMyNeural", "AI la he thong mo phong.", 0, CancellationToken.None))
            .ReturnsAsync(new LessonTutorAudioSegment(0, "AI la he thong mo phong.", "/storage/voice-tutor/assistant-answers/a1.mp3", 2.3));
        speech.Setup(x => x.SynthesizeSegmentAsync("vi-VN-HoaiMyNeural", "Rat huu ich.", 1, CancellationToken.None))
            .ReturnsAsync(new LessonTutorAudioSegment(1, "Rat huu ich.", "/storage/voice-tutor/assistant-answers/a2.mp3", 1.1));

        var streamedSegments = new List<LessonTutorAudioSegment>();
        var service = new LessonVoiceTutorService(
            sessions.Object,
            contextBuilder.Object,
            transcription.Object,
            responseStream.Object,
            segmenter.Object,
            speech.Object);

        var result = await service.CompleteTurnAsync(
            session.Id,
            session.UserId,
            10,
            [1, 2, 3],
            segment =>
            {
                streamedSegments.Add(segment);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        result.Status.Should().Be("AwaitingFollowUpDecision");
        result.TranscriptionText.Should().Be("Tri tue nhan tao la gi?");
        result.AnswerText.Should().Be("AI la he thong mo phong. Rat huu ich.");
        result.AudioSegments.Should().HaveCount(2);
        streamedSegments.Should().HaveCount(2);
        sessions.Verify(x => x.AddTurnAsync(It.Is<LessonVoiceTurn>(turn => turn.Status == "Completed"), CancellationToken.None), Times.Once);
        sessions.Verify(x => x.AddMessageAsync(It.IsAny<LessonVoiceMessage>(), CancellationToken.None), Times.Exactly(2));
        sessions.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
