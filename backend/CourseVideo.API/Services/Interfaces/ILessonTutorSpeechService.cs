namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorSpeechService
{
    Task<LessonTutorAudioSegment> SynthesizeSegmentAsync(
        string voiceProfileKey,
        string answerSegment,
        int sequenceIndex,
        CancellationToken cancellationToken);
}

public record LessonTutorAudioSegment(int SequenceIndex, string Text, string AudioUrl, double DurationSeconds);
