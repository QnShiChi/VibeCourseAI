using CourseVideo.API.DTOs.Courses;

namespace CourseVideo.API.Services.Interfaces;

public interface IFullCourseGenerationService
{
    Task<GenerateFullCourseResponse> GenerateFullCourseAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
