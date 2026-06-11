using CourseVideo.API.DTOs.Courses;

namespace CourseVideo.API.Services.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync();
    Task<IReadOnlyList<AdminCourseListItemResponse>> GetAdminCoursesAsync();
    Task<IReadOnlyList<PublishedCourseListItemResponse>> GetPublishedCoursesAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<AdminCourseListItemResponse?> PublishAsync(Guid id);
    Task<AdminCourseListItemResponse?> UnpublishAsync(Guid id);
    Task<CourseStructureResponse?> UpdateCategoryAsync(Guid id, Guid categoryId);
    Task<CourseStructureResponse?> UpdatePriceAsync(Guid id, int price);
    Task<CourseStructureResponse?> UploadThumbnailAsync(Guid id, IFormFile file, CancellationToken cancellationToken = default);
    Task<GenerateFullCourseResponse> GenerateFullCourseAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonContentResponse> GenerateLessonContentAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonAudioResponse> RegenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<GenerateLessonVideoResponse> RegenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);
    Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseLearnResponse?> GetLearnPayloadAsync(Guid id, Guid? currentUserId, bool canPreviewDraft, CancellationToken cancellationToken = default);
    Task<CourseStructureResponse?> GetStructureAsync(Guid id);
}
