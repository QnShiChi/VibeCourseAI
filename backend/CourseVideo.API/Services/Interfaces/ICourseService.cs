using CourseVideo.API.DTOs.Courses;

namespace CourseVideo.API.Services.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync();
    Task<IReadOnlyList<AdminCourseListItemResponse>> GetAdminCoursesAsync();
    Task<IReadOnlyList<PublishedCourseListItemResponse>> GetPublishedCoursesAsync();
    Task<AdminCourseListItemResponse?> PublishAsync(Guid id);
    Task<AdminCourseListItemResponse?> UnpublishAsync(Guid id);
    Task<GenerateLessonContentResponse> GenerateLessonContentAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> RegenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> RegenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<CourseLearnResponse?> GetLearnPayloadAsync(Guid id, bool canPreviewDraft);
    Task<CourseStructureResponse?> GetStructureAsync(Guid id);
}
