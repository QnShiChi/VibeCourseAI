using CourseVideo.API.DTOs.Quizzes;

namespace CourseVideo.API.Services.Interfaces;

public interface IQuizService
{
    Task<QuizResponse?> GetLessonQuizAsync(Guid lessonId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default);
    Task<QuizResponse?> GetFinalQuizAsync(Guid courseId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default);
    Task<CreateQuizAttemptResponse> StartAttemptAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
    Task<SubmitQuizAttemptResponse> SubmitAttemptAsync(Guid quizId, Guid attemptId, Guid userId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizAttemptHistoryItemResponse>> GetAttemptHistoryAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
}
