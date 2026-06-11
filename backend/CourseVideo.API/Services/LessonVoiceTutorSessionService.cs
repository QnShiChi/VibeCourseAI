using CourseVideo.API.DTOs.LessonVoiceTutor;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonVoiceTutorSessionService : ILessonVoiceTutorSessionService
{
    private readonly ILessonVoiceSessionRepository _sessionRepository;
    private readonly ILessonRepository _lessonRepository;

    public LessonVoiceTutorSessionService(
        ILessonVoiceSessionRepository sessionRepository,
        ILessonRepository lessonRepository)
    {
        _sessionRepository = sessionRepository;
        _lessonRepository = lessonRepository;
    }

    public async Task<LessonVoiceSessionResponse> CreateOrResumeSessionAsync(
        Guid lessonId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var activeSession = await _sessionRepository.GetActiveSessionAsync(lessonId, userId, cancellationToken);
        if (activeSession is not null)
        {
            activeSession.LastActivityAt = DateTime.UtcNow;
            await _sessionRepository.SaveChangesAsync(cancellationToken);
            return MapSession(activeSession);
        }

        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        if (!isAdmin && !lesson.VoiceTutorEnabled)
        {
            throw new InvalidOperationException("Voice tutor is disabled for this lesson.");
        }

        var session = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            CourseId = lesson.Module?.CourseId ?? Guid.Empty,
            UserId = userId,
            VoiceProfileKey = string.IsNullOrWhiteSpace(lesson.NarrationVoiceKey)
                ? "vi-VN-HoaiMyNeural"
                : lesson.NarrationVoiceKey
        };

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return MapSession(session);
    }

    public async Task<LessonVoiceSessionResponse?> GetCurrentSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetActiveSessionAsync(lessonId, userId, cancellationToken);
        return session is null ? null : MapSession(session);
    }

    public async Task<IReadOnlyList<LessonVoiceMessageResponse>> GetMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var messages = await _sessionRepository.GetMessagesAsync(sessionId, cancellationToken);
        return messages.Select(message => new LessonVoiceMessageResponse
        {
            MessageId = message.Id,
            TurnNumber = message.TurnNumber,
            Role = message.Role,
            ContentText = message.ContentText,
            ContentSourceType = message.ContentSourceType,
            AudioUrl = message.AudioUrl ?? string.Empty,
            AudioDurationSeconds = message.AudioDurationSeconds,
            SequenceIndex = message.SequenceIndex,
            CreatedAt = message.CreatedAt
        }).ToList();
    }

    public async Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        session.Status = "Closed";
        session.EndedAt = DateTime.UtcNow;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);
    }

    private static LessonVoiceSessionResponse MapSession(LessonVoiceSession session)
    {
        return new LessonVoiceSessionResponse
        {
            SessionId = session.Id,
            LessonId = session.LessonId,
            CourseId = session.CourseId,
            Status = session.Status,
            VoiceProfileKey = session.VoiceProfileKey,
            LastPausedVideoTimeSeconds = session.LastPausedVideoTimeSeconds
        };
    }
}
