using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ILessonCommentRepository
{
    Task<IReadOnlyList<LessonComment>> GetRootCommentsByLessonIdAsync(Guid lessonId, bool includeHidden, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LessonComment>> GetRepliesByParentIdsAsync(IReadOnlyCollection<Guid> parentIds, bool includeHidden, CancellationToken cancellationToken = default);
    Task<LessonComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<LessonCommentReaction?> GetReactionAsync(Guid commentId, Guid userId, string emoji, CancellationToken cancellationToken = default);
    Task AddAsync(LessonComment comment, CancellationToken cancellationToken = default);
    Task AddReactionAsync(LessonCommentReaction reaction, CancellationToken cancellationToken = default);
    void RemoveReaction(LessonCommentReaction reaction);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
