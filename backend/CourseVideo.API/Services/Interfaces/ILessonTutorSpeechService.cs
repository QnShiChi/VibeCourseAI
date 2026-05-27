namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorSpeechService
{
    Task<IReadOnlyList<LessonTutorAudioSegment>> SynthesizeAsync(string voiceProfileKey, string answerText, CancellationToken cancellationToken);
}

public record LessonTutorAudioSegment(int SequenceIndex, string Text, string AudioUrl, double DurationSeconds);
