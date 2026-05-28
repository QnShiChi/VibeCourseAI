using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IQuizRepository
{
    Task<Quiz?> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<Quiz?> GetCourseFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default);
    Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default);
    Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default);
    Task AddAttemptAnswersAsync(IReadOnlyCollection<QuizAttemptAnswer> answers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
