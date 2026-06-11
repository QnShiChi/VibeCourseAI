using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonAudioGenerationService
{
    Task<GenerateLessonAudioResponse> GenerateCourseAudioAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task GenerateAudioForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken, Func<int, int, Task>? onSegmentCompleted = null);
}
