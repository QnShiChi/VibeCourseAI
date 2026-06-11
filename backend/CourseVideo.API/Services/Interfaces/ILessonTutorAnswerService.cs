namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorAnswerService
{
    Task<LessonTutorAnswerResult> GenerateAnswerAsync(LessonTutorAnswerRequest request, CancellationToken cancellationToken);
}

public record LessonTutorAnswerRequest(LessonTutorContext Context, string QuestionText, string? ConversationSummary);
public record LessonTutorAnswerResult(string AnswerText, string SourceType);
