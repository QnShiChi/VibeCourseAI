namespace CourseVideo.API.Services.Interfaces;

public interface IQuizGenerationService
{
    Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);
    Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task RegenerateQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
}
