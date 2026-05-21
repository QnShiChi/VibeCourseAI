using CourseVideo.API.DTOs.Courses;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonAudioGenerationService
{
    Task<GenerateLessonAudioResponse> GenerateCourseAudioAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
