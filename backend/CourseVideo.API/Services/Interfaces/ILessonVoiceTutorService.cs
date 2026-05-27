namespace CourseVideo.API.Services.Interfaces;

public interface ILessonVoiceTutorService
{
    Task<LessonVoiceTurnResult> CompleteTurnAsync(
        Guid sessionId,
        Guid userId,
        double playbackTimeSeconds,
        byte[] audioBytes,
        CancellationToken cancellationToken);
}

public record LessonVoiceTurnResult(
    string Status,
    string TranscriptionText,
    string AnswerText,
    string SourceType,
    IReadOnlyList<LessonTutorAudioSegment> AudioSegments);
