using CourseVideo.API.DTOs.LessonVoiceTutor;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonVoiceTutorSessionService
{
    Task<LessonVoiceSessionResponse> CreateOrResumeSessionAsync(Guid lessonId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<LessonVoiceSessionResponse?> GetCurrentSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LessonVoiceMessageResponse>> GetMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}
