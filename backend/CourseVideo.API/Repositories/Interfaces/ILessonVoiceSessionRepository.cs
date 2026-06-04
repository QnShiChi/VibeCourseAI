using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ILessonVoiceSessionRepository
{
    Task<LessonVoiceSession?> GetActiveSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken);
    Task<LessonVoiceSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LessonVoiceMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task AddAsync(LessonVoiceSession session, CancellationToken cancellationToken);
    Task AddTurnAsync(LessonVoiceTurn turn, CancellationToken cancellationToken);
    Task AddMessageAsync(LessonVoiceMessage message, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
