using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonVideoGenerationService
{
    Task<GenerateLessonVideoResponse> GenerateCourseVideoAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task GenerateVideoForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken);
}
