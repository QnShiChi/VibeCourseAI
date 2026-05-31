using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;
namespace CourseVideo.API.Services.Interfaces;

public interface ILessonContentGenerationService
{
    Task<GenerateLessonContentResponse> GenerateCourseContentAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task GenerateContentForLessonInternalAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken);
}
