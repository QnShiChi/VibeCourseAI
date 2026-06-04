using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class LessonVoiceSessionRepository : ILessonVoiceSessionRepository
{
    private readonly AppDbContext _dbContext;

    public LessonVoiceSessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LessonVoiceSession?> GetActiveSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceSessions
            .OrderByDescending(session => session.LastActivityAt)
            .FirstOrDefaultAsync(
                session => session.LessonId == lessonId
                    && session.UserId == userId
                    && session.Status == "Active",
                cancellationToken);
    }

    public Task<LessonVoiceSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<LessonVoiceMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _dbContext.LessonVoiceMessages
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.TurnNumber)
            .ThenBy(message => message.SequenceIndex)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(LessonVoiceSession session, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceSessions.AddAsync(session, cancellationToken).AsTask();
    }

    public Task AddTurnAsync(LessonVoiceTurn turn, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceTurns.AddAsync(turn, cancellationToken).AsTask();
    }

    public Task AddMessageAsync(LessonVoiceMessage message, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceMessages.AddAsync(message, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
