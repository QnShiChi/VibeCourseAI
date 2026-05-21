using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class LessonCommentRepository : ILessonCommentRepository
{
    private readonly AppDbContext _dbContext;

    public LessonCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LessonComment>> GetRootCommentsByLessonIdAsync(Guid lessonId, bool includeHidden, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LessonComments
            .Include(comment => comment.User)
            .Include(comment => comment.Reactions)
            .Where(comment => comment.LessonId == lessonId && comment.ParentCommentId == null);

        if (!includeHidden)
        {
            query = query.Where(comment => !comment.IsHidden);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LessonComment>> GetRepliesByParentIdsAsync(IReadOnlyCollection<Guid> parentIds, bool includeHidden, CancellationToken cancellationToken = default)
    {
        if (parentIds.Count == 0)
        {
            return [];
        }

        var query = _dbContext.LessonComments
            .Include(comment => comment.User)
            .Include(comment => comment.ReplyToUser)
            .Include(comment => comment.Reactions)
            .Where(comment => comment.ParentCommentId.HasValue && parentIds.Contains(comment.ParentCommentId.Value));

        if (!includeHidden)
        {
            query = query.Where(comment => !comment.IsHidden);
        }

        return await query
            .OrderBy(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<LessonComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LessonComments
            .Include(comment => comment.User)
            .Include(comment => comment.ReplyToUser)
            .Include(comment => comment.Reactions)
            .FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);
    }

    public Task<LessonCommentReaction?> GetReactionAsync(Guid commentId, Guid userId, string emoji, CancellationToken cancellationToken = default)
    {
        return _dbContext.LessonCommentReactions
            .FirstOrDefaultAsync(
                reaction => reaction.CommentId == commentId && reaction.UserId == userId && reaction.Emoji == emoji,
                cancellationToken);
    }

    public Task AddAsync(LessonComment comment, CancellationToken cancellationToken = default)
    {
        return _dbContext.LessonComments.AddAsync(comment, cancellationToken).AsTask();
    }

    public Task AddReactionAsync(LessonCommentReaction reaction, CancellationToken cancellationToken = default)
    {
        return _dbContext.LessonCommentReactions.AddAsync(reaction, cancellationToken).AsTask();
    }

    public void RemoveReaction(LessonCommentReaction reaction)
    {
        _dbContext.LessonCommentReactions.Remove(reaction);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
