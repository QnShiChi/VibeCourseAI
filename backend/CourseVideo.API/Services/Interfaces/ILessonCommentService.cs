using CourseVideo.API.DTOs.Comments;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonCommentService
{
    Task<LessonCommentListResponse> GetCommentsAsync(Guid lessonId, Guid currentUserId, bool isAdmin, string sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<LessonCommentThreadResponse> CreateCommentAsync(Guid lessonId, Guid currentUserId, bool isAdmin, CreateLessonCommentRequest request, CancellationToken cancellationToken = default);
    Task<LessonCommentThreadResponse> CreateReplyAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, CreateLessonReplyRequest request, CancellationToken cancellationToken = default);
    Task AddReactionAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, string emoji, CancellationToken cancellationToken = default);
    Task RemoveReactionAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, string emoji, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task HideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default);
    Task UnhideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default);
}
